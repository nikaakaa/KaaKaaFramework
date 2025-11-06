using UnityEngine;

/// <summary>
/// 服务定位器使用示例
/// 展示如何使用服务定位模式访问管理器
/// </summary>
public class ServiceLocatorExample : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("如果为 true，将使用服务定位器；否则使用单例模式")]
    public bool useServiceLocator = true;

    private void Start()
    {
        if (useServiceLocator)
        {
            UseServiceLocator();
        }
        else
        {
            UseSingleton();
        }
    }

    /// <summary>
    /// 使用服务定位器模式访问管理器
    /// </summary>
    private void UseServiceLocator()
    {
        Debug.Log("=== 使用服务定位器模式 ===");

        // 1. 注册管理器（通常在游戏启动时执行一次）
        // 方法一：手动注册
        ServiceLocator.Register(UIMgr.Instance);
        ServiceLocator.Register(PoolMgr.Instance);

        // 方法二：使用适配器自动注册所有管理器
        // ServiceManagerAdapter.RegisterAllManagers();

        // 2. 使用服务定位器获取服务
        var uiMgr = ServiceLocator.Get<UIMgr>();
        if (uiMgr != null)
        {
            Debug.Log($"成功通过服务定位器获取 UIMgr: {uiMgr.GetType().Name}");
            // 使用 uiMgr...
        }

        var poolMgr = ServiceLocator.Get<PoolMgr>();
        if (poolMgr != null)
        {
            Debug.Log($"成功通过服务定位器获取 PoolMgr: {poolMgr.GetType().Name}");
            // 使用 poolMgr...
        }

        // 3. 检查服务是否已注册
        if (ServiceLocator.IsRegistered<UIMgr>())
        {
            Debug.Log("UIMgr 已注册");
        }

        // 4. 注销服务（如果需要）
        // ServiceLocator.Unregister<UIMgr>();
    }

    /// <summary>
    /// 使用传统单例模式访问管理器
    /// </summary>
    private void UseSingleton()
    {
        Debug.Log("=== 使用传统单例模式 ===");

        // 直接使用单例访问
        var uiMgr = UIMgr.Instance;
        var poolMgr = PoolMgr.Instance;

        Debug.Log($"通过单例获取 UIMgr: {uiMgr.GetType().Name}");
        Debug.Log($"通过单例获取 PoolMgr: {poolMgr.GetType().Name}");
    }

    /// <summary>
    /// 演示如何创建自定义服务并注册
    /// </summary>
    private void RegisterCustomService()
    {
        // 创建自定义服务
        var customService = new CustomGameService();
        
        // 注册到服务定位器
        ServiceLocator.Register(customService);
        
        // 获取服务
        var service = ServiceLocator.Get<CustomGameService>();
        if (service != null)
        {
            Debug.Log("自定义服务已成功注册并获取");
        }
    }
}

/// <summary>
/// 自定义服务示例
/// 实现 IService 接口以支持初始化/清理
/// </summary>
public class CustomGameService : IService
{
    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        Debug.Log("[CustomGameService] 初始化");
        IsInitialized = true;
        // 执行初始化逻辑...
    }

    public void Cleanup()
    {
        Debug.Log("[CustomGameService] 清理");
        IsInitialized = false;
        // 执行清理逻辑...
    }

    public void DoSomething()
    {
        Debug.Log("[CustomGameService] 执行某些操作");
    }
}

