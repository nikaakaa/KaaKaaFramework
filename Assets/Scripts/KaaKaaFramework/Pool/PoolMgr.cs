using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations; // Addressables 必需的命名空间

/// <summary>
/// 抽屉（池子中的数据）对象
/// </summary>
public class PoolData
{
    // 用来存储抽屉中的对象 记录的是没有使用的对象
    private Stack<GameObject> dataStack = new Stack<GameObject>();

    // 用来记录使用中的对象的 
    private List<GameObject> usedList = new List<GameObject>();

    // 抽屉上限 场景上同时存在的对象的上限个数
    private int maxNum;

    // 抽屉根对象 用来进行布局管理的对象
    private GameObject rootObj;

    // -- 核心改动 --
    // 用于缓存从Addressables加载的Prefab
    public GameObject prefab;

    // 获取容器中是否有对象
    public int Count => dataStack.Count;

    public int UsedCount => usedList.Count;

    /// <summary>
    /// 进行使用中对象数量和最大容量进行比较 小于返回true 需要实例化
    /// </summary>
    public bool NeedCreate => usedList.Count < maxNum;

    /// <summary>
    /// 初始化构造函数
    /// </summary>
    /// <param name="root">柜子（缓存池）父对象</param>
    /// <param name="name">抽屉父对象的名字</param>
    public PoolData(GameObject root, string name)
    {
        if (PoolMgr.isOpenLayout)
        {
            rootObj = new GameObject(name);
            rootObj.transform.SetParent(root.transform);
        }
    }

    /// <summary>
    /// 初始化池子数据，特别是最大数量
    /// </summary>
    public void InitPoolData(GameObject initialObj)
    {
        // 记录第一个被实例化的对象
        PushUsedList(initialObj);

        // 通过第一个对象获取其PoolObj脚本来设置池子上限
        // 注意：你的预设体上需要挂载一个类似PoolObj的脚本来定义其在池中的最大数量
        PoolObj poolObj = initialObj.GetComponent<PoolObj>();
        if (poolObj == null)
        {
            Debug.LogError("请为使用缓存池功能的预设体对象挂载PoolObj脚本 用于设置数量上限: " + initialObj.name);
            maxNum = 10; // 给一个默认值避免出错
            return;
        }
        maxNum = poolObj.maxNum;
    }

    /// <summary>
    /// 从抽屉中弹出数据对象
    /// </summary>
    public GameObject Pop()
    {
        GameObject obj;

        if (Count > 0)
        {
            obj = dataStack.Pop();
            usedList.Add(obj);
        }
        else // 如果池里没东西了，就复用一个已经激活的
        {
            obj = usedList[0];
            usedList.RemoveAt(0);
            usedList.Add(obj);
        }

        obj.SetActive(true);
        if (PoolMgr.isOpenLayout)
            obj.transform.SetParent(null);

        return obj;
    }

    /// <summary>
    /// 将物体放入到抽屉对象中
    /// </summary>
    public void Push(GameObject obj)
    {
        obj.SetActive(false);
        if (PoolMgr.isOpenLayout)
            obj.transform.SetParent(rootObj.transform);
        dataStack.Push(obj);
        usedList.Remove(obj);
    }

    /// <summary>
    /// 将对象压入到使用中的容器中记录
    /// </summary>
    public void PushUsedList(GameObject obj)
    {
        usedList.Add(obj);
    }
}

/*
 * 注意: 为了让上面的 InitPoolData 方法正常工作,
 * 你需要在你的游戏对象预设体 (Prefab) 上挂载一个类似下面这样的脚本：
 * * public class PoolObj : MonoBehaviour
 * {
 * public int maxNum = 10; // 定义此对象在池中的最大数量
 * }
 * */


/// <summary>
/// 方便在字典当中用里式替换原则 存储子类对象
/// </summary>
public abstract class PoolObjectBase { }

