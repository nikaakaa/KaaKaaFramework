using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 属性组配置
/// </summary>
[CreateAssetMenu(fileName = "PropertyGroupConfig", menuName = "PropertySystem/PropertyGroupConfig")]
public class PropertyGroupConfig : ScriptableObject
{
    [Tooltip("属性组名称，如 MoveSpeed、Attack")]
    public string groupName = "NewProperty";

    [Tooltip("属性节点列表")]
    public List<PropertyNodeConfig> nodes = new List<PropertyNodeConfig>();
}

/// <summary>
/// 单个属性节点配置
/// </summary>
[Serializable]
public class PropertyNodeConfig
{
    [Tooltip("属性ID后缀，完整ID = 组名 + 后缀")]
    public string suffix = "";

    [Tooltip("是否为计算属性")]
    public bool isComputed = false;

    [Tooltip("基础值（仅基础属性有效）")]
    public float baseValue = 0f;

    [Tooltip("计算公式类型")]
    public FormulaType formula = FormulaType.Sum;

    [Tooltip("依赖的属性后缀列表")]
    public List<string> dependencies = new List<string>();

    [Tooltip("父属性后缀（用于脏标记通知）")]
    public string parentSuffix = "";

    [Header("限制配置")]
    public bool hasClamp = false;
    public float clampMin = 0f;
    public float clampMax = 100f;

    [Tooltip("使用动态上限")]
    public bool useDynamicMax = false;
    public string dynamicMaxSuffix = "";
    public float dynamicMaxMultiplier = 1f;
}

public enum FormulaType
{
    Sum,            // A + B + C
    Product,        // A * B * C
    BuffMul,        // (1 + A) * B
}

/// <summary>
/// 属性预设 - 常用属性结构的快速创建
/// </summary>
public static class PropertyPresets
{
    /// <summary>
    /// 创建标准属性（完整结构）
    /// 结构: Config + Buff + Other -> Value, MulBuff + MulOther -> Mul, Value * Mul -> Final
    /// </summary>
    public static void Standard(PropertyHandler handler, string name, float baseValue,
        float otherMax = 50f, float mulBuffRange = 0.5f, float mulOtherMax = 1.5f)
    {
        new PropertyGroupBuilder(handler, name)
            // 基础属性
            .Basic("-Value-Config", baseValue, "-Value")
            .Basic("-Value-Buff", 0f, "-Value")
            .Basic("-Value-Other", 0f, "-Value")
            .Basic("-Mul-Buff", 0f, "-Mul")
            .Basic("-Mul-Other", 1f, "-Mul")
            // 计算属性
            .Sum("-Value", "", "-Value-Config", "-Value-Buff", "-Value-Other")
            .BuffMul("-Mul", "-Mul-Buff", "-Mul-Other", "")
            .Product("", null, "-Value", "-Mul")
            // 限制
            .Clamp("-Value-Other", -999f, otherMax)
            .Clamp("-Mul-Buff", -1f, mulBuffRange)
            .Clamp("-Mul-Other", 0f, mulOtherMax)
            .ClampDynamic("-Value", 0f, "-Value-Config", 2f)
            .Build();
    }

    /// <summary>
    /// 创建简单属性（基础值 + Buff）
    /// </summary>
    public static void Simple(PropertyHandler handler, string name, float baseValue,
        float min = 0f, float max = 999f)
    {
        new PropertyGroupBuilder(handler, name)
            .Basic("-Base", baseValue, "")
            .Basic("-Buff", 0f, "")
            .Sum("", null, "-Base", "-Buff")
            .Clamp("", min, max)
            .Build();
    }

    /// <summary>
    /// 创建百分比属性（0-100%）
    /// </summary>
    public static void Percent(PropertyHandler handler, string name, float basePercent = 0f)
    {
        new PropertyGroupBuilder(handler, name)
            .Basic("-Base", basePercent, "")
            .Basic("-Bonus", 0f, "")
            .Sum("", null, "-Base", "-Bonus")
            .Clamp("", 0f, 100f)
            .Build();
    }

    /// <summary>
    /// 创建单一属性（只有一个值，可添加修改器）
    /// </summary>
    public static void Single(PropertyHandler handler, string name, float baseValue)
    {
        new BasicProperty<float>(baseValue, name).Register(handler);
    }
}