using System;
using System.Collections.Generic;

namespace KaaKaaFramework.HFSM
{
    /// <summary>
    /// HFSM构建器 - 流式API构建状态机
    /// </summary>
    public class HFSMBuilder
    {
        private readonly HierarchicalStateMachine _fsm;
        private readonly List<HFSMTransition> _pendingTransitions = new List<HFSMTransition>();
        private readonly Dictionary<string, IHFSMState> _states = new Dictionary<string, IHFSMState>();

        public HFSMBuilder(string name = "Root")
        {
            _fsm = new HierarchicalStateMachine(name);
        }

        #region State Building

        /// <summary>
        /// 添加状态
        /// </summary>
        public HFSMBuilder State<T>(string name) where T : HFSMState, new()
        {
            var state = new T();
            SetStateName(state, name);
            _fsm.AddState(state);
            _states[name] = state;
            return this;
        }

        /// <summary>
        /// 添加状态实例
        /// </summary>
        public HFSMBuilder State(IHFSMState state)
        {
            _fsm.AddState(state);
            _states[state.StateName] = state;
            return this;
        }

        /// <summary>
        /// 添加子状态机
        /// </summary>
        public HFSMBuilder SubStateMachine(string name, Action<HFSMBuilder> configure)
        {
            var subBuilder = new HFSMBuilder(name);
            configure(subBuilder);
            var subFsm = subBuilder.Build();
            _fsm.AddState(subFsm);
            _states[name] = subFsm;
            return this;
        }

        /// <summary>
        /// 添加子状态机实例
        /// </summary>
        public HFSMBuilder SubStateMachine(HierarchicalStateMachine subFsm)
        {
            _fsm.AddState(subFsm);
            _states[subFsm.StateName] = subFsm;
            return this;
        }

        /// <summary>
        /// 设置默认状态
        /// </summary>
        public HFSMBuilder DefaultState(string stateName)
        {
            _fsm.SetDefaultState(stateName);
            return this;
        }

        #endregion

        #region Transition Building

        /// <summary>
        /// 添加转换
        /// </summary>
        public TransitionBuilder Transition(string from, string to)
        {
            return new TransitionBuilder(this, from, to, false);
        }

        /// <summary>
        /// 添加Any转换
        /// </summary>
        public TransitionBuilder AnyTransition(string to)
        {
            return new TransitionBuilder(this, "Any", to, true);
        }

        internal void AddTransition(HFSMTransition transition)
        {
            _pendingTransitions.Add(transition);
        }

        #endregion

        #region Parameter Building

        /// <summary>
        /// 设置初始参数
        /// </summary>
        public HFSMBuilder WithParameter(string name, bool value)
        {
            _fsm.Blackboard?.Set(name, value);
            return this;
        }

        public HFSMBuilder WithParameter(string name, int value)
        {
            _fsm.Blackboard?.Set(name, value);
            return this;
        }

        public HFSMBuilder WithParameter(string name, float value)
        {
            _fsm.Blackboard?.Set(name, value);
            return this;
        }

        public HFSMBuilder WithParameter(string name, string value)
        {
            _fsm.Blackboard?.Set(name, value);
            return this;
        }

        #endregion

        #region Build

        /// <summary>
        /// 构建状态机
        /// </summary>
        public HierarchicalStateMachine Build()
        {
            // 初始化
            _fsm.Initialize();

            // 添加所有待处理的转换
            foreach (var transition in _pendingTransitions)
            {
                _fsm.AddTransition(transition);
            }

            // 重新初始化以解析转换引用
            _fsm.Initialize(_fsm.Blackboard);

            return _fsm;
        }

        #endregion

        #region Utility

        private void SetStateName(HFSMState state, string name)
        {
            // 使用反射设置私有字段
            var field = typeof(HFSMState).GetField("stateName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(state, name);
        }

        #endregion
    }

    /// <summary>
    /// 转换构建器
    /// </summary>
    public class TransitionBuilder
    {
        private readonly HFSMBuilder _parent;
        private readonly string _from;
        private readonly string _to;
        private readonly bool _isAny;
        private readonly List<HFSMCondition> _conditions = new List<HFSMCondition>();
        private string _name;

        public TransitionBuilder(HFSMBuilder parent, string from, string to, bool isAny)
        {
            _parent = parent;
            _from = from;
            _to = to;
            _isAny = isAny;
            _name = $"{from}_to_{to}";
        }

        /// <summary>
        /// 设置转换名称
        /// </summary>
        public TransitionBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        /// <summary>
        /// 添加布尔条件
        /// </summary>
        public TransitionBuilder When(string parameterName, bool value = true)
        {
            _conditions.Add(new HFSMCondition
            {
                ParameterName = parameterName,
                ValueType = ConditionValueType.Bool,
                CompareType = CompareType.Equal,
                BoolValue = value
            });
            return this;
        }

        /// <summary>
        /// 添加Trigger条件
        /// </summary>
        public TransitionBuilder WhenTrigger(string parameterName)
        {
            _conditions.Add(new HFSMCondition
            {
                ParameterName = parameterName,
                ValueType = ConditionValueType.Trigger
            });
            return this;
        }

        /// <summary>
        /// 添加整数比较条件
        /// </summary>
        public TransitionBuilder WhenInt(string parameterName, CompareType compareType, int value)
        {
            _conditions.Add(new HFSMCondition
            {
                ParameterName = parameterName,
                ValueType = ConditionValueType.Int,
                CompareType = compareType,
                IntValue = value
            });
            return this;
        }

        /// <summary>
        /// 添加浮点数比较条件
        /// </summary>
        public TransitionBuilder WhenFloat(string parameterName, CompareType compareType, float value)
        {
            _conditions.Add(new HFSMCondition
            {
                ParameterName = parameterName,
                ValueType = ConditionValueType.Float,
                CompareType = compareType,
                FloatValue = value
            });
            return this;
        }

        /// <summary>
        /// 添加字符串比较条件
        /// </summary>
        public TransitionBuilder WhenString(string parameterName, string value, CompareType compareType = CompareType.Equal)
        {
            _conditions.Add(new HFSMCondition
            {
                ParameterName = parameterName,
                ValueType = ConditionValueType.String,
                CompareType = compareType,
                StringValue = value
            });
            return this;
        }

        /// <summary>
        /// 完成转换配置并返回父构建器
        /// </summary>
        public HFSMBuilder End()
        {
            HFSMTransition transition;

            if (_isAny)
            {
                transition = new HFSMAnyTransition
                {
                    TransitionName = _name,
                    ToStateName = _to,
                    Conditions = new List<HFSMCondition>(_conditions)
                };
            }
            else
            {
                transition = new HFSMTransition
                {
                    TransitionName = _name,
                    FromStateName = _from,
                    ToStateName = _to,
                    Conditions = new List<HFSMCondition>(_conditions)
                };
            }

            _parent.AddTransition(transition);
            return _parent;
        }
    }
}
