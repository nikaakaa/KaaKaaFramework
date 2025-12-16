using System;
using UnityEngine;

namespace KaaKaaFramework.HFSM
{
    /// <summary>
    /// HFSM状态行为资源 - 可复用的状态逻辑
    /// </summary>
    public abstract class HFSMStateBehaviour : ScriptableObject, IHFSMState
    {
        [SerializeField]
        protected string stateName;
        
        public string StateName => stateName;
        public IHFSMState ParentState { get; set; }
        public HFSMBlackboard Blackboard { get; set; }

        /// <summary>
        /// 状态拥有者（通常是运行状态机的MonoBehaviour）
        /// </summary>
        public GameObject Owner { get; set; }

        public virtual void OnEnter()
        {
#if UNITY_EDITOR
            Debug.Log($"[HFSM] Enter State Behaviour: {StateName}");
#endif
        }

        public virtual void OnUpdate() { }

        public virtual void OnFixedUpdate() { }

        public virtual void OnLateUpdate() { }

        public virtual void OnExit()
        {
#if UNITY_EDITOR
            Debug.Log($"[HFSM] Exit State Behaviour: {StateName}");
#endif
        }

        /// <summary>
        /// 创建运行时实例（避免修改原始资源）
        /// </summary>
        public virtual HFSMStateBehaviour CreateRuntimeInstance()
        {
            return Instantiate(this);
        }
    }
}
