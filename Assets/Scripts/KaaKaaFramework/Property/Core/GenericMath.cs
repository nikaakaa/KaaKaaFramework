using System;
using UnityEngine;

// 这些委托是为了在 ProccessModifiers 让泛型T支持加法,乘法操作
public delegate T AddOperation<T>(T left, T right);
public delegate T MultiplyOperation<T>(T left, T right);
public delegate T ClampOperation<T>(T value, T min, T max);
/// <summary>
/// 让属性泛型T支持数学操作的静态类
/// </summary>
/// <typeparam name="T"></typeparam>
public static class GenericMath<T>
{
    // 存储操作委托的静态字段
    public static AddOperation<T> Add { get; set; }
    public static MultiplyOperation<T> Multiply { get; set; }
    public static ClampOperation<T> Clamp { get; set; }

    public static void ThrowIfNotInitialized()
    {
        if (Add == null || Multiply == null || Clamp == null)
        {
            MathInitialization.Initialize();
        }
        if (Add == null || Multiply == null || Clamp == null)
        {
            throw new InvalidOperationException($"GenericMath<{typeof(T).Name}> has not been initialized. Call Initialize() for this type at startup.");
        }
    }
}
/// <summary>
/// 负责初始化 GenericMath<T> 静态类中所有常用类型的数学运算委托。
/// 必须在任何 BaseProperty<T> 实例使用前调用 Initialize()。
/// </summary>
public static class MathInitialization
{
    public static void Initialize()
    {
        GenericMath<int>.Add = (a, b) => a + b;
        GenericMath<int>.Multiply = (a, b) => a * b;
        GenericMath<int>.Clamp = (val, min, max) =>
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        };

        GenericMath<float>.Add = (a, b) => a + b;
        GenericMath<float>.Multiply = (a, b) => a * b;
        GenericMath<float>.Clamp = (val, min, max) => Mathf.Clamp(val, min, max);

        GenericMath<double>.Add = (a, b) => a + b;
        GenericMath<double>.Multiply = (a, b) => a * b;
        GenericMath<double>.Clamp = (val, min, max) => System.Math.Max(min, System.Math.Min(max, val));

        GenericMath<Vector3>.Add = (a, b) => a + b;
        GenericMath<Vector3>.Multiply = (a, b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        GenericMath<Vector3>.Clamp = (val, min, max) =>
        {
            return new Vector3(
                Mathf.Clamp(val.x, min.x, max.x),
                Mathf.Clamp(val.y, min.y, max.y),
                Mathf.Clamp(val.z, min.z, max.z)
            );
        };
        Debug.Log("MathInitialization: Generic math operations initialized for int, float, double, and Vector3.");
    }
}
