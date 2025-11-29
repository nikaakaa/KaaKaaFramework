using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 抽屉（池子中的数据）对象
/// </summary>
public class PoolData
{
    // 存储未使用的对象 (用栈 Stack 也就是后进先出，拿取最新放进去的，数据热度高)
    private readonly Stack<GameObject> _dataStack = new Stack<GameObject>();

    // 存储使用中的对象
    private readonly List<GameObject> _usedList = new List<GameObject>();

    // 抽屉根对象
    private readonly GameObject _rootObj;

    // 最大容量限制
    private int _maxNum;

    // 缓存的预设体引用 (从Addressables加载来的原始资源)
    public GameObject Prefab;

    public int UnusedCount => _dataStack.Count;
    public int UsedCount => _usedList.Count;

    // 是否需要实例化新对象 (使用量 < 上限)
    public bool NeedCreate => _usedList.Count < _maxNum;

    public PoolData(GameObject root, string name)
    {
        if (PoolMgr.isOpenLayout)
        {
            _rootObj = new GameObject(name);
            _rootObj.transform.SetParent(root.transform);
        }
    }

    /// <summary>
    /// 初始化池子参数 (主要是上限)
    /// </summary>
    public void InitPoolData(GameObject initialObj)
    {
        // 记录第一个对象
        PushUsedList(initialObj);

        PoolObj poolObj = initialObj.GetComponent<PoolObj>();
        if (poolObj == null)
        {
            Debug.LogWarning($"预设体 {initialObj.name} 未挂载 PoolObj 脚本，默认上限设置为 10");
            _maxNum = 10;
        }
        else
        {
            _maxNum = poolObj.maxNum;
        }
    }

    /// <summary>
    /// 取出对象
    /// </summary>
    public GameObject Pop()
    {
        GameObject obj;

        if (UnusedCount > 0)
        {
            obj = _dataStack.Pop();
            _usedList.Add(obj);
        }
        else
        {
            // 池子空了但一定要取，就复用最早的一个 (通常是使用了 LRU 策略，这里简化为取列表第一个)
            obj = _usedList[0];
            _usedList.RemoveAt(0);
            _usedList.Add(obj);
        }

        obj.SetActive(true);
        if (PoolMgr.isOpenLayout)
            obj.transform.SetParent(null);

        return obj;
    }

    /// <summary>
    /// 回收对象
    /// </summary>
    public void Push(GameObject obj)
    {
        obj.SetActive(false);
        if (PoolMgr.isOpenLayout && _rootObj != null)
            obj.transform.SetParent(_rootObj.transform);

        _dataStack.Push(obj);
        _usedList.Remove(obj);
    }

    public void PushUsedList(GameObject obj)
    {
        _usedList.Add(obj);
    }
}

/* * 依赖脚本提示:
 * public class PoolObj : MonoBehaviour { public int maxNum = 50; }
 */

/// <summary>
/// 非Mono对象池基类
/// </summary>
public abstract class PoolObjectBase { }

public class PoolObject<T> : PoolObjectBase where T : class
{
    public Queue<T> poolObjs = new Queue<T>();
}

public interface IPoolObject
{
    void ResetInfo();
}

/// <summary>
/// 对象池管理器 (优化版)
/// </summary>
public class PoolMgr : BaseManager<PoolMgr>
{
    // GameObject 池字典
    private readonly Dictionary<string, PoolData> _poolDic = new Dictionary<string, PoolData>();
    // C# 对象池字典
    private readonly Dictionary<string, PoolObjectBase> _poolObjectDic = new Dictionary<string, PoolObjectBase>();

    private GameObject _poolRootObj;

    public static bool isOpenLayout = true;

    private PoolMgr()
    {
        CheckPoolRoot();
    }

    private void CheckPoolRoot()
    {
        if (_poolRootObj == null && isOpenLayout)
            _poolRootObj = new GameObject("Pool");
    }

    #region 核心辅助逻辑 (提取重复代码)

    /// <summary>
    /// 统一的实例化与池子初始化逻辑
    /// </summary>
    private GameObject CreateAndInitObj(string name, GameObject prefab)
    {
        GameObject obj = GameObject.Instantiate(prefab);
        obj.name = name; // 去掉(Clone)后缀

        if (!_poolDic.TryGetValue(name, out PoolData poolData))
        {
            // 创建新池子
            poolData = new PoolData(_poolRootObj, name);
            poolData.Prefab = prefab;
            poolData.InitPoolData(obj);
            _poolDic.Add(name, poolData);
        }
        else
        {
            // 补充现有池子
            if (poolData.Prefab == null) poolData.Prefab = prefab;
            poolData.PushUsedList(obj);
        }

        return obj;
    }

