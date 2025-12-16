using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;


#region State

public interface IStateNode
{
    public void OnEnter();
    public void OnUpdate();
    public void OnExit();
}
public interface IStateMachine
{
    public Dictionary<string, IState> StatesDic { get; }
    public HashSet<ITransition> Transitions { get; }
}
public static class StateMachineExtension
{
    public static IStateMachine RegisterState(this IStateMachine stateMachine, string stateName, IState state)
    {
        if (!stateMachine.StatesDic.ContainsKey(stateName))
        {
            stateMachine.StatesDic.Add(stateName, state);
        }
        return stateMachine;
    }
    public static IStateMachine RegisterTransition(this IStateMachine stateMachine, ITransition transition)
    {
        stateMachine.Transitions.Add(transition);
        return stateMachine;
    }
    public static bool TryTransitions(this IStateMachine stateMachine)
    {
        var triggered = false;
        foreach (var transition in stateMachine.Transitions)
        {
            if (transition.TryTransition())
            {
                triggered = true;
                break;
            }
        }
        return !triggered;
    }
}
public interface IState : IStateNode, IStateMachine
{
    public string StateName { get; }
    public IState CurrentState { get; set;}
    public bool OnTick();
}
public class State : IState
{
    private Action onEnterAction{get;}
    private Action onUpdateAction{get;}
    private Action onExitAction{get;}
    
    public State(string stateName)
    {
        StateName = stateName;
    }
    public State(string stateName,Action onEnter=null,Action onUpdate=null,Action onExit=null)
    {
        StateName = stateName;
        onEnterAction = onEnter;
        onUpdateAction = onUpdate;
        onExitAction = onExit;
    }
    public string StateName { get; }
    public IState CurrentState { get; set; }
    public Dictionary<string, IState> StatesDic { get; } = new();
    public HashSet<ITransition> Transitions { get; } = new();

    public virtual void OnEnter()
    {
        onEnterAction?.Invoke();
    }

    public virtual void OnExit()
    {
        onExitAction?.Invoke();
    }
    public virtual void OnUpdate()
    {
        onUpdateAction?.Invoke();
    }
    /// <summary>
    /// 给外部调用的
    /// </summary>
    /// <returns></returns>
    public bool OnTick()
    {
        if(!this.TryTransitions())return false;

        OnUpdate();

        foreach (var state in StatesDic.Values)
        {
            if (!state.OnTick())return false;
        }
        return true;
    }

    public bool ChangeToTheState(string stateName)
    {
        if (StatesDic.TryGetValue(stateName, out var targetState))
        {
            CurrentState?.OnExit();
            CurrentState = targetState;
            CurrentState.OnEnter();
            return true;
        }
        throw new KeyNotFoundException($"State {stateName} not found");
    }

}

#endregion

#region Transitions

public interface ITransition
{
    public IState FromState { get; }
    public IState ToState { get; }
    public Func<bool> CanTrigger { get; }
}
public static class TransitionExtension
{
    public static bool IsTriggered(this ITransition transition)
    {
        if (transition is Transition t)
        {
            return t.CanTrigger();
        }
        throw new InvalidOperationException("Unknown transition type");
    }
    public static bool TryTransition(this ITransition transition)
    {
        if (transition.IsTriggered())
        {
            transition.FromState.OnExit();
            transition.ToState.OnEnter();
            return true;
        }
        return false;
    }

}
public class Transition : ITransition
{
    public Transition(IStateMachine parentState,string fromStateName, string toStateName, Func<bool> condition)
    {
        if (!parentState.StatesDic.TryGetValue(fromStateName, out var fromState) ||
            !parentState.StatesDic.TryGetValue(toStateName, out var toState))
        {
            throw new KeyNotFoundException("FromState or ToState not found in the state machine");
        }
        FromState = fromState;
        ToState = toState;
        CanTrigger = condition;
    }
    public Transition(IState fromState, IState toState, Func<bool> condition)
    {
        if (fromState == null || toState == null || condition == null)
        {
            throw new ArgumentNullException("Transition parameters cannot be null");
        }
        FromState = fromState;
        ToState = toState;
        CanTrigger = condition;
    }

    public IState FromState { get; }
    public IState ToState { get; }
    public Func<bool> CanTrigger { get; }
}

#endregion
