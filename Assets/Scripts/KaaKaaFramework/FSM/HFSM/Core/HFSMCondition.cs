using System;
using UnityEngine;

namespace KaaKaaFramework.HFSM
{
    /// <summary>
    /// 条件比较类型
    /// </summary>
    public enum CompareType
    {
        Equal,
        NotEqual,
        Greater,
        Less,
        GreaterOrEqual,
        LessOrEqual
    }

    /// <summary>
    /// 条件值类型
    /// </summary>
    public enum ConditionValueType
    {
        Bool,
        Int,
        Float,
        String,
        Trigger
    }

    /// <summary>
    /// HFSM转换条件
    /// </summary>
    [Serializable]
    public class HFSMCondition
    {
        public string ParameterName;
        public ConditionValueType ValueType = ConditionValueType.Bool;
        public CompareType CompareType = CompareType.Equal;
        
        // 条件值
        public bool BoolValue;
        public int IntValue;
        public float FloatValue;
        public string StringValue;

        /// <summary>
        /// 评估条件是否满足
        /// </summary>
        public bool Evaluate(HFSMBlackboard blackboard)
        {
            if (blackboard == null)
                return false;

            switch (ValueType)
            {
                case ConditionValueType.Bool:
                    return EvaluateBool(blackboard);
                case ConditionValueType.Int:
                    return EvaluateInt(blackboard);
                case ConditionValueType.Float:
                    return EvaluateFloat(blackboard);
                case ConditionValueType.String:
                    return EvaluateString(blackboard);
                case ConditionValueType.Trigger:
                    return EvaluateTrigger(blackboard);
                default:
                    return false;
            }
        }

        private bool EvaluateBool(HFSMBlackboard blackboard)
        {
            var value = blackboard.Get<bool>(ParameterName);
            return CompareType switch
            {
                CompareType.Equal => value == BoolValue,
                CompareType.NotEqual => value != BoolValue,
                _ => value == BoolValue
            };
        }

        private bool EvaluateInt(HFSMBlackboard blackboard)
        {
            var value = blackboard.Get<int>(ParameterName);
            return CompareType switch
            {
                CompareType.Equal => value == IntValue,
                CompareType.NotEqual => value != IntValue,
                CompareType.Greater => value > IntValue,
                CompareType.Less => value < IntValue,
                CompareType.GreaterOrEqual => value >= IntValue,
                CompareType.LessOrEqual => value <= IntValue,
                _ => false
            };
        }

        private bool EvaluateFloat(HFSMBlackboard blackboard)
        {
            var value = blackboard.Get<float>(ParameterName);
            return CompareType switch
            {
                CompareType.Equal => Mathf.Approximately(value, FloatValue),
                CompareType.NotEqual => !Mathf.Approximately(value, FloatValue),
                CompareType.Greater => value > FloatValue,
                CompareType.Less => value < FloatValue,
                CompareType.GreaterOrEqual => value >= FloatValue,
                CompareType.LessOrEqual => value <= FloatValue,
                _ => false
            };
        }

        private bool EvaluateString(HFSMBlackboard blackboard)
        {
            var value = blackboard.Get<string>(ParameterName, "");
            return CompareType switch
            {
                CompareType.Equal => value == StringValue,
                CompareType.NotEqual => value != StringValue,
                _ => value == StringValue
            };
        }

        private bool EvaluateTrigger(HFSMBlackboard blackboard)
        {
            var value = blackboard.Get<bool>(ParameterName);
            if (value)
            {
                // Trigger触发后自动重置
                blackboard.Set(ParameterName, false);
                return true;
            }
            return false;
        }
    }
}
