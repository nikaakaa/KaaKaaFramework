using System;
using UnityEngine;

namespace KaaKaaFramework.HFSM
{
    /// <summary>
    /// HFSM状态基类
    /// </summary>
    [Serializable]
    public abstract class HFSMState : IHFSMState
    {
        [SerializeField]
        protected string stateName;
        
        public string StateName => stateName;
        public IHFSMState ParentState { get; set; }
        public HFSMBlackboard Blackboard { get; set; }

        public HFSMState() { }

        public HFSMState(string name)
        {
            stateName = name;
        }

        public virtual void OnEnter()
        {
#if UNITY_EDITOR
            Debug.Log($"[HFSM] Enter State: {StateName}");
#endif
        }

        public virtual void OnUpdate() { }

        public virtual void OnFixedUpdate() { }

        public virtual void OnLateUpdate() { }

        public virtual void OnExit()
        {
#if UNITY_EDITOR
            Debug.Log($"[HFSM] Exit State: {StateName}");
#endif
        }

        /// <summary>
        /// 获取根状态机
        /// </summary>
        protected IHierarchicalStateMachine GetRootStateMachine()
        {
            IHFSMState current = this;
            while (current.ParentState != null)
            {
                current = current.ParentState;
            }
            return current as IHierarchicalStateMachine;
        }
    }
}
