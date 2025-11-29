using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 属性组建造者 - 简化属性组的创建
/// </summary>
public class PropertyGroupBuilder
{
    private readonly PropertyHandler _handler;
    private readonly string _groupName;
    private readonly List<Action> _buildSteps = new List<Action>();

    public PropertyGroupBuilder(PropertyHandler handler, string groupName)
    {
        _handler = handler;
        _groupName = groupName;
    }

    /// <summary>
    /// 添加基础属性
    /// </summary>
    public PropertyGroupBuilder Basic(string suffix, float baseValue, string parentSuffix = null)
    {
        _buildSteps.Add(() =>
        {
            var prop = new BasicProperty<float>(baseValue, Id(suffix)).Register(_handler);
            if (!string.IsNullOrEmpty(parentSuffix))
                prop.NotifyParentOnDirty(Id(parentSuffix));
        });
        return this;
    }

    /// <summary>
    /// 添加求和计算属性: A + B + C
    /// </summary>
    public PropertyGroupBuilder Sum(string suffix, string parentSuffix, params string[] depSuffixes)
    {
        _buildSteps.Add(() =>
        {
            var depIds = ToIds(depSuffixes);
            var prop = new ComputedProperty<float>(
                () => SumValues(depIds), Id(suffix)
            ).Register(_handler);

            if (!string.IsNullOrEmpty(parentSuffix))
                prop.NotifyParentOnDirty(Id(parentSuffix));
        });
        return this;
    }

    /// <summary>
    /// 添加乘积计算属性: A * B * C
    /// </summary>
    public PropertyGroupBuilder Product(string suffix, string parentSuffix, params string[] depSuffixes)
    {
        _buildSteps.Add(() =>
        {
            var depIds = ToIds(depSuffixes);
            var prop = new ComputedProperty<float>(
                () => ProductValues(depIds), Id(suffix)
            ).Register(_handler);

            if (!string.IsNullOrEmpty(parentSuffix))
                prop.NotifyParentOnDirty(Id(parentSuffix));
        });
        return this;
    }

    /// <summary>
    /// 添加Buff倍率计算属性: (1 + A) * B
    /// </summary>
    public PropertyGroupBuilder BuffMul(string suffix, string buffSuffix, string mulSuffix, string parentSuffix = null)
    {
        _buildSteps.Add(() =>
        {
            var buffId = Id(buffSuffix);
            var mulId = Id(mulSuffix);
            var prop = new ComputedProperty<float>(
                () => (1f + GetValue(buffId)) * GetValue(mulId), Id(suffix)
            ).Register(_handler);

            if (!string.IsNullOrEmpty(parentSuffix))
                prop.NotifyParentOnDirty(Id(parentSuffix));
        });
        return this;
    }

    /// <summary>
    /// 添加固定范围限制
    /// </summary>
    public PropertyGroupBuilder Clamp(string suffix, float min, float max)
    {
        _buildSteps.Add(() =>
        {
            _handler.GetProperty(Id(suffix))?.AddModifier(new ClampModifier<float>(min, max));
        });
        return this;
    }

    /// <summary>
    /// 添加动态范围限制
    /// </summary>
    public PropertyGroupBuilder ClampDynamic(string suffix, float min, string maxRefSuffix, float maxMul = 1f)
    {
        _buildSteps.Add(() =>
        {
            var maxRefId = Id(maxRefSuffix);
            _handler.GetProperty(Id(suffix))?.AddModifier(new ClampModifier<float>(
                () => min,
                () => GetValue(maxRefId) * maxMul
            ));
        });
        return this;
    }

    /// <summary>
    /// 执行构建
    /// </summary>
    public void Build()
    {
        foreach (var step in _buildSteps)
        {
            step();
        }
    }

    // ===== 辅助方法 =====

    private string Id(string suffix)
    {
        return string.IsNullOrEmpty(suffix) ? _groupName : $"{_groupName}{suffix}";
    }

    private string[] ToIds(string[] suffixes)
    {
        var ids = new string[suffixes.Length];
        for (int i = 0; i < suffixes.Length; i++)
            ids[i] = Id(suffixes[i]);
        return ids;
    }

    private float GetValue(string id)
    {
        return _handler.GetProperty<float>(id)?.GetValue() ?? 0f;
    }

    private float SumValues(string[] ids)
    {
        float sum = 0f;
        foreach (var id in ids)
            sum += GetValue(id);
        return sum;
    }

    private float ProductValues(string[] ids)
    {
        if (ids.Length == 0) return 0f;
        float result = 1f;
        foreach (var id in ids)
            result *= GetValue(id);
        return result;
    }
}