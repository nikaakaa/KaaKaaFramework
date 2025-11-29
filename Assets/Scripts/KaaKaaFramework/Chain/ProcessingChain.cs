using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IProcessor<in TIn,out TOut>
{
    TOut Process(TIn input);
}
public class Combined<A, B, C> : IProcessor<A, C>
{
    readonly IProcessor<A, B> first;
    readonly IProcessor<B, C> second;
    public C Process(A input)=>second.Process(first.Process(input));
}
