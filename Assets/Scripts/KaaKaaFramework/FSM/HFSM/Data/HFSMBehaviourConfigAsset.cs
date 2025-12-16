using System;
using System.Collections.Generic;
using UnityEngine;

namespace KaaKaaFramework.HFSM
{
    /// <summary>
    /// HFSM状态行为引用 - 支持使用ScriptableObject作为状态
    /// </summary>
    [Serializable]
    public class HFSMStateReference
    {
        public string StateName;
        public HFSMStateBehaviour StateBehaviour;
        public bool UseRuntimeInstance = true; // 是否在运行时创建实例
    }

    /// <summary>
    /// 扩展的HFSM配置资源 - 支持ScriptableObject状态
    /// </summary>
    [CreateAssetMenu(fileName = "NewHFSMBehaviourConfig", menuName = "KaaKaaFramework/HFSM/Behaviour Config", order = 1)]
    public class HFSMBehaviourConfigAsset : ScriptableObject
    {
        [Header("状态机设置")]
        public string StateMachineName = "Root";
        public string DefaultStateName;

        [Header("参数定义")]
        public List<HFSMParameter> Parameters = new List<HFSMParameter>();

        [Header("状态行为引用")]
        public List<HFSMStateReference> StateReferences = new List<HFSMStateReference>();

        [Header("转换配置")]
        public List<HFSMTransitionData> Transitions = new List<HFSMTransitionData>();

        /// <summary>
        /// 根据配置创建状态机实例
        /// </summary>
        public HierarchicalStateMachine CreateStateMachine(GameObject owner = null)
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
            foreach (var stateRef in StateReferences)
            {
                if (stateRef.StateBehaviour == null)
                {
                    Debug.LogWarning($"[HFSM] State behaviour is null for: {stateRef.StateName}");
                    continue;
                }

                HFSMStateBehaviour state;
                if (stateRef.UseRuntimeInstance)
                {
                    state = stateRef.StateBehaviour.CreateRuntimeInstance();
                }
                else
                {
                    state = stateRef.StateBehaviour;
                }

                state.Owner = owner;
                fsm.AddState(state);
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
    }
}
