// ============================================
// 技能系统完整代码 - 所有文件合并版本
// ============================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TechCosmos.SkillSystem.Runtime
{
    // ============================================
    // 第一部分：核心接口和枚举
    // ============================================

    /// <summary>
    /// 单位的泛型接口
    /// </summary>
    public interface IUnit<T> where T : IUnit<T>
    {
        string[] GetSupportedEvents();
        void TriggerEvent(string eventName, SkillContext<T> context);
        void AddSkill(ISkill<T> skill);
        void RemoveSkill(ISkill<T> skill);
    }

    /// <summary>
    /// 技能类型枚举
    /// </summary>
    public enum SkillType
    {
        Active,
        Passive
    }

    /// <summary>
    /// 技能上下文信息
    /// </summary>
    public class SkillContext<T> where T : IUnit<T>
    {
        public T caster;
        public T target;
    }

    /// <summary>
    /// 技能接口
    /// </summary>
    public interface ISkill<T> where T : IUnit<T>
    {
        IBaseLayer<T> BaseLayer { get; }
        IConditionLayer<T> ConditionLayer { get; }
        IInformationLayer<T> InformationLayer { get; }
        IMechanismLayer<T> MechanismLayer { get; }
        IDataLayer<T> DataLayer { get; }
        IExecuteLayer<T> ExecuteLayer { get; }
    }

    // ============================================
    // 第二部分：条件系统
    // ============================================

    /// <summary>
    /// 条件基类 - 支持运算符重载进行组合
    /// </summary>
    public abstract class Condition<T> where T : IUnit<T>
    {
        public abstract bool IsEligible(SkillContext<T> skillContext);

        // 组合条件运算符
        public static Condition<T> operator &(Condition<T> left, Condition<T> right)
            => new AndCondition<T>(left, right);

        public static Condition<T> operator |(Condition<T> left, Condition<T> right)
            => new OrCondition<T>(left, right);

        public static Condition<T> operator !(Condition<T> condition)
            => new NotCondition<T>(condition);
    }

    /// <summary>
    /// AND 条件（所有条件都需要满足）
    /// </summary>
    public class AndCondition<T> : Condition<T> where T : IUnit<T>
    {
        private List<Condition<T>> _conditions;

        public AndCondition(params Condition<T>[] conditions)
        {
            _conditions = conditions.Where(c => c != null).ToList();
        }

        public override bool IsEligible(SkillContext<T> skillContext)
        {
            if (_conditions.Count == 0) return true;
            return _conditions.All(condition => condition.IsEligible(skillContext));
        }
    }

    /// <summary>
    /// OR 条件（任一条件满足即可）
    /// </summary>
    public class OrCondition<T> : Condition<T> where T : IUnit<T>
    {
        private List<Condition<T>> _conditions;

        public OrCondition(params Condition<T>[] conditions)
        {
            _conditions = conditions.Where(c => c != null).ToList();
        }

        public override bool IsEligible(SkillContext<T> skillContext)
        {
            if (_conditions.Count == 0) return true;
            return _conditions.Any(condition => condition.IsEligible(skillContext));
        }
    }

    /// <summary>
    /// NOT 条件（条件取反）
    /// </summary>
    public class NotCondition<T> : Condition<T> where T : IUnit<T>
    {
        private Condition<T> _condition;

        public NotCondition(Condition<T> condition)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public override bool IsEligible(SkillContext<T> skillContext)
            => !_condition.IsEligible(skillContext);
    }

    /// <summary>
    /// 函数式条件（可以封装 Func<SkillContext<T>, bool>）
    /// </summary>
    public class FuncCondition<T> : Condition<T> where T : IUnit<T>
    {
        private Func<SkillContext<T>, bool> _func;

        public FuncCondition(Func<SkillContext<T>, bool> func)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public override bool IsEligible(SkillContext<T> skillContext)
            => _func(skillContext);
    }

    /// <summary>
    /// 冷却时间条件实现
    /// </summary>
    public class CooldownCondition<T> : Condition<T> where T : IUnit<T>
    {
        public float cooldown;
        private float _nextAvailableTime;

        public CooldownCondition(float cooldown, SkillData<T> skillData)
        {
            this.cooldown = cooldown;
            skillData.AddMechanism(StartCooldown);
        }

        public override bool IsEligible(SkillContext<T> skillContext)
            => UnityEngine.Time.time >= _nextAvailableTime;

        public void StartCooldown(SkillContext<T> skillContext = null)
            => _nextAvailableTime = UnityEngine.Time.time + cooldown;
    }

    // ============================================
    // 第三部分：事件系统
    // ============================================

    /// <summary>
    /// 单位事件系统
    /// </summary>
    public class UnitEvent<T> where T : IUnit<T>
    {
        private Dictionary<string, Action<SkillContext<T>>> _events = new Dictionary<string, Action<SkillContext<T>>>();

        public UnitEvent(params string[] events)
        {
            foreach (var @event in events) _events[@event] = null;
        }

        public void Subscribe(string eventName, Action<SkillContext<T>> action)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning("事件名称不能为null或空字符串");
                return;
            }

            if (!_events.ContainsKey(eventName))
                _events[eventName] = null;

            _events[eventName] += action;
        }

        public void Unsubscribe(string eventName, Action<SkillContext<T>> action)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            if (_events.ContainsKey(eventName))
                _events[eventName] -= action;
        }

        public void Trigger(string eventName, SkillContext<T> skillContext)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            if (_events.ContainsKey(eventName))
                _events[eventName]?.Invoke(skillContext);
        }

        public void SubscribeMany(string[] eventNames, Action<SkillContext<T>> action)
        {
            foreach (var name in eventNames)
                Subscribe(name, action);
        }

        public int GetSubscriberCount(string eventName)
        {
            if (string.IsNullOrEmpty(eventName) || !_events.ContainsKey(eventName))
                return 0;

            return _events[eventName]?.GetInvocationList().Length ?? 0;
        }

        public bool HasEvent(string eventName) => _events.ContainsKey(eventName);

        public void ClearEvent(string eventName) => _events.Remove(eventName);
        public void ClearAllEvents() => _events.Clear();
    }

    // ============================================
    // 第四部分：数据层
    // ============================================

    /// <summary>
    /// 技能数据类
    /// </summary>
    [Serializable]
    public class SkillData<T> where T : IUnit<T>
    {
        //基础层
        public SkillType SkillType;
        public string TriggerEvent = string.Empty;

        //条件层
        public List<Condition<T>> Conditions = new();

        //信息层
        public string SkillName;
        public string SkillDescription;

        //机制层
        public List<Action<SkillContext<T>>> Mechanisms = new();

        public void AddMechanism(Action<SkillContext<T>> mechanism) => Mechanisms.Add(mechanism);
        public void AddMechanisms(Action<SkillContext<T>>[] mechanisms) => Mechanisms.AddRange(mechanisms);
        public void RemoveMechanism(Action<SkillContext<T>> mechanism) => Mechanisms.Remove(mechanism);
        public void AddCondition(Condition<T> condition) => Conditions.Add(condition);
        public void AddConditions(Condition<T>[] conditions) => Conditions.AddRange(conditions);
        public void RemoveCondition(Condition<T> condition) => Conditions.Remove(condition);
        public void ClearMechanisms() => Mechanisms.Clear();
        public void ClearConditions() => Conditions.Clear();
    }

    // ============================================
    // 第五部分：层接口定义
    // ============================================

    /// <summary>
    /// 技能层基接口
    /// </summary>
    public interface ISkillLayer<T> where T : IUnit<T>
    {
        public ISkill<T> Skill { get; set; }
    }

    /// <summary>
    /// 基础层接口
    /// </summary>
    public interface IBaseLayer<T> : ISkillLayer<T> where T : IUnit<T>
    {
        public string TriggerEvent { get; set; }
        public void Trigger(SkillContext<T> context);
    }

    /// <summary>
    /// 条件层接口
    /// </summary>
    public interface IConditionLayer<T> : ISkillLayer<T> where T : IUnit<T>
    {
        public List<Condition<T>> Conditions { get; set; }
        public bool CheckCondition(SkillContext<T> skillContext);
    }

    /// <summary>
    /// 信息层接口
    /// </summary>
    public interface IInformationLayer<T> : ISkillLayer<T> where T : IUnit<T>
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// 机制层接口
    /// </summary>
    public interface IMechanismLayer<T> : ISkillLayer<T> where T : IUnit<T>
    {
        public void Mechanism(SkillContext<T> skillContext);
        public void AddActionMechanism(Action<SkillContext<T>> action);
        public void RemoveActionMechanism(Action<SkillContext<T>> action);
        public void ClearMechanisms();
    }

    /// <summary>
    /// 数据层接口
    /// </summary>
    public interface IDataLayer<T> : ISkillLayer<T> where T : IUnit<T>
    {
        public TValue GetValue<TValue>(string key, SkillContext<T> context);
        public void SetValue<TValue>(string key, TValue value);
        public void SetFormula<TValue>(string key, Func<SkillContext<T>, TValue> formula);
    }

    /// <summary>
    /// 执行层接口
    /// </summary>
    public interface IExecuteLayer<T> : ISkillLayer<T> where T : IUnit<T>
    {
        public void Execute(SkillContext<T> skillContext);
    }

    // ============================================
    // 第六部分：层实现
    // ============================================

    /// <summary>
    /// 基础层抽象类
    /// </summary>
    public abstract class BaseLayer<T> : IBaseLayer<T> where T : IUnit<T>
    {
        public ISkill<T> Skill { get; set; }
        public string TriggerEvent { get; set; }

        public BaseLayer(string triggerEvent)
        {
            this.TriggerEvent = triggerEvent;
        }

        public virtual void Trigger(SkillContext<T> context) { }
    }

    /// <summary>
    /// 主动技能基础层
    /// </summary>
    public class ActiveBaseLayer<T> : BaseLayer<T> where T : IUnit<T>
    {
        public ActiveBaseLayer(string triggerEvent) : base(triggerEvent) { }

        public override void Trigger(SkillContext<T> skillContext)
            => Skill.ExecuteLayer.Execute(skillContext);
    }

    /// <summary>
    /// 被动技能基础层
    /// </summary>
    public class PassiveBaseLayer<T> : BaseLayer<T> where T : IUnit<T>
    {
        public PassiveBaseLayer(string triggerEvent) : base(triggerEvent) { }

        public override void Trigger(SkillContext<T> context)
            => Skill.ExecuteLayer.Execute(context);
    }

    /// <summary>
    /// 条件层实现
    /// </summary>
    public class ConditionLayer<T> : IConditionLayer<T> where T : IUnit<T>
    {
        public List<Condition<T>> Conditions { get; set; }
        public ISkill<T> Skill { get; set; }

        public bool CheckCondition(SkillContext<T> skillContext)
            => Conditions.All(s => s.IsEligible(skillContext));

        public ConditionLayer(List<Condition<T>> conditions = null)
        {
            this.Conditions = conditions ?? new List<Condition<T>>();
        }
    }

    /// <summary>
    /// 信息层实现
    /// </summary>
    public class InformationLayer<T> : IInformationLayer<T> where T : IUnit<T>
    {
        public ISkill<T> Skill { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public InformationLayer(string name = null, string description = null)
        {
            Name = name;
            Description = description;
        }
    }

    /// <summary>
    /// 机制层实现
    /// </summary>
    public class MechanismLayer<T> : IMechanismLayer<T> where T : IUnit<T>
    {
        public ISkill<T> Skill { get; set; }

        public void Mechanism(SkillContext<T> skillContext) => ActionMechanism?.Invoke(skillContext);

        private Action<SkillContext<T>> ActionMechanism { get; set; }

        public MechanismLayer(List<Action<SkillContext<T>>> actions = null)
        {
            if (actions != null)
            {
                foreach (Action<SkillContext<T>> action in actions)
                    AddActionMechanism(action);
            }
        }

        public void AddActionMechanism(Action<SkillContext<T>> action) => ActionMechanism += action;
        public void RemoveActionMechanism(Action<SkillContext<T>> action) => ActionMechanism -= action;
        public void ClearMechanisms() => ActionMechanism = null;
    }

    /// <summary>
    /// 数据层实现
    /// </summary>
    public class DataLayer<T> : IDataLayer<T> where T : IUnit<T>
    {
        private Dictionary<string, object> _data = new();
        public ISkill<T> Skill { get; set; }

        public TValue GetValue<TValue>(string key, SkillContext<T> context)
        {
            if (!_data.ContainsKey(key))
                return default(TValue);

            var value = _data[key];

            if (value is Func<SkillContext<T>, TValue> func)
                return func(context);

            return (TValue)value;
        }

        public void SetValue<TValue>(string key, TValue value) => _data[key] = value;

        public void SetFormula<TValue>(string key, Func<SkillContext<T>, TValue> formula)
            => _data[key] = formula;
    }

    /// <summary>
    /// 执行层实现
    /// </summary>
    public class ExecuteLayer<T> : IExecuteLayer<T> where T : IUnit<T>
    {
        public ISkill<T> Skill { get; set; }

        public void Execute(SkillContext<T> skillContext)
        {
            if (Skill.ConditionLayer.CheckCondition(skillContext))
                Skill.MechanismLayer.Mechanism(skillContext);
        }
    }

    // ============================================
    // 第七部分：核心类
    // ============================================

    /// <summary>
    /// 技能类 - 组合所有层
    /// </summary>
    public class Skill<T> : ISkill<T> where T : IUnit<T>
    {
        public IBaseLayer<T> BaseLayer { get; }
        public IConditionLayer<T> ConditionLayer { get; }
        public IInformationLayer<T> InformationLayer { get; }
        public IMechanismLayer<T> MechanismLayer { get; }
        public IDataLayer<T> DataLayer { get; }
        public IExecuteLayer<T> ExecuteLayer { get; }

        public Skill(
            IBaseLayer<T> baseLayer,
            IInformationLayer<T> infoLayer,
            IConditionLayer<T> conditionLayer,
            IMechanismLayer<T> mechanismLayer,
            IDataLayer<T> dataLayer,
            IExecuteLayer<T> executeLayer)
        {
            BaseLayer = baseLayer;
            InformationLayer = infoLayer;
            ConditionLayer = conditionLayer;
            MechanismLayer = mechanismLayer;
            DataLayer = dataLayer;
            ExecuteLayer = executeLayer;

            // 设置反向引用
            baseLayer.Skill = this;
            infoLayer.Skill = this;
            conditionLayer.Skill = this;
            mechanismLayer.Skill = this;
            dataLayer.Skill = this;
            executeLayer.Skill = this;
        }
    }

    /// <summary>
    /// 技能持有者 - 管理单位的技能列表
    /// </summary>
    public class SkillHolder<T> where T : IUnit<T>
    {
        private List<ISkill<T>> skillList = new();
        private UnitEvent<T> unitEvent;

        public SkillHolder(UnitEvent<T> unitEvent) => this.unitEvent = unitEvent;

        public void AddSkill(ISkill<T> skill)
        {
            unitEvent.Subscribe(skill.BaseLayer.TriggerEvent, skill.BaseLayer.Trigger);
            skillList.Add(skill);
        }

        public void RemoveSkill(ISkill<T> skill)
        {
            unitEvent.Unsubscribe(skill.BaseLayer.TriggerEvent, skill.BaseLayer.Trigger);
            skillList.Remove(skill);
        }
    }

    /// <summary>
    /// 基础机制类
    /// </summary>
    public abstract class BaseMechanism<T> where T : IUnit<T>
    {
        public SkillContext<T> Context { get; }
        public IDataLayer<T> DataLayer { get; }

        public BaseMechanism(SkillContext<T> context, IDataLayer<T> dataLayer = null)
        {
            this.Context = context;
            this.DataLayer = dataLayer;
        }
    }

    // ============================================
    // 第八部分：管理器
    // ============================================

    /// <summary>
    /// 技能管理器 - 负责创建技能
    /// </summary>
    public static class SkillManager<T> where T : IUnit<T>
    {
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
        }

        public static ISkill<T> CreateSkill(SkillData<T> data)
        {
            IBaseLayer<T> baseLayer = data.SkillType == SkillType.Passive ?
                new PassiveBaseLayer<T>(data.TriggerEvent) : new ActiveBaseLayer<T>(data.TriggerEvent);

            IConditionLayer<T> conditionLayer = new ConditionLayer<T>(data.Conditions);
            IInformationLayer<T> infoLayer = new InformationLayer<T>(data.SkillName, data.SkillDescription);
            IMechanismLayer<T> mechanismLayer = new MechanismLayer<T>(data.Mechanisms);
            IDataLayer<T> dataLayer = new DataLayer<T>();
            IExecuteLayer<T> executeLayer = new ExecuteLayer<T>();

            return new Skill<T>(baseLayer, infoLayer, conditionLayer, mechanismLayer, dataLayer, executeLayer);
        }
    }

    /// <summary>
    /// 技能系统配置
    /// </summary>
    public static class SkillSystemConfig
    {
        private static Type _currentUnitType;
        private static bool _isInitialized = false;

        public static void Initialize<T>() where T : IUnit<T>
        {
            _currentUnitType = typeof(T);
            _isInitialized = true;
            Debug.Log($"技能系统已初始化为使用类型: {_currentUnitType.Name}");
        }

        public static Type GetCurrentUnitType()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("SkillSystemConfig not initialized. Call Initialize<T>() first.");
            return _currentUnitType;
        }

        public static bool IsInitialized => _isInitialized;
    }
}