using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ServerLocator
{
    private static Dictionary<Type, object> _services = new();
    public static void Register<T>(T serverInstance)
    {
        if(_services.ContainsKey(typeof(T)))
        {
            Debug.Log("已经存在");
            return;
        }
        _services.Add(typeof(T), serverInstance);
    }
    public static T Resolve<T>()
    {
        if(_services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }
        else
        {
            throw new Exception($"没有注册类型为 {typeof(T)} 的服务");
        }
    }
}
