using System;
using System.Collections.Generic;

namespace KaaKaaFramework.HFSM
{
    /// <summary>
    /// HFSM状态接口
    /// </summary>
    public interface IHFSMState
    {
        string StateName { get; }
        IHFSMState ParentState { get; set; }
        HFSMBlackboard Blackboard { get; set; }
        
        void OnEnter();
        void OnUpdate();
        void OnFixedUpdate();
        void OnLateUpdate();
        void OnExit();
    }

    /// <summary>
    /// 分层状态机接口
    /// </summary>
    public interface IHierarchicalStateMachine : IHFSMState
    {
        IHFSMState CurrentState { get; }
        IHFSMState DefaultState { get; }
        List<IHFSMState> States { get; }
        List<HFSMTransition> Transitions { get; }
        
        void AddState(IHFSMState state);
        void RemoveState(IHFSMState state);
        void AddTransition(HFSMTransition transition);
        void RemoveTransition(HFSMTransition transition);
        void ChangeState(string stateName);
        void ChangeState(IHFSMState state);
    }
}
