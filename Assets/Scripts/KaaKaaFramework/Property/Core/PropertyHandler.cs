using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public interface IPropertyHandler
{
    public Dictionary<string, IProperty> PropertyDict { get; }
    public void RegisterProperty(IProperty property);
    public IProperty GetProperty(string propertyName);
    public IProperty<T> GetProperty<T>(string propertyName);
}
public class PropertyHandler : MonoBehaviour,IPropertyHandler
{
    private readonly Dictionary<string, IProperty> propertyDict = new();
    public Dictionary<string, IProperty> PropertyDict => propertyDict;
    public void RegisterProperty(IProperty property)
    {
        propertyDict[property.PropertyName] = property;
    }
    public IProperty GetProperty(string propertyName)
    {
        return propertyDict.TryGetValue(propertyName, out var prop) ? prop : null;
    }
    public IProperty<T> GetProperty<T>(string propertyName)
    {
        return GetProperty(propertyName) as IProperty<T>;
    }
}
public enum E_PropertyModifierType
{
    Additive,
    Multiplicative,
    Clamp,
    Override,
}
public interface IPropertyModifier
{
    public E_PropertyModifierType ModifierType { get; }
    public int Priority { get; }
}
public interface IPropertyModifier<T> : IPropertyModifier
{
    
}
public interface IPropertyClampModifier<T> : IPropertyModifier<T>
{
    T Min { get; }
    T Max { get; }
}
public interface IPropertyAdditiveModifier<T> : IPropertyModifier<T>
{
    T AddValue { get; }
}
public interface IPropertyMultiplicativeModifier<T> : IPropertyModifier<T>
{
    T MultiValue { get;  }
}
public interface IPropertyOverrideModifier<T> : IPropertyModifier<T>
{
    T OverrideValue { get; }
}
public class AdditiveModifier<T> : IPropertyAdditiveModifier<T>
{
    private readonly Func<T> valueGetter;

    public AdditiveModifier(T value, int priority = 0)
    {
        valueGetter = () => value;
        Priority = priority;
    }

    // 动态值构造函数
    public AdditiveModifier(Func<T> valueGetter, int priority = 0)
    {
        this.valueGetter = valueGetter;
        Priority = priority;
    }

    public E_PropertyModifierType ModifierType => E_PropertyModifierType.Additive;
    public int Priority { get; }
    public T AddValue => valueGetter();
}
public class MultiplicativeModifier<T> : IPropertyMultiplicativeModifier<T>
{
    private readonly Func<T> valueGetter;

    public MultiplicativeModifier(T value, int priority = 0)
    {
        valueGetter = () => value;
        Priority = priority;
    }

    public MultiplicativeModifier(Func<T> valueGetter, int priority = 0)
    {
        this.valueGetter = valueGetter;
        Priority = priority;
    }

    public E_PropertyModifierType ModifierType => E_PropertyModifierType.Multiplicative;
    public int Priority { get; }

    public T MultiValue => valueGetter();
}
public class ClampModifier<T> : IPropertyClampModifier<T>
{
    private readonly Func<T> minGetter;
    private readonly Func<T> maxGetter;

    public ClampModifier(T min, T max, int priority = 0)
    {
        minGetter = () => min;
        maxGetter = () => max;
        Priority = priority;
    }

    public ClampModifier(Func<T> minGetter, Func<T> maxGetter, int priority = 0)
    {
        this.minGetter = minGetter;
        this.maxGetter = maxGetter;
        Priority = priority;
    }

    public E_PropertyModifierType ModifierType => E_PropertyModifierType.Clamp;
    public int Priority { get; }
    public T Min => minGetter();
    public T Max => maxGetter();
}
public class OverrideModifier<T> : IPropertyOverrideModifier<T>
{
    private readonly Func<T> valueGetter;

    public OverrideModifier(T value, int priority = 0)
    {
        valueGetter = () => value;
        Priority = priority;
    }

    public OverrideModifier(Func<T> valueGetter, int priority = 0)
    {
        this.valueGetter = valueGetter;
        Priority = priority;
    }

    public E_PropertyModifierType ModifierType => E_PropertyModifierType.Override;
    public int Priority { get; }
    public T OverrideValue => valueGetter();
}
public interface IProperty
{
    public string PropertyName { get; }
    public PropertyHandler PropertyHandler { get; }
    public IReadOnlyList<IPropertyModifier> Modifiers { get; }
    public IProperty Register(PropertyHandler handler);
    public IProperty AddModifier(IPropertyModifier modifier);
    public IProperty RemoveModifier(IPropertyModifier modifier);
    public IProperty NotifyParentOnDirty(string parentPropertyName);
    public void SetDirty();
    public void OnDirty();

}
public interface IProperty<T> : IProperty
{
    public T GetValue();
    public Func<T> GetValueGetter();
    public void ApplyModify(ref T value);
    public void ProccessModifiers(ref T value, E_PropertyModifierType modifiersType);
}
public class BasicProperty<T> : IProperty<T>
{
    public BasicProperty(T value, string propertyName)
    {
        BaseValue = value;
        PropertyName = propertyName;
    }
    public string PropertyName { get; }

    private PropertyHandler propertyHandler;
    public PropertyHandler PropertyHandler => propertyHandler;

    private readonly List<IPropertyModifier> modifiers = new();
    public IReadOnlyList<IPropertyModifier> Modifiers => modifiers;

    private bool isDirty = true;
    private Action onDirtyCallBack = null;

    public T BaseValue { get; }
    private T cachedValue;


    public IProperty AddModifier(IPropertyModifier modifier)
    {
        modifiers.Add(modifier);
        SetDirty();
        return this;
    }
    public IProperty RemoveModifier(IPropertyModifier modifier)
    {
        modifiers.Remove(modifier);
        SetDirty();
        return this;
    }
    public IProperty Register(PropertyHandler handler)
    {
        propertyHandler = handler;
        handler.RegisterProperty(this);
        return this;
    }

    public IProperty NotifyParentOnDirty(string parentPropertyName)
    {
        onDirtyCallBack += () =>
        {
            propertyHandler.GetProperty(parentPropertyName)?.SetDirty();
        };
        return this;
    }
    public T GetValue()
    {
        if (!isDirty) return cachedValue;
        cachedValue = BaseValue;
        ApplyModify(ref cachedValue);
        isDirty = false;
        return cachedValue;
    }

    public void ApplyModify(ref T value)
    {
        ProccessModifiers(ref value, E_PropertyModifierType.Additive);
        ProccessModifiers(ref value, E_PropertyModifierType.Multiplicative);
        ProccessModifiers(ref value, E_PropertyModifierType.Clamp);
        ProccessModifiers(ref value, E_PropertyModifierType.Override);
    }
    public void ProccessModifiers(ref T value, E_PropertyModifierType modifiersType)
    {
        GenericMath<T>.ThrowIfNotInitialized();
        var sortedModifiers = Modifiers
            .Where(m => m.ModifierType == modifiersType)
            .OrderBy(m => m.Priority);
        var valueModifiers = sortedModifiers.OfType<IPropertyModifier>();

        switch (modifiersType)
        {
            case E_PropertyModifierType.Additive:
                value = valueModifiers
                    .OfType<IPropertyAdditiveModifier<T>>()
                    .Aggregate(value, (current, mod) => GenericMath<T>.Add(current, mod.AddValue));
                break;

            case E_PropertyModifierType.Multiplicative:
                value = valueModifiers
                    .OfType<IPropertyMultiplicativeModifier<T>>()
                    .Aggregate(value, (current, mod) => GenericMath<T>.Multiply(current, mod.MultiValue));
                break;

            case E_PropertyModifierType.Clamp:
                value = sortedModifiers
                    .OfType<IPropertyClampModifier<T>>()
                    .Aggregate(value, (current, mod) => GenericMath<T>.Clamp(current, mod.Min, mod.Max));
                break;

            case E_PropertyModifierType.Override:
                var lastOverride = valueModifiers
                    .OfType<IPropertyOverrideModifier<T>>()
                    .LastOrDefault();
                if (lastOverride != null)
                {
                    value = lastOverride.OverrideValue;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(modifiersType), modifiersType, null);
        }
    }
    public Func<T> GetValueGetter()
    {
        return GetValue;
    }

