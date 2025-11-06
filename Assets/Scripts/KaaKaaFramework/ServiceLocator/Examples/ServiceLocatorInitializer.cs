using UnityEngine;

/// <summary>
/// 服务定位器初始化器
/// 在游戏启动时自动注册所有管理器到服务定位器
/// 可以挂载到场景中的任意 GameObject 上
/// </summary>
public class ServiceLocatorInitializer : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("是否在 Start 时自动注册所有管理器")]
    public bool autoRegisterOnStart = true;

    [Tooltip("是否在 Awake 时初始化服务定位器")]
    public bool initializeOnAwake = true;

    [Tooltip("ServiceLocatorSO 资源（可选，如果不设置则从 Resources 加载）")]
    public ServiceLocatorSO serviceLocatorSO;

    private void Awake()
    {
        if (initializeOnAwake)
        {
            InitializeServiceLocator();
        }
    }

    private void Start()
    {
        if (autoRegisterOnStart)
        {
            RegisterAllManagers();
        }
    }

    /// <summary>
    /// 初始化服务定位器
    /// </summary>
    public void InitializeServiceLocator()
    {
        // 如果指定了 SO 实例，使用它；否则从 Resources 加载
        if (serviceLocatorSO != null)
        {
            ServiceLocator.Instance = serviceLocatorSO;
            Debug.Log("[ServiceLocatorInitializer] 使用指定的 ServiceLocatorSO 实例");
        }
        else
        {
            // 尝试从 Resources 加载
            var loadedSO = Resources.Load<ServiceLocatorSO>("ServiceLocator");
            if (loadedSO != null)
            {
                ServiceLocator.Instance = loadedSO;
                Debug.Log("[ServiceLocatorInitializer] 从 Resources 加载 ServiceLocatorSO");
            }
            else
            {
                Debug.LogWarning("[ServiceLocatorInitializer] 未找到 ServiceLocatorSO 资源，请确保在 Resources 文件夹中有 ServiceLocator 资源，或手动设置 serviceLocatorSO");
            }
        }

        ServiceLocator.Initialize();
    }

    /// <summary>
    /// 注册所有管理器
    /// </summary>
    public void RegisterAllManagers()
    {
        if (ServiceLocator.Instance == null)
        {
            Debug.LogError("[ServiceLocatorInitializer] ServiceLocatorSO 未初始化，无法注册管理器");
            return;
        }

        Debug.Log("[ServiceLocatorInitializer] 开始注册所有管理器...");
        ServiceManagerAdapter.RegisterAllManagers();
        Debug.Log($"[ServiceLocatorInitializer] 注册完成，当前注册服务数: {ServiceLocator.Instance.ServiceCount}");
    }

    /// <summary>
    /// 手动注册单个管理器
    /// </summary>
    public void RegisterManager<T>() where T : class
    {
        ServiceManagerAdapter.RegisterSingleton<T>();
    }

    /// <summary>
    /// 清空所有服务（用于测试或场景切换）
    /// </summary>
    public void ClearAllServices()
    {
        ServiceLocator.Clear();
        Debug.Log("[ServiceLocatorInitializer] 所有服务已清空");
    }
}

