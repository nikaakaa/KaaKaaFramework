using System;
using UnityEngine;

/// <summary>
/// 服务定位器静态访问类
/// 提供全局统一的访问入口
/// 封装对 ServiceLocatorSO 的访问
/// </summary>
public static class ServiceLocator
{
    private static ServiceLocatorSO instance;

    /// <summary>
    /// 当前使用的 ServiceLocatorSO 实例
    /// 如果未设置，会在 Resources 中查找
    /// </summary>
    public static ServiceLocatorSO Instance
    {
        get
        {
            if (instance == null)
            {
                // 尝试从 Resources 加载
                instance = Resources.Load<ServiceLocatorSO>("ServiceLocator");
                
                if (instance == null)
                {
                    Debug.LogWarning("[ServiceLocator] 未找到 ServiceLocatorSO 资源，请确保在 Resources 文件夹中有 ServiceLocator 资源，或手动设置实例");
                }
            }

            return instance;
        }
        set
        {
            instance = value;
            if (value != null)
            {
                Debug.Log("[ServiceLocator] ServiceLocatorSO 实例已设置");
            }
        }
    }

    /// <summary>
    /// 注册服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="service">服务实例</param>
    /// <returns>是否注册成功</returns>
    public static bool Register<T>(T service) where T : class
    {
        if (Instance == null)
        {
            Debug.LogError("[ServiceLocator] ServiceLocatorSO 实例未设置，无法注册服务");
            return false;
        }

        return Instance.Register(service);
    }

    /// <summary>
    /// 注销服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>是否注销成功</returns>
    public static bool Unregister<T>() where T : class
    {
        if (Instance == null)
        {
            Debug.LogError("[ServiceLocator] ServiceLocatorSO 实例未设置，无法注销服务");
            return false;
        }

        return Instance.Unregister<T>();
    }

    /// <summary>
    /// 获取服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例，如果未注册则返回 null</returns>
    public static T Get<T>() where T : class
    {
        if (Instance == null)
        {
            Debug.LogWarning($"[ServiceLocator] ServiceLocatorSO 实例未设置，无法获取服务: {typeof(T).Name}");
            return null;
        }

        return Instance.Get<T>();
    }

    /// <summary>
    /// 检查服务是否已注册
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>是否已注册</returns>
    public static bool IsRegistered<T>() where T : class
    {
        if (Instance == null)
        {
            return false;
        }

        return Instance.IsRegistered<T>();
    }

    /// <summary>
    /// 清空所有服务
    /// </summary>
    public static void Clear()
    {
        if (Instance != null)
        {
            Instance.Clear();
        }
    }

    /// <summary>
    /// 初始化服务定位器（可选）
    /// 可以在游戏启动时调用，确保所有服务都已注册
    /// </summary>
    public static void Initialize()
    {
        if (Instance == null)
        {
            Debug.LogWarning("[ServiceLocator] ServiceLocatorSO 实例未设置，无法初始化");
            return;
        }

        Debug.Log($"[ServiceLocator] 服务定位器已初始化，当前注册服务数: {Instance.ServiceCount}");
    }
}

