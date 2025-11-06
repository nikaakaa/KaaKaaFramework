using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 基于 ScriptableObject 的服务定位器
/// 可以在 Inspector 中查看和配置服务
/// 支持运行时动态注册和注销服务
/// </summary>
[CreateAssetMenu(fileName = "ServiceLocator", menuName = "KaaKaaFramework/ServiceLocator", order = 1)]
public class ServiceLocatorSO : ScriptableObject
{
    // 运行时服务字典，用于快速查找
    // 注意：服务是运行时注册的，不进行序列化
    private Dictionary<Type, object> serviceDictionary = new Dictionary<Type, object>();

    /// <summary>
    /// 服务是否已初始化
    /// </summary>
    public bool IsInitialized => serviceDictionary.Count > 0;

    /// <summary>
    /// 获取已注册的服务数量
    /// </summary>
    public int ServiceCount => serviceDictionary.Count;

    private void OnEnable()
    {
        // 在启用时清空字典（服务需要在运行时重新注册）
        // 这样可以避免序列化问题，服务都是运行时注册的
        if (!Application.isPlaying)
        {
            serviceDictionary.Clear();
        }
    }

    /// <summary>
    /// 注册服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="service">服务实例</param>
    /// <returns>是否注册成功</returns>
    public bool Register<T>(T service) where T : class
    {
        if (service == null)
        {
            Debug.LogError($"[ServiceLocatorSO] 尝试注册空服务: {typeof(T).Name}");
            return false;
        }

        Type serviceType = typeof(T);

        // 检查是否已注册
        if (serviceDictionary.ContainsKey(serviceType))
        {
            Debug.LogWarning($"[ServiceLocatorSO] 服务已注册: {serviceType.Name}，将覆盖原有服务");
            Unregister<T>();
        }

        // 添加到字典
        serviceDictionary[serviceType] = service;

        // 如果实现了 IService 接口，调用初始化
        if (service is IService serviceInterface)
        {
            serviceInterface.Initialize();
        }

        Debug.Log($"[ServiceLocatorSO] 服务注册成功: {serviceType.Name}");
        return true;
    }

    /// <summary>
    /// 注销服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>是否注销成功</returns>
    public bool Unregister<T>() where T : class
    {
        Type serviceType = typeof(T);

        if (!serviceDictionary.ContainsKey(serviceType))
        {
            Debug.LogWarning($"[ServiceLocatorSO] 服务未注册: {serviceType.Name}");
            return false;
        }

        var service = serviceDictionary[serviceType];

        // 如果实现了 IService 接口，调用清理
        if (service is IService serviceInterface)
        {
            serviceInterface.Cleanup();
        }

        // 从字典移除
        serviceDictionary.Remove(serviceType);

        Debug.Log($"[ServiceLocatorSO] 服务注销成功: {serviceType.Name}");
        return true;
    }

    /// <summary>
    /// 获取服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例，如果未注册则返回 null</returns>
    public T Get<T>() where T : class
    {
        Type serviceType = typeof(T);

        if (serviceDictionary.TryGetValue(serviceType, out object service))
        {
            return service as T;
        }

        Debug.LogWarning($"[ServiceLocatorSO] 服务未找到: {serviceType.Name}");
        return null;
    }

    /// <summary>
    /// 检查服务是否已注册
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>是否已注册</returns>
    public bool IsRegistered<T>() where T : class
    {
        return serviceDictionary.ContainsKey(typeof(T));
    }

    /// <summary>
    /// 清空所有服务
    /// </summary>
    public void Clear()
    {
        // 清理所有实现了 IService 的服务
        foreach (var service in serviceDictionary.Values)
        {
            if (service is IService serviceInterface)
            {
                serviceInterface.Cleanup();
            }
        }

        serviceDictionary.Clear();
        Debug.Log("[ServiceLocatorSO] 所有服务已清空");
    }

    /// <summary>
    /// 获取所有已注册的服务类型（用于调试）
    /// </summary>
    /// <returns>服务类型列表</returns>
    public List<Type> GetAllServiceTypes()
    {
        return new List<Type>(serviceDictionary.Keys);
    }

    /// <summary>
    /// 获取所有已注册的服务类型名称（用于 Inspector 显示）
    /// </summary>
    /// <returns>服务类型名称列表</returns>
    public List<string> GetAllServiceTypeNames()
    {
        var names = new List<string>();
        foreach (var type in serviceDictionary.Keys)
        {
            names.Add(type.Name);
        }
        return names;
    }
}