    /// <summary>
    /// 检查池子是否有可用对象，有则直接返回
    /// </summary>
    private GameObject TryGetFromCache(string name)
    {
        if (_poolDic.TryGetValue(name, out PoolData poolData))
        {
            // 1. 池中有闲置对象
            if (poolData.UnusedCount > 0)
                return poolData.Pop();

            // 2. 池中无闲置，但已达上限 (强制复用)
            if (!poolData.NeedCreate)
                return poolData.Pop();

            // 3. 还有余额，但Prefab已经有了，直接实例化不需要Load
            if (poolData.Prefab != null)
            {
                GameObject obj = GameObject.Instantiate(poolData.Prefab);
                obj.name = name;
                poolData.PushUsedList(obj);
                return obj;
            }
        }
        return null; // 需要加载资源
    }

    #endregion

    #region GameObject 同步加载

    /// <summary>
    /// 【同步】获取对象。
    /// 适用于小资源，注意不要频繁调用大资源的同步加载。
    /// </summary>
    public GameObject GetObjSync(string name)
    {
        CheckPoolRoot();

        // 1. 尝试从缓存获取
        GameObject cachedObj = TryGetFromCache(name);
        if (cachedObj != null) return cachedObj;

        // 2. 缓存没有，调用 AddressablesMgr 的同步加载
        // 关键修改：必须走 Manager，否则引用计数会乱
        GameObject prefab = AddressablesMgr.Instance.LoadAssetSync<GameObject>(name);

        if (prefab != null)
        {
            return CreateAndInitObj(name, prefab);
        }
        else
        {
            Debug.LogError($"[PoolMgr] 同步加载资源失败: {name}");
            return null;
        }
    }

    #endregion

    #region GameObject 异步加载

    /// <summary>
    /// 【异步】获取对象 (推荐)
    /// </summary>
    public void GetObjAsync(string name, Action<GameObject> callback)
    {
        CheckPoolRoot();

        // 1. 尝试从缓存获取
        GameObject cachedObj = TryGetFromCache(name);
        if (cachedObj != null)
        {
            callback?.Invoke(cachedObj);
            return;
        }

        // 2. 缓存没有，调用 AddressablesMgr 的异步加载
        AddressablesMgr.Instance.LoadAssetAsync<GameObject>(name, (prefab) =>
        {
            if (prefab != null)
            {
                GameObject obj = CreateAndInitObj(name, prefab);
                callback?.Invoke(obj);
            }
            else
            {
                Debug.LogError($"[PoolMgr] 异步加载资源失败: {name}");
                callback?.Invoke(null);
            }
        });
    }

    #endregion

    #region GameObject 回收

    /// <summary>
    /// 将对象还回池子
    /// </summary>
    public void PushObj(GameObject obj)
    {
        if (obj == null) return;

        if (_poolDic.TryGetValue(obj.name, out PoolData poolData))
        {
            poolData.Push(obj);
        }
        else
        {
            // 防止放错对象，直接销毁
            GameObject.Destroy(obj);
        }
    }

    #endregion

    #region C# 类对象池

    public T GetObj<T>(string nameSpace = "") where T : class, IPoolObject, new()
    {
        string poolName = $"{nameSpace}_{typeof(T).Name}";

        if (_poolObjectDic.TryGetValue(poolName, out PoolObjectBase poolBase))
        {
            PoolObject<T> pool = poolBase as PoolObject<T>;
            if (pool != null && pool.poolObjs.Count > 0)
            {
                return pool.poolObjs.Dequeue();
            }
        }
        return new T();
    }

    public void PushObj<T>(T obj, string nameSpace = "") where T : class, IPoolObject
    {
        if (obj == null) return;
        string poolName = $"{nameSpace}_{typeof(T).Name}";

        if (!_poolObjectDic.TryGetValue(poolName, out PoolObjectBase poolBase))
        {
            poolBase = new PoolObject<T>();
            _poolObjectDic.Add(poolName, poolBase);
        }

        PoolObject<T> pool = poolBase as PoolObject<T>;
        if (pool != null)
        {
            obj.ResetInfo();
            pool.poolObjs.Enqueue(obj);
        }
    }

    #endregion

    #region 清理

    /// <summary>
    /// 清理指定池子并释放资源引用
    /// </summary>
    public void ClearPool(string name)
    {
        if (_poolDic.TryGetValue(name, out PoolData poolData))
        {
            // 通知 AddressablesMgr 释放引用计数
            // 只有当引用计数为0时，底层资源才会真正卸载
            AddressablesMgr.Instance.Release<GameObject>(name);
            _poolDic.Remove(name);
        }
    }

    /// <summary>
    /// 清理所有池子
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var key in _poolDic.Keys)
        {
            AddressablesMgr.Instance.Release<GameObject>(key);
        }

        _poolDic.Clear();
        _poolObjectDic.Clear();

        if (_poolRootObj != null)
        {
            GameObject.Destroy(_poolRootObj);
            _poolRootObj = null;
        }
    }

    #endregion
}