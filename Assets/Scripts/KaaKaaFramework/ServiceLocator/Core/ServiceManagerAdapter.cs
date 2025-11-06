using System;
using UnityEngine;

/// <summary>
/// 服务管理器适配器
/// 用于将现有的单例管理器适配到服务定位器
/// 提供便捷的注册方法
/// </summary>
public static class ServiceManagerAdapter
{
    /// <summary>
    /// 注册单例管理器到服务定位器
    /// 自动从单例的 Instance 属性获取实例
    /// </summary>
    /// <typeparam name="T">管理器类型</typeparam>
    /// <returns>是否注册成功</returns>
    public static bool RegisterSingleton<T>() where T : class
    {
        try
        {
            // 尝试通过反射获取 Instance 属性
            var type = typeof(T);
            var instanceProperty = type.GetProperty("Instance", 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.Static);

            if (instanceProperty == null)
            {
                Debug.LogError($"[ServiceManagerAdapter] 类型 {type.Name} 没有 Instance 静态属性");
                return false;
            }

            var instance = instanceProperty.GetValue(null) as T;
            
            if (instance == null)
            {
                Debug.LogWarning($"[ServiceManagerAdapter] 类型 {type.Name} 的 Instance 为 null，可能尚未初始化");
                // 尝试访问 Instance 以触发初始化（如果是懒加载）
                instance = instanceProperty.GetValue(null) as T;
            }

            if (instance != null)
            {
                return ServiceLocator.Register(instance);
            }

            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ServiceManagerAdapter] 注册单例时发生错误: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 注册所有常见的单例管理器到服务定位器
    /// 这是一个便捷方法，用于快速注册框架中的管理器
    /// </summary>
    public static void RegisterAllManagers()
    {
        Debug.Log("[ServiceManagerAdapter] 开始注册所有管理器...");

        // 注册框架中的管理器
        // 注意：这些管理器需要先初始化（访问 Instance 属性）
        RegisterManager<UIMgr>();
        RegisterManager<PoolMgr>();
        RegisterManager<EventCenter>();
        RegisterManager<MusicMgr>();
        RegisterManager<AddressablesMgr>();
        RegisterManager<MonoMgr>();

        Debug.Log("[ServiceManagerAdapter] 所有管理器注册完成");
    }

    /// <summary>
    /// 注册单个管理器（内部方法）
    /// </summary>
    private static void RegisterManager<T>() where T : class
    {
        try
        {
            if (RegisterSingleton<T>())
            {
                Debug.Log($"[ServiceManagerAdapter] 成功注册: {typeof(T).Name}");
            }
            else
            {
                Debug.LogWarning($"[ServiceManagerAdapter] 注册失败: {typeof(T).Name}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ServiceManagerAdapter] 注册 {typeof(T).Name} 时发生错误: {e.Message}");
        }
    }
}