    public void OnDirty()
    {
        onDirtyCallBack?.Invoke();
    }
    public void SetDirty()
    {
        isDirty = true;
        OnDirty();
    }
}
public class ComputedProperty<T> : IProperty<T>
{
    public ComputedProperty(Func<T> valueGetter, string propertyName)
    {
        this.valueGetter = valueGetter;
        PropertyName = propertyName;
    }

    public string PropertyName { get; }

    private PropertyHandler propertyHandler;
    public PropertyHandler PropertyHandler => propertyHandler;

    private readonly List<IPropertyModifier> modifiers = new();
    public IReadOnlyList<IPropertyModifier> Modifiers => modifiers;

    private readonly Func<T> valueGetter;
    private bool isDirty = true;
    private T cachedValue;
    private Action onDirtyCallback;

    public IProperty AddModifier(IPropertyModifier modifier)
    {
        // 计算属性只支持 Clamp 和 Override
        if (modifier.ModifierType == E_PropertyModifierType.Additive ||
            modifier.ModifierType == E_PropertyModifierType.Multiplicative)
        {
            Debug.LogWarning($"ComputedProperty '{PropertyName}' 不支持 Additive/Multiplicative 修改器");
            return this;
        }
        modifiers.Add(modifier);
        SetDirty();
        return this;
    }

    public IProperty RemoveModifier(IPropertyModifier modifier)
    {
        modifiers.Remove(modifier);
        SetDirty();
        return this;
    }

    public IProperty Register(PropertyHandler handler)
    {
        propertyHandler = handler;
        handler.RegisterProperty(this);
        return this;
    }
    public IProperty NotifyParentOnDirty(string parentPropertyName)
    {
        onDirtyCallback += () =>
        {
            propertyHandler?.GetProperty(parentPropertyName)?.SetDirty();
        };
        return this;
    }
    public T GetValue()
    {
        if (!isDirty) return cachedValue;
        cachedValue = valueGetter();
        ApplyModify(ref cachedValue);
        isDirty = false;
        return cachedValue;
    }

    public Func<T> GetValueGetter() => GetValue;

    public void ApplyModify(ref T value)
    {
        // 计算属性只处理 Clamp 和 Override
        ProccessModifiers(ref value, E_PropertyModifierType.Clamp);
        ProccessModifiers(ref value, E_PropertyModifierType.Override);
    }

    public void ProccessModifiers(ref T value, E_PropertyModifierType modifiersType)
    {
        GenericMath<T>.ThrowIfNotInitialized();

        var sortedModifiers = modifiers
            .Where(m => m.ModifierType == modifiersType)
            .OrderBy(m => m.Priority);

        switch (modifiersType)
        {
            case E_PropertyModifierType.Clamp:
                value = sortedModifiers
                    .OfType<IPropertyClampModifier<T>>()
                    .Aggregate(value, (current, mod) => GenericMath<T>.Clamp(current, mod.Min, mod.Max));
                break;

            case E_PropertyModifierType.Override:
                var lastOverride = sortedModifiers
                    .OfType<IPropertyOverrideModifier<T>>()
                    .LastOrDefault();
                if (lastOverride != null)
                {
                    value = lastOverride.OverrideValue;
                }
                break;
        }
    }

    public void SetDirty()
    {
        isDirty = true;
        OnDirty();
    }

    public void OnDirty()
    {
        onDirtyCallback?.Invoke();
    }
}