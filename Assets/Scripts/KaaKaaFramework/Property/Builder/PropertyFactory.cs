using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 属性配置工厂 - 从配置创建属性
/// </summary>
public static class PropertyFactory
{
    /// <summary>
    /// 从配置创建属性组
    /// </summary>
    public static void CreateFromConfig(PropertyHandler handler, PropertyGroupConfig config,
        Dictionary<string, float> overrides = null)
    {
        var groupName = config.groupName;

        // 第一遍：创建所有属性
        foreach (var node in config.nodes)
        {
            var id = GetId(groupName, node.suffix);

            if (node.isComputed)
            {
                var depIds = GetDepIds(groupName, node.dependencies);
                var formula = CreateFormula(handler, node.formula, depIds);
                new ComputedProperty<float>(formula, id).Register(handler);
            }
            else
            {
                var baseValue = node.baseValue;
                if (overrides != null && overrides.TryGetValue(id, out var val))
                    baseValue = val;
                new BasicProperty<float>(baseValue, id).Register(handler);
            }
        }

        // 第二遍：设置依赖和限制
        foreach (var node in config.nodes)
        {
            var id = GetId(groupName, node.suffix);
            var prop = handler.GetProperty(id);
            if (prop == null) continue;

            // 父属性通知
            if (!string.IsNullOrEmpty(node.parentSuffix))
            {
                prop.NotifyParentOnDirty(GetId(groupName, node.parentSuffix));
            }

            // 限制
            if (node.hasClamp)
            {
                if (node.useDynamicMax)
                {
                    var maxRefId = GetId(groupName, node.dynamicMaxSuffix);
                    var mul = node.dynamicMaxMultiplier;
                    prop.AddModifier(new ClampModifier<float>(
                        () => node.clampMin,
                        () => (handler.GetProperty<float>(maxRefId)?.GetValue() ?? 0f) * mul
                    ));
                }
                else
                {
                    prop.AddModifier(new ClampModifier<float>(node.clampMin, node.clampMax));
                }
            }
        }
    }

    private static string GetId(string groupName, string suffix)
    {
        return string.IsNullOrEmpty(suffix) ? groupName : $"{groupName}{suffix}";
    }

    private static string[] GetDepIds(string groupName, List<string> suffixes)
    {
        var ids = new string[suffixes.Count];
        for (int i = 0; i < suffixes.Count; i++)
            ids[i] = GetId(groupName, suffixes[i]);
        return ids;
    }

    private static Func<float> CreateFormula(PropertyHandler handler, FormulaType type, string[] depIds)
    {
        return type switch
        {
            FormulaType.Sum => () =>
            {
                float sum = 0f;
                foreach (var id in depIds)
                    sum += handler.GetProperty<float>(id)?.GetValue() ?? 0f;
                return sum;
            }
            ,
            FormulaType.Product => () =>
            {
                float result = 1f;
                foreach (var id in depIds)
                    result *= handler.GetProperty<float>(id)?.GetValue() ?? 1f;
                return result;
            }
            ,
            FormulaType.BuffMul => () =>
            {
                if (depIds.Length < 2) return 1f;
                var buff = handler.GetProperty<float>(depIds[0])?.GetValue() ?? 0f;
                var mul = handler.GetProperty<float>(depIds[1])?.GetValue() ?? 1f;
                return (1f + buff) * mul;
            }
            ,
            _ => () => 0f
        };
    }
}