using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ServerLocator : BaseManager<ServerLocator>
{
    private ServerLocator() { }

    private Dictionary<Type, object> _services = new();
    public void Register<T>(T serverInstance)
    {
        if(_services.ContainsKey(typeof(T)))
        {
            Debug.Log("已经存在");
            return;
        }
        _services.Add(typeof(T), serverInstance);
    }
    public T Resolve<T>()
    {
        if(_services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }
        else
        {
            Debug.Log($"没有注册{typeof(T)}服务");
            return default;
        }
    }
}