/// <summary>
/// 用于存储 数据结构类 和 逻辑类 （不继承mono的）容器类
/// </summary>
public class PoolObject<T> : PoolObjectBase where T : class
{
    public Queue<T> poolObjs = new Queue<T>();
}

/// <summary>
/// 想要被复用的 数据结构类、逻辑类 都必须要继承该接口
/// </summary>
public interface IPoolObject
{
    /// <summary>
    /// 重置数据的方法
    /// </summary>
    void ResetInfo();
}


/// <summary>
/// 缓存池(对象池)模块 管理器 (已兼容Addressables)
/// </summary>
public class PoolMgr : BaseManager<PoolMgr>
{
    // GameObject 对象池
    private Dictionary<string, PoolData> poolDic = new Dictionary<string, PoolData>();
    // C# 类对象池
    private Dictionary<string, PoolObjectBase> poolObjectDic = new Dictionary<string, PoolObjectBase>();
    // 池子根对象
    private GameObject poolObj;
    // 是否开启布局功能
    public static bool isOpenLayout = true;

    private PoolMgr()
    {
        if (poolObj == null && isOpenLayout)
            poolObj = new GameObject("Pool");
    }

    #region --- 【新增】同步获取 GameObject 方法 ---

    /// <summary>
    /// 【同步】从对象池获取一个GameObject。
    /// 【警告】此方法会阻塞主线程直到资源加载完成，可能导致卡顿！请谨慎使用！
    /// </summary>
    /// <param name="name">对象（预设体）的Addressable Key</param>
    /// <returns>获取到的GameObject实例，如果加载失败则返回null</returns>
    public GameObject GetObjSync(string name)
    {
        // 确保根对象存在
        if (poolObj == null && isOpenLayout)
            poolObj = new GameObject("Pool");

        // 情况1 & 2：池子可用，直接从中获取
        if (poolDic.ContainsKey(name) && (poolDic[name].Count > 0 || !poolDic[name].NeedCreate))
        {
            return poolDic[name].Pop();
        }

        // 情况3：需要加载或实例化一个新对象
        // 检查Prefab是否已加载
        if (poolDic.ContainsKey(name) && poolDic[name].prefab != null)
        {
            // Prefab已经加载过了，直接用它来实例化
            GameObject obj = GameObject.Instantiate(poolDic[name].prefab);
            obj.name = name;
            poolDic[name].PushUsedList(obj);
            return obj;
        }
        else
        {
            // Prefab没加载过，需要通过Addressables同步加载
            // 这是阻塞主线程的操作！
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(name);
            GameObject prefab = handle.WaitForCompletion(); // 等待加载完成

            if (handle.Status == AsyncOperationStatus.Succeeded && prefab != null)
            {
                // 实例化新对象
                GameObject obj = GameObject.Instantiate(prefab);
                obj.name = name;

                // 如果池子是第一次创建
                if (!poolDic.ContainsKey(name))
                {
                    PoolData newPoolData = new PoolData(poolObj, name);
                    newPoolData.prefab = prefab;
                    newPoolData.InitPoolData(obj);
                    poolDic.Add(name, newPoolData);
                }
                else // 池子已存在
                {
                    poolDic[name].prefab = prefab;
                    poolDic[name].PushUsedList(obj);
                }

                // 注意：同步加载时，资源句柄需要手动管理释放。
                // AddressablesMgr通常会处理这个问题，但这里我们是独立加载的。
                // 一个简单的策略是让Addressables的引用计数系统自动管理，
                // 并在ClearPool时调用Release释放。

                return obj;
            }
            else
            {
                Debug.LogError($"[PoolMgr-Sync] Failed to load asset from Addressables: {name}");
                Addressables.Release(handle); // 加载失败，释放句柄
                return null;
            }
        }
    }

    #endregion
    #region --- GameObject 对象池 (已兼容 Addressables) ---

    /// <summary>
    /// 异步地从对象池获取一个GameObject
    /// </summary>
    /// <param name="name">对象（预设体）的Addressable Key</param>
    /// <param name="callback">获取到对象后执行的回调</param>
    public void GetObjAsync(string name, Action<GameObject> callback)
    {
        if (poolObj == null && isOpenLayout)
            poolObj = new GameObject("Pool");

        // 情况1：池子已存在，并且池中有未使用的对象
        if (poolDic.ContainsKey(name) && poolDic[name].Count > 0)
        {
            GameObject obj = poolDic[name].Pop();
            callback?.Invoke(obj);
            return;
        }

        // 情况2：池子已存在，但池中无对象，且已达到场景中最大数量 (复用已激活对象)
        if (poolDic.ContainsKey(name) && !poolDic[name].NeedCreate)
        {
            GameObject obj = poolDic[name].Pop();
            callback?.Invoke(obj);
            return;
        }

        // 情况3：需要加载并实例化一个新对象
        // 检查Prefab是否已加载
        if (poolDic.ContainsKey(name) && poolDic[name].prefab != null)
        {
            // Prefab已经加载过了，直接用它来实例化
            GameObject obj = GameObject.Instantiate(poolDic[name].prefab);
            obj.name = name; // 移除(Clone)后缀
            poolDic[name].PushUsedList(obj);
            callback?.Invoke(obj);
        }
        else
        {
            // Prefab没加载过，需要通过Addressables异步加载
            AddressablesMgr.Instance.LoadAssetAsync<GameObject>(name, (handle) =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject prefab = handle.Result;
                    GameObject obj = GameObject.Instantiate(prefab);
                    obj.name = name;

                    // 如果池子是第一次创建
                    if (!poolDic.ContainsKey(name))
                    {
                        PoolData newPoolData = new PoolData(poolObj, name);
                        newPoolData.prefab = prefab; // 缓存加载到的Prefab
                        newPoolData.InitPoolData(obj); // 初始化池子信息（如数量上限）
                        poolDic.Add(name, newPoolData);
                    }
                    else // 池子已存在，只是在补充对象
                    {
                        poolDic[name].prefab = prefab; // 也缓存一下
                        poolDic[name].PushUsedList(obj);
                    }

                    callback?.Invoke(obj);
                }
                else
                {
                    Debug.LogError($"[PoolMgr] Failed to load asset from Addressables: {name}");
                    callback?.Invoke(null);
                }
            });
        }
    }

    /// <summary>
    /// 【新增】使用协程从对象池获取一个GameObject
    /// 此方法在内部启动一个协程来处理异步加载
    /// </summary>
    /// <param name="name">对象（预设体）的Addressable Key</param>
    /// <param name="callback">获取到对象后执行的回调</param>
    public void GetObjByCoroutine(string name, Action<GameObject> callback)
    {
        // 确保根对象存在
        if (poolObj == null && isOpenLayout)
            poolObj = new GameObject("Pool");

        // 情况1：池子已存在，并且池中有未使用的对象
        if (poolDic.ContainsKey(name) && poolDic[name].Count > 0)
        {
            GameObject obj = poolDic[name].Pop();
            callback?.Invoke(obj);
            return;
        }

        // 情况2：池子已存在，但池中无对象，且已达到场景中最大数量 (复用已激活对象)
        if (poolDic.ContainsKey(name) && !poolDic[name].NeedCreate)
        {
            GameObject obj = poolDic[name].Pop();
            callback?.Invoke(obj);
            return;
        }

        // 情况3：需要加载并实例化一个新对象
        // 检查Prefab是否已加载
        if (poolDic.ContainsKey(name) && poolDic[name].prefab != null)
        {
            // Prefab已经加载过了，直接用它来实例化
            GameObject obj = GameObject.Instantiate(poolDic[name].prefab);
            obj.name = name; // 移除(Clone)后缀
            poolDic[name].PushUsedList(obj);
            callback?.Invoke(obj);
        }
        else
        {
            // Prefab没加载过，需要通过 AddressablesMgr 的协程方法进行异步加载
            AddressablesMgr.Instance.LoadAssetCoroutine<GameObject>(name, (handle) =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject prefab = handle.Result;
                    GameObject obj = GameObject.Instantiate(prefab);
                    obj.name = name;

                    // 如果池子是第一次创建
                    if (!poolDic.ContainsKey(name))
                    {
                        PoolData newPoolData = new PoolData(poolObj, name);
                        newPoolData.prefab = prefab; // 缓存加载到的Prefab
                        newPoolData.InitPoolData(obj); // 初始化池子信息（如数量上限）
                        poolDic.Add(name, newPoolData);
                    }
                    else // 池子已存在，只是在补充对象
                    {
                        poolDic[name].prefab = prefab; // 也缓存一下
                        poolDic[name].PushUsedList(obj);
                    }

                    callback?.Invoke(obj);
                }
                else
                {
                    Debug.LogError($"[PoolMgr] Failed to load asset from Addressables: {name}");
                    callback?.Invoke(null);
                }
            });
        }
    }


    /// <summary>
    /// 将GameObject放回池中
    /// </summary>
    public void PushObj(GameObject obj)
    {
        if (obj == null) return;

        if (poolDic.ContainsKey(obj.name))
        {
            poolDic[obj.name].Push(obj);
        }
        else
        {
            Debug.LogWarning($"[PoolMgr] Trying to push an object '{obj.name}' to a non-existent pool. Destroying it instead.");
            GameObject.Destroy(obj);
        }
    }

    #endregion

    #region --- C# 类对象池 (非MonoBehaviour) ---

    /// <summary>
    /// 获取自定义的数据结构类和逻辑类对象
    /// </summary>
    public T GetObj<T>(string nameSpace = "") where T : class, IPoolObject, new()
    {
        string poolName = nameSpace + "_" + typeof(T).Name;
        if (poolObjectDic.ContainsKey(poolName))
        {
            PoolObject<T> pool = poolObjectDic[poolName] as PoolObject<T>;
            if (pool.poolObjs.Count > 0)
            {
                return pool.poolObjs.Dequeue() as T;
            }
        }
        return new T();
    }

    /// <summary>
    /// 将自定义数据结构类和逻辑类 放入池子中
    /// </summary>
    public void PushObj<T>(T obj, string nameSpace = "") where T : class, IPoolObject
    {
        if (obj == null) return;
        string poolName = nameSpace + "_" + typeof(T).Name;

        if (!poolObjectDic.TryGetValue(poolName, out PoolObjectBase poolBase))
        {
            poolBase = new PoolObject<T>();
            poolObjectDic.Add(poolName, poolBase);
        }

        PoolObject<T> pool = poolBase as PoolObject<T>;
        obj.ResetInfo();
        pool.poolObjs.Enqueue(obj);
    }

    #endregion

    #region --- 清理和释放 ---

    /// <summary>
    /// 清理指定的单个GameObject对象池，并释放其Addressable资源
    /// </summary>
    /// <param name="name">要清理的池子的名字 (Addressable Key)</param>
    public void ClearPool(string name)
    {
        if (poolDic.TryGetValue(name, out PoolData poolData))
        {
            // 释放对应的Addressable资源
            // 注意：这里假设AddressablesMgr有正确的引用计数，或者你知道这是唯一使用该资源的地方
            if (poolData.prefab != null)
            {
                AddressablesMgr.Instance.Release<GameObject>(name);
            }
            poolDic.Remove(name);
        }
    }

    /// <summary>
    /// 清理所有对象池 (通常在切换场景时调用)
    /// </summary>
    public void ClearAllPools()
    {
        // 遍历所有GameObject池子，释放它们缓存的Addressable资源
        foreach (var pair in poolDic)
        {
            if (pair.Value.prefab != null)
            {
                AddressablesMgr.Instance.Release<GameObject>(pair.Key);
            }
        }

        poolDic.Clear();
        poolObjectDic.Clear();

        // 销毁场景中的池子根对象
        if (poolObj != null)
        {
            GameObject.Destroy(poolObj);
            poolObj = null;
        }
    }

    #endregion
}