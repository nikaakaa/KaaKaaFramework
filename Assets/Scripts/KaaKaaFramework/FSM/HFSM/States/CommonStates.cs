using System;
using UnityEngine;

namespace KaaKaaFramework.HFSM
{
    /// <summary>
    /// 带计时器的状态 - 在指定时间后自动结束
    /// </summary>
    [Serializable]
    public class TimedState : HFSMState
    {
        [SerializeField]
        protected float duration = 1f;
        
        protected float _timer;
        protected bool _isComplete;

        public float Duration => duration;
        public float ElapsedTime => _timer;
        public float RemainingTime => Mathf.Max(0, duration - _timer);
        public float Progress => duration > 0 ? Mathf.Clamp01(_timer / duration) : 1f;
        public bool IsComplete => _isComplete;

        public TimedState() { }

        public TimedState(string name, float duration = 1f) : base(name)
        {
            this.duration = duration;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _timer = 0f;
            _isComplete = false;
        }

        public override void OnUpdate()
        {
            _timer += Time.deltaTime;
            
            if (!_isComplete && _timer >= duration)
            {
                _isComplete = true;
                OnTimeComplete();
            }
        }

        /// <summary>
        /// 计时完成时调用
        /// </summary>
        protected virtual void OnTimeComplete()
        {
            // 可以在子类中覆写，或者通过黑板触发转换
            Blackboard?.Set($"{StateName}_Complete", true);
        }
    }

    /// <summary>
    /// 动画状态 - 播放动画的状态
    /// </summary>
    [Serializable]
    public class AnimationState : HFSMState
    {
        [SerializeField]
        protected string animationName;
        
        [SerializeField]
        protected float crossFadeDuration = 0.1f;
        
        [SerializeField]
        protected int layer = 0;

        protected Animator _animator;

        public string AnimationName => animationName;

        public AnimationState() { }

        public AnimationState(string name, string animationName) : base(name)
        {
            this.animationName = animationName;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            
            // 尝试从黑板获取Animator
            if (Blackboard != null && Blackboard.TryGet("Animator", out Animator animator))
            {
                _animator = animator;
                PlayAnimation();
            }
        }

        protected virtual void PlayAnimation()
        {
            if (_animator != null && !string.IsNullOrEmpty(animationName))
            {
                _animator.CrossFade(animationName, crossFadeDuration, layer);
            }
        }
    }

    /// <summary>
    /// 条件等待状态 - 等待条件满足
    /// </summary>
    [Serializable]
    public class WaitForConditionState : HFSMState
    {
        [SerializeField]
        protected string conditionKey;
        
        [SerializeField]
        protected bool targetValue = true;
        
        [SerializeField]
        protected float timeout = -1f; // -1表示无超时

        protected float _timer;
        protected bool _isComplete;

        public bool IsComplete => _isComplete;

        public WaitForConditionState() { }

        public WaitForConditionState(string name, string conditionKey, bool targetValue = true) : base(name)
        {
            this.conditionKey = conditionKey;
            this.targetValue = targetValue;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _timer = 0f;
            _isComplete = false;
        }

        public override void OnUpdate()
        {
            if (_isComplete)
                return;

            // 检查条件
            if (Blackboard != null && Blackboard.TryGet(conditionKey, out bool value) && value == targetValue)
            {
                _isComplete = true;
                OnConditionMet();
                return;
            }

            // 检查超时
            if (timeout > 0)
            {
                _timer += Time.deltaTime;
                if (_timer >= timeout)
                {
                    _isComplete = true;
                    OnTimeout();
                }
            }
        }

        protected virtual void OnConditionMet()
        {
            Blackboard?.Set($"{StateName}_ConditionMet", true);
        }

        protected virtual void OnTimeout()
        {
            Blackboard?.Set($"{StateName}_Timeout", true);
        }
    }

    /// <summary>
    /// 并行状态 - 同时执行多个子状态
    /// </summary>
    [Serializable]
    public class ParallelState : HFSMState
    {
        protected IHFSMState[] _parallelStates;

        public ParallelState() { }

        public ParallelState(string name, params IHFSMState[] states) : base(name)
        {
            _parallelStates = states;
        }

        public void SetParallelStates(params IHFSMState[] states)
        {
            _parallelStates = states;
            foreach (var state in _parallelStates)
            {
                state.ParentState = this;
                state.Blackboard = Blackboard;
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();
            
            if (_parallelStates != null)
            {
                foreach (var state in _parallelStates)
                {
                    state.Blackboard = Blackboard;
                    state.OnEnter();
                }
            }
        }

        public override void OnUpdate()
        {
            if (_parallelStates != null)
            {
                foreach (var state in _parallelStates)
                {
                    state.OnUpdate();
                }
            }
        }

        public override void OnFixedUpdate()
        {
            if (_parallelStates != null)
            {
                foreach (var state in _parallelStates)
                {
                    state.OnFixedUpdate();
                }
            }
        }

        public override void OnLateUpdate()
        {
            if (_parallelStates != null)
            {
                foreach (var state in _parallelStates)
                {
                    state.OnLateUpdate();
                }
            }
        }

        public override void OnExit()
        {
            if (_parallelStates != null)
            {
                foreach (var state in _parallelStates)
                {
                    state.OnExit();
                }
            }
            
            base.OnExit();
        }
    }

    /// <summary>
    /// 序列状态 - 按顺序执行子状态
    /// </summary>
    [Serializable]
    public class SequenceState : HFSMState
    {
        protected IHFSMState[] _sequenceStates;
        protected int _currentIndex;
        protected bool _isComplete;

        public int CurrentIndex => _currentIndex;
        public bool IsComplete => _isComplete;
        public IHFSMState CurrentSubState => _sequenceStates != null && _currentIndex < _sequenceStates.Length 
            ? _sequenceStates[_currentIndex] : null;

        public SequenceState() { }

        public SequenceState(string name, params IHFSMState[] states) : base(name)
        {
            _sequenceStates = states;
        }

        public void SetSequenceStates(params IHFSMState[] states)
        {
            _sequenceStates = states;
            foreach (var state in _sequenceStates)
            {
                state.ParentState = this;
                state.Blackboard = Blackboard;
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _currentIndex = 0;
            _isComplete = false;
            
            if (_sequenceStates != null && _sequenceStates.Length > 0)
            {
                _sequenceStates[0].Blackboard = Blackboard;
                _sequenceStates[0].OnEnter();
            }
            else
            {
                _isComplete = true;
            }
        }

        public override void OnUpdate()
        {
            if (_isComplete || _sequenceStates == null)
                return;

            var currentState = _sequenceStates[_currentIndex];
            currentState.OnUpdate();

            // 检查当前子状态是否完成（需要在子状态中设置完成标志）
            if (Blackboard != null && 
                Blackboard.TryGet($"{currentState.StateName}_Complete", out bool isComplete) && 
                isComplete)
            {
                Blackboard.Set($"{currentState.StateName}_Complete", false);
                MoveToNextState();
            }
        }

        /// <summary>
        /// 手动移动到下一个状态
        /// </summary>
        public void MoveToNextState()
        {
            if (_isComplete || _sequenceStates == null)
                return;

            _sequenceStates[_currentIndex].OnExit();
            _currentIndex++;

            if (_currentIndex < _sequenceStates.Length)
            {
                _sequenceStates[_currentIndex].Blackboard = Blackboard;
                _sequenceStates[_currentIndex].OnEnter();
            }
            else
            {
                _isComplete = true;
                OnSequenceComplete();
            }
        }

        protected virtual void OnSequenceComplete()
        {
            Blackboard?.Set($"{StateName}_Complete", true);
        }

        public override void OnExit()
        {
            if (!_isComplete && _sequenceStates != null && _currentIndex < _sequenceStates.Length)
            {
                _sequenceStates[_currentIndex].OnExit();
            }
            
            base.OnExit();
        }
    }
}
