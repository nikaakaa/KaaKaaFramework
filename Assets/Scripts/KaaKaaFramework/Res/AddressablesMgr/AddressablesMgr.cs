using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Addressables 资源管理器 (优化版)
/// </summary>
public class AddressablesMgr : BaseManager<AddressablesMgr>
{
    // 缓存字典：存储 AsyncOperationHandle (结构体)，避免装箱
    private readonly Dictionary<string, AsyncOperationHandle> _resDic = new Dictionary<string, AsyncOperationHandle>();

    // 字符串构建器缓存，用于减少GC
    private readonly StringBuilder _sb = new StringBuilder();

    private AddressablesMgr() { }

    #region 内部辅助方法

    /// <summary>
    /// 生成单个资源的缓存Key
    /// </summary>
    private string GetKey<T>(string name)
    {
        return $"{name}_{typeof(T).Name}";
    }

    /// <summary>
    /// 生成资源列表的缓存Key
    /// </summary>
    private string GetKeys<T>(IEnumerable<string> keys)
    {
        _sb.Clear();
        foreach (var key in keys)
        {
            _sb.Append(key).Append('_');
        }
        _sb.Append(typeof(T).Name);
        return _sb.ToString();
    }

    #endregion

    #region 异步加载单个资源

    /// <summary>
    /// 异步加载单个资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="name">资源地址名称</param>
    /// <param name="callback">加载完成后的回调</param>
    public void LoadAssetAsync<T>(string name, Action<T> callback)
    {
        string keyName = GetKey<T>(name);

        // 1. 检查缓存
        if (_resDic.TryGetValue(keyName, out AsyncOperationHandle handle))
        {
            // 将无泛型句柄转换为泛型句柄
            AsyncOperationHandle<T> tHandle = handle.Convert<T>();

            if (tHandle.IsDone)
            {
                if (tHandle.Status == AsyncOperationStatus.Succeeded)
                    callback?.Invoke(tHandle.Result);
            }
            else
            {
                // 如果正在加载中，订阅完成事件
                tHandle.Completed += (op) =>
                {
                    if (op.Status == AsyncOperationStatus.Succeeded)
                        callback?.Invoke(op.Result);
                };
            }
            return;
        }

        // 2. 如果未加载，发起新请求
        // 关键点：Addressables 内部自动处理异步，不需要 StartCoroutine
        var newHandle = Addressables.LoadAssetAsync<T>(name);

        // 立即加入字典，防止同一帧多次调用导致的重复加载
        _resDic.Add(keyName, newHandle);

        newHandle.Completed += (op) =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                callback?.Invoke(op.Result);
            }
            else
            {
                Debug.LogError($"[AddressablesMgr] 资源加载失败: {name} (Key: {keyName})");
                // 加载失败，从缓存移除并释放句柄
                if (_resDic.ContainsKey(keyName))
                {
                    _resDic.Remove(keyName);
                    Addressables.Release(op);
                }
            }
        };
    }

    #endregion

    #region 同步加载

    /// <summary>
    /// 同步加载单个资源 (注意：可能会卡顿主线程)
    /// </summary>
    public T LoadAssetSync<T>(string name)
    {
        string keyName = GetKey<T>(name);

        if (_resDic.TryGetValue(keyName, out AsyncOperationHandle handle))
        {
            var tHandle = handle.Convert<T>();
            if (!tHandle.IsDone) tHandle.WaitForCompletion(); // 强制等待
            return tHandle.Status == AsyncOperationStatus.Succeeded ? tHandle.Result : default;
        }

        var newHandle = Addressables.LoadAssetAsync<T>(name);
        newHandle.WaitForCompletion(); // 阻塞等待

        if (newHandle.Status == AsyncOperationStatus.Succeeded)
        {
            _resDic.Add(keyName, newHandle);
            return newHandle.Result;
        }
        else
        {
            Debug.LogError($"[AddressablesMgr] 同步加载失败: {name}");
            Addressables.Release(newHandle); // 失败也要释放
            return default;
        }
    }

    #endregion

    #region 资源释放与清理

    /// <summary>
    /// 释放单个资源
    /// </summary>
    public void Release<T>(string name)
    {
        string keyName = GetKey<T>(name);
        if (_resDic.TryGetValue(keyName, out AsyncOperationHandle handle))
        {
            // 必须先 Release，再 Remove
            Addressables.Release(handle);
            _resDic.Remove(keyName);
        }
    }

    /// <summary>
    /// 释放资源组
    /// </summary>
    public void Release<T>(params string[] keys)
    {
        if (keys == null || keys.Length == 0) return;
        List<string> list = new List<string>(keys);
        string keyName = GetKeys<T>(list);

        if (_resDic.TryGetValue(keyName, out AsyncOperationHandle handle))
        {
            Addressables.Release(handle);
            _resDic.Remove(keyName);
        }
    }

    /// <summary>
    /// 清空所有资源
    /// </summary>
    public void Clear()
    {
        // 1. 遍历并释放所有句柄
        foreach (var handle in _resDic.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        // 2. 清空字典
        _resDic.Clear();
        _sb.Clear();

        // 3. 清理内存 (不建议调用 UnloadAllAssetBundles，这很危险)
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    #endregion
}