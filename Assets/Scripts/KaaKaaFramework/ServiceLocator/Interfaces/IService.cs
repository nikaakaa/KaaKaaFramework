using System;

/// <summary>
/// 服务接口，所有注册到服务定位器的服务都应实现此接口
/// 提供统一的初始化和清理方法
/// </summary>
public interface IService
{
    /// <summary>
    /// 服务初始化方法
    /// 在服务注册后调用，用于初始化服务
    /// </summary>
    void Initialize();

    /// <summary>
    /// 服务清理方法
    /// 在服务注销前调用，用于清理资源
    /// </summary>
    void Cleanup();

    /// <summary>
    /// 服务是否已初始化
    /// </summary>
    bool IsInitialized { get; }
}

