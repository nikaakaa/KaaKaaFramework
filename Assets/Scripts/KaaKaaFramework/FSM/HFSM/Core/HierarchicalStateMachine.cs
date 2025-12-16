using System;
using System.Collections.Generic;
using UnityEngine;

namespace KaaKaaFramework.HFSM
{
    /// <summary>
    /// 分层有限状态机
    /// </summary>
    [Serializable]
    public class HierarchicalStateMachine : IHierarchicalStateMachine
    {
        [SerializeField]
        protected string stateName = "Root";
        
        [SerializeField]
        protected string defaultStateName;

        public string StateName => stateName;
        public IHFSMState ParentState { get; set; }
        public HFSMBlackboard Blackboard { get; set; }
        public IHFSMState CurrentState { get; protected set; }
        public IHFSMState DefaultState { get; protected set; }
        public List<IHFSMState> States { get; protected set; } = new List<IHFSMState>();
        public List<HFSMTransition> Transitions { get; protected set; } = new List<HFSMTransition>();
        public List<HFSMAnyTransition> AnyTransitions { get; protected set; } = new List<HFSMAnyTransition>();

        protected Dictionary<string, IHFSMState> _stateDict = new Dictionary<string, IHFSMState>();
        protected Dictionary<string, List<HFSMTransition>> _transitionDict = new Dictionary<string, List<HFSMTransition>>();

        public HierarchicalStateMachine() { }

        public HierarchicalStateMachine(string name)
        {
            stateName = name;
        }

        #region State Management

        public void AddState(IHFSMState state)
        {
            if (state == null || string.IsNullOrEmpty(state.StateName))
                return;

            if (_stateDict.ContainsKey(state.StateName))
            {
                Debug.LogWarning($"[HFSM] State '{state.StateName}' already exists in '{StateName}'");
                return;
            }

            state.ParentState = this;
            state.Blackboard = Blackboard;
            States.Add(state);
            _stateDict[state.StateName] = state;

            // 如果是第一个状态，设为默认状态
            if (DefaultState == null)
            {
                DefaultState = state;
                defaultStateName = state.StateName;
            }
        }

        public void RemoveState(IHFSMState state)
        {
            if (state == null)
                return;

            if (_stateDict.Remove(state.StateName))
            {
                States.Remove(state);
                state.ParentState = null;

                if (DefaultState == state)
                {
                    DefaultState = States.Count > 0 ? States[0] : null;
                    defaultStateName = DefaultState?.StateName;
                }
            }
        }

        public void SetDefaultState(string stateName)
        {
            if (_stateDict.TryGetValue(stateName, out var state))
            {
                DefaultState = state;
                defaultStateName = stateName;
            }
        }

        #endregion

        #region Transition Management

        public void AddTransition(HFSMTransition transition)
        {
            if (transition == null)
                return;

            transition.Blackboard = Blackboard;

            // 解析状态引用
            if (!string.IsNullOrEmpty(transition.FromStateName) && transition.FromStateName != "Any")
            {
                _stateDict.TryGetValue(transition.FromStateName, out var fromState);
                transition.FromState = fromState;
            }

            if (!string.IsNullOrEmpty(transition.ToStateName))
            {
                _stateDict.TryGetValue(transition.ToStateName, out var toState);
                transition.ToState = toState;
            }

            // Any转换特殊处理
            if (transition is HFSMAnyTransition anyTransition)
            {
                AnyTransitions.Add(anyTransition);
            }
            else
            {
                Transitions.Add(transition);

                // 按源状态分组
                if (!string.IsNullOrEmpty(transition.FromStateName))
                {
                    if (!_transitionDict.ContainsKey(transition.FromStateName))
                    {
                        _transitionDict[transition.FromStateName] = new List<HFSMTransition>();
                    }
                    _transitionDict[transition.FromStateName].Add(transition);
                }
            }
        }

        public void RemoveTransition(HFSMTransition transition)
        {
            if (transition == null)
                return;

            if (transition is HFSMAnyTransition anyTransition)
            {
                AnyTransitions.Remove(anyTransition);
            }
            else
            {
                Transitions.Remove(transition);

                if (!string.IsNullOrEmpty(transition.FromStateName) &&
                    _transitionDict.TryGetValue(transition.FromStateName, out var list))
                {
                    list.Remove(transition);
                }
            }
        }

        #endregion

        #region State Machine Lifecycle

        public virtual void OnEnter()
        {
#if UNITY_EDITOR
            Debug.Log($"[HFSM] Enter StateMachine: {StateName}");
#endif
            // 进入默认状态
            if (DefaultState != null)
            {
                CurrentState = DefaultState;
                CurrentState.OnEnter();
            }
        }

        public virtual void OnUpdate()
        {
            // 检查Any转换
            foreach (var anyTransition in AnyTransitions)
            {
                if (anyTransition.CanTransition() && anyTransition.ToState != CurrentState)
                {
                    ChangeState(anyTransition.ToState);
                    return;
                }
            }

            // 检查当前状态的转换
            if (CurrentState != null)
            {
                if (_transitionDict.TryGetValue(CurrentState.StateName, out var transitions))
                {
                    foreach (var transition in transitions)
                    {
                        if (transition.CanTransition())
                        {
                            ChangeState(transition.ToState);
                            return;
                        }
                    }
                }

                // 更新当前状态
                CurrentState.OnUpdate();
            }
        }

        public virtual void OnFixedUpdate()
        {
            CurrentState?.OnFixedUpdate();
        }

        public virtual void OnLateUpdate()
        {
            CurrentState?.OnLateUpdate();
        }

        public virtual void OnExit()
        {
            CurrentState?.OnExit();
            CurrentState = null;
#if UNITY_EDITOR
            Debug.Log($"[HFSM] Exit StateMachine: {StateName}");
#endif
        }

        #endregion

        #region State Change

        public void ChangeState(string stateName)
        {
            if (_stateDict.TryGetValue(stateName, out var state))
            {
                ChangeState(state);
            }
            else
            {
                Debug.LogWarning($"[HFSM] State '{stateName}' not found in '{StateName}'");
            }
        }

        public void ChangeState(IHFSMState state)
        {
            if (state == null || state == CurrentState)
                return;

            CurrentState?.OnExit();
            CurrentState = state;
            CurrentState.OnEnter();
        }

        #endregion

        #region Utility

        public IHFSMState GetState(string stateName)
        {
            _stateDict.TryGetValue(stateName, out var state);
            return state;
        }

        public T GetState<T>(string stateName) where T : class, IHFSMState
        {
            return GetState(stateName) as T;
        }

        /// <summary>
        /// 初始化状态机（在所有状态和转换添加完成后调用）
        /// </summary>
        public void Initialize(HFSMBlackboard blackboard = null)
        {
            Blackboard = blackboard ?? new HFSMBlackboard();

            // 传递黑板给所有状态
            foreach (var state in States)
            {
                state.Blackboard = Blackboard;
                
                // 如果是子状态机，递归初始化
                if (state is HierarchicalStateMachine subMachine)
                {
                    subMachine.Initialize(Blackboard);
                }
            }

            // 重新解析转换
            foreach (var transition in Transitions)
            {
                transition.Blackboard = Blackboard;
                if (!string.IsNullOrEmpty(transition.FromStateName))
                    _stateDict.TryGetValue(transition.FromStateName, out var fromState);
                if (!string.IsNullOrEmpty(transition.ToStateName))
                    transition.ToState = _stateDict.GetValueOrDefault(transition.ToStateName);
            }

            foreach (var anyTransition in AnyTransitions)
            {
                anyTransition.Blackboard = Blackboard;
                if (!string.IsNullOrEmpty(anyTransition.ToStateName))
                    anyTransition.ToState = _stateDict.GetValueOrDefault(anyTransition.ToStateName);
            }

            // 设置默认状态
            if (!string.IsNullOrEmpty(defaultStateName))
            {
                SetDefaultState(defaultStateName);
            }
        }

        #endregion
    }
}
