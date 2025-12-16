using System;
using System.Collections.Generic;
using UnityEngine;

namespace KaaKaaFramework.HFSM
{
    /// <summary>
    /// HFSM参数定义
    /// </summary>
    [Serializable]
    public class HFSMParameter
    {
        public string Name;
        public ConditionValueType Type = ConditionValueType.Bool;
        public bool DefaultBoolValue;
        public int DefaultIntValue;
        public float DefaultFloatValue;
        public string DefaultStringValue;

        /// <summary>
        /// 应用默认值到黑板
        /// </summary>
        public void ApplyDefault(HFSMBlackboard blackboard)
        {
            switch (Type)
            {
                case ConditionValueType.Bool:
                case ConditionValueType.Trigger:
                    blackboard.Set(Name, DefaultBoolValue);
                    break;
                case ConditionValueType.Int:
                    blackboard.Set(Name, DefaultIntValue);
                    break;
                case ConditionValueType.Float:
                    blackboard.Set(Name, DefaultFloatValue);
                    break;
                case ConditionValueType.String:
                    blackboard.Set(Name, DefaultStringValue);
                    break;
            }
        }
    }

    /// <summary>
    /// HFSM状态配置数据
    /// </summary>
    [Serializable]
    public class HFSMStateData
    {
        public string StateName;
        public string StateType; // 状态类型的完全限定名
        public bool IsSubStateMachine;
        public HFSMConfigAsset SubStateMachineConfig; // 如果是子状态机，引用子状态机配置
        public Vector2 EditorPosition; // 编辑器中的位置（用于可视化编辑器）
    }

    /// <summary>
    /// HFSM转换配置数据
    /// </summary>
    [Serializable]
    public class HFSMTransitionData
    {
        public string TransitionName;
        public string FromStateName;
        public string ToStateName;
        public bool IsAnyTransition;
        public List<HFSMCondition> Conditions = new List<HFSMCondition>();
    }

    /// <summary>
    /// HFSM配置资源 - 数据驱动的核心
    /// </summary>
    [CreateAssetMenu(fileName = "NewHFSMConfig", menuName = "KaaKaaFramework/HFSM/Config", order = 0)]
    public class HFSMConfigAsset : ScriptableObject
    {
        [Header("状态机设置")]
        public string StateMachineName = "Root";
        public string DefaultStateName;

        [Header("参数定义")]
        public List<HFSMParameter> Parameters = new List<HFSMParameter>();

        [Header("状态配置")]
        public List<HFSMStateData> States = new List<HFSMStateData>();

        [Header("转换配置")]
        public List<HFSMTransitionData> Transitions = new List<HFSMTransitionData>();

        /// <summary>
        /// 根据配置创建状态机实例
        /// </summary>
        public HierarchicalStateMachine CreateStateMachine()
        {
            var fsm = new HierarchicalStateMachine(StateMachineName);
            var blackboard = new HFSMBlackboard();

            // 应用参数默认值
            foreach (var param in Parameters)
            {
                param.ApplyDefault(blackboard);
            }

            fsm.Initialize(blackboard);

            // 创建状态
            foreach (var stateData in States)
            {
                IHFSMState state;

                if (stateData.IsSubStateMachine && stateData.SubStateMachineConfig != null)
                {
                    // 递归创建子状态机
                    state = stateData.SubStateMachineConfig.CreateStateMachine();
                }
                else
                {
                    // 通过反射创建状态实例
                    state = CreateStateInstance(stateData);
                }

                if (state != null)
                {
                    fsm.AddState(state);
                }
            }

            // 设置默认状态
            if (!string.IsNullOrEmpty(DefaultStateName))
            {
                fsm.SetDefaultState(DefaultStateName);
            }

            // 创建转换
            foreach (var transData in Transitions)
            {
                HFSMTransition transition;

                if (transData.IsAnyTransition)
                {
                    transition = new HFSMAnyTransition
                    {
                        TransitionName = transData.TransitionName,
                        ToStateName = transData.ToStateName,
                        Conditions = new List<HFSMCondition>(transData.Conditions)
                    };
                }
                else
                {
                    transition = new HFSMTransition
                    {
                        TransitionName = transData.TransitionName,
                        FromStateName = transData.FromStateName,
                        ToStateName = transData.ToStateName,
                        Conditions = new List<HFSMCondition>(transData.Conditions)
                    };
                }

                fsm.AddTransition(transition);
            }

            // 重新初始化以确保所有引用正确
            fsm.Initialize(blackboard);

            return fsm;
        }

        /// <summary>
        /// 通过反射创建状态实例
        /// </summary>
        private IHFSMState CreateStateInstance(HFSMStateData stateData)
        {
            if (string.IsNullOrEmpty(stateData.StateType))
            {
                // 如果没有指定类型，创建一个默认的空状态
                return new EmptyHFSMState(stateData.StateName);
            }

            try
            {
                var type = Type.GetType(stateData.StateType);
                if (type == null)
                {
                    Debug.LogError($"[HFSM] Cannot find state type: {stateData.StateType}");
                    return new EmptyHFSMState(stateData.StateName);
                }

                // 尝试使用带名称参数的构造函数
                var constructor = type.GetConstructor(new Type[] { typeof(string) });
                if (constructor != null)
                {
                    return constructor.Invoke(new object[] { stateData.StateName }) as IHFSMState;
                }

                // 使用无参构造函数
                return Activator.CreateInstance(type) as IHFSMState;
            }
            catch (Exception e)
            {
                Debug.LogError($"[HFSM] Failed to create state instance: {stateData.StateType}, Error: {e.Message}");
                return new EmptyHFSMState(stateData.StateName);
            }
        }

        #region Editor Helpers

        /// <summary>
        /// 获取所有状态名称（编辑器用）
        /// </summary>
        public List<string> GetAllStateNames()
        {
            var names = new List<string>();
            foreach (var state in States)
            {
                names.Add(state.StateName);
            }
            return names;
        }

        /// <summary>
        /// 获取所有参数名称（编辑器用）
        /// </summary>
        public List<string> GetAllParameterNames()
        {
            var names = new List<string>();
            foreach (var param in Parameters)
            {
                names.Add(param.Name);
            }
            return names;
        }

        #endregion
    }

    /// <summary>
    /// 空状态（用于占位）
    /// </summary>
    public class EmptyHFSMState : HFSMState
    {
        public EmptyHFSMState(string name) : base(name) { }
    }
}
