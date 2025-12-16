using System;
using UnityEngine;

public interface IProcessor<in TIn, out TOut>
{
    TOut Process(TIn input);
}

public delegate TOut ProcessorDelegate<in TIn, out TOut>(TIn input);

class Combined<A, B, C> : IProcessor<A, C>
{
    readonly IProcessor<A, B> first;
    readonly IProcessor<B, C> second;

    public Combined(IProcessor<A, B> first, IProcessor<B, C> second)
    {
        this.first = first;
        this.second = second;
    }

    public C Process(A input) => second.Process(first.Process(input));
}

class Chain<TIn, TOut>
{
    readonly IProcessor<TIn, TOut> processor;

    Chain(IProcessor<TIn, TOut> processor)
    {
        this.processor = processor;
    }

    public static Chain<TIn, TOut> Start(IProcessor<TIn, TOut> processor)
    {
        return new Chain<TIn, TOut>(processor);
    }

    public Chain<TIn, TNext> Then<TNext>(IProcessor<TOut, TNext> next)
    {
        var combined = new Combined<TIn, TOut, TNext>(processor, next);
        return new Chain<TIn, TNext>(combined);
    }

    public TOut Run(TIn input) => processor.Process(input);

    public ProcessorDelegate<TIn, TOut> Compile() => input => processor.Process(input);
}

public delegate TChain ChainFactory<out TIn, in TOut, out TChain>(IProcessor<TIn, TOut> processor)
    where TChain : FluentChain<TIn, TOut, TChain>;

public abstract class FluentChain<TIn, TOut, TDerived> where TDerived : FluentChain<TIn, TOut, TDerived>
{
    public IProcessor<TIn, TOut> processor;

    protected FluentChain(IProcessor<TIn, TOut> processor)
    {
        this.processor = processor ?? throw new ArgumentNullException(nameof(processor));
    }

    protected TNextSelf Then<TNext, TNextSelf, TProcessor>(
        TProcessor nextProcessor,
        ChainFactory<TIn, TNext, TNextSelf> factory)
        where TNextSelf : FluentChain<TIn, TNext, TNextSelf>
        where TProcessor : class, IProcessor<TOut, TNext>
    {
        if (nextProcessor == null) throw new ArgumentNullException(nameof(nextProcessor));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        var combined = new Combined<TIn, TOut, TNext>(processor, nextProcessor);

        return factory(combined);
    }

    public TOut Run(TIn input)
    {
        if (processor == null) throw new InvalidOperationException("Processor is not initialized. Use Chain.Start() to begin a chain.");
        return processor.Process(input);
    }

    public ProcessorDelegate<TIn, TOut> Compile()
    {
        if (processor == null) throw new InvalidOperationException("Processor is not initialized. Use Chain.Start() to begin a chain.");
        return input => processor.Process(input);
    }
}