using System;
using System.Collections.Generic;
using UnityEngine;

namespace KaaKaaFramework.HFSM
{
    /// <summary>
    /// HFSM转换
    /// </summary>
    [Serializable]
    public class HFSMTransition
    {
        public string TransitionName;
        public string FromStateName;
        public string ToStateName;
        public List<HFSMCondition> Conditions = new List<HFSMCondition>();
        
        [NonSerialized]
        public IHFSMState FromState;
        [NonSerialized]
        public IHFSMState ToState;
        [NonSerialized]
        public HFSMBlackboard Blackboard;

        /// <summary>
        /// 检查是否可以转换
        /// </summary>
        public bool CanTransition()
        {
            if (Conditions == null || Conditions.Count == 0)
                return true;

            foreach (var condition in Conditions)
            {
                if (!condition.Evaluate(Blackboard))
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// 任意状态转换（从任何状态都可以触发）
    /// </summary>
    [Serializable]
    public class HFSMAnyTransition : HFSMTransition
    {
        public HFSMAnyTransition()
        {
            FromStateName = "Any";
        }
    }
}
