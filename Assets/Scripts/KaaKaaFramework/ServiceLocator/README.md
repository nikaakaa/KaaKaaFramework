# 服务定位模式使用指南

## 概述

服务定位模式（Service Locator Pattern）是一种设计模式，用于解耦服务的使用者和提供者。本框架提供了基于 ScriptableObject 的服务定位器实现，可以作为单例模式的可选替代方案。

## 特性

- ✅ **可视化配置**：通过 ScriptableObject 在 Inspector 中查看和配置服务
- ✅ **运行时动态注册**：支持在运行时注册和注销服务
- ✅ **向后兼容**：与现有单例模式完全兼容，可以共存
- ✅ **类型安全**：使用泛型提供类型安全的访问
- ✅ **生命周期管理**：支持实现 IService 接口进行初始化和清理

## 快速开始

### 1. 创建 ServiceLocator 资源

1. 在 Unity 编辑器中，右键点击 Project 窗口
2. 选择 `Create > KaaKaaFramework > ServiceLocator`
3. 将创建的 `ServiceLocator` 资源放入 `Resources` 文件夹（或手动设置实例）

### 2. 注册服务

在游戏启动时（如场景的 Start 方法或初始化脚本中）注册服务：

```csharp
// 方法一：手动注册
ServiceLocator.Register(UIMgr.Instance);
ServiceLocator.Register(PoolMgr.Instance);
ServiceLocator.Register(EventCenter.Instance);

// 方法二：使用适配器自动注册所有管理器
ServiceManagerAdapter.RegisterAllManagers();
```

### 3. 使用服务

在代码中通过服务定位器获取服务：

```csharp
// 获取服务
var uiMgr = ServiceLocator.Get<UIMgr>();
if (uiMgr != null)
{
    uiMgr.ShowPanel<MainPanel>();
}

// 检查服务是否已注册
if (ServiceLocator.IsRegistered<UIMgr>())
{
    // 服务已注册
}
```

## API 参考

### ServiceLocator（静态类）

#### 注册服务
```csharp
bool Register<T>(T service) where T : class
```
注册一个服务实例。

#### 注销服务
```csharp
bool Unregister<T>() where T : class
```
注销指定的服务。

#### 获取服务
```csharp
T Get<T>() where T : class
```
获取已注册的服务实例，如果未注册则返回 null。

#### 检查注册状态
```csharp
bool IsRegistered<T>() where T : class
```
检查服务是否已注册。

#### 清空所有服务
```csharp
void Clear()
```
清空所有已注册的服务。

#### 设置实例
```csharp
ServiceLocatorSO Instance { get; set; }
```
设置或获取 ServiceLocatorSO 实例。

### IService 接口

如果服务实现了 `IService` 接口，服务定位器会在注册时自动调用 `Initialize()`，在注销时自动调用 `Cleanup()`。

```csharp
public interface IService
{
    void Initialize();
    void Cleanup();
    bool IsInitialized { get; }
}
```

## 使用场景

### 场景 1：替换单例访问

**之前（单例模式）：**
```csharp
UIMgr.Instance.ShowPanel<MainPanel>();
```

**之后（服务定位器）：**
```csharp
ServiceLocator.Get<UIMgr>()?.ShowPanel<MainPanel>();
```

### 场景 2：创建自定义服务

```csharp
public class GameDataService : IService
{
    public bool IsInitialized { get; private set; }
    
    public void Initialize()
    {
        // 加载游戏数据
        IsInitialized = true;
    }
    
    public void Cleanup()
    {
        // 保存游戏数据
        IsInitialized = false;
    }
    
    public PlayerData GetPlayerData() { /* ... */ }
}

// 注册和使用
var dataService = new GameDataService();
ServiceLocator.Register(dataService);

var service = ServiceLocator.Get<GameDataService>();
var playerData = service.GetPlayerData();
```

### 场景 3：测试时替换服务

```csharp
// 在测试中，可以注册 Mock 服务
var mockUIMgr = new MockUIMgr();
ServiceLocator.Register(mockUIMgr);

// 被测代码使用服务定位器，无需修改
var uiMgr = ServiceLocator.Get<UIMgr>();
```

## 与单例模式的对比

| 特性 | 单例模式 | 服务定位器 |
|------|----------|-----------|
| 访问方式 | `UIMgr.Instance` | `ServiceLocator.Get<UIMgr>()` |
| 依赖关系 | 隐式，编译时绑定 | 显式，运行时解析 |
| 可测试性 | 难以替换 | 易于替换（Mock） |
| 配置化 | 不支持 | 支持（SO配置） |
| 学习成本 | 低 | 中 |
| 性能 | 略高 | 略低（字典查找） |

## 最佳实践

1. **统一初始化**：在游戏启动时统一注册所有服务
2. **使用适配器**：使用 `ServiceManagerAdapter.RegisterAllManagers()` 快速注册框架管理器
3. **实现 IService**：为自定义服务实现 IService 接口，支持生命周期管理
4. **空值检查**：使用 `?.` 操作符或检查 null，因为服务可能未注册
5. **逐步迁移**：可以逐步将代码从单例模式迁移到服务定位器

## 注意事项

1. **资源位置**：ServiceLocatorSO 资源需要放在 `Resources` 文件夹，或手动设置 `ServiceLocator.Instance`
2. **初始化顺序**：确保服务在使用前已注册
3. **性能考虑**：服务定位器使用字典查找，性能略低于直接访问，但差异可忽略
4. **向后兼容**：单例模式仍然可用，两者可以共存

## 故障排除

### 问题：获取服务返回 null

**原因：** 服务未注册或 ServiceLocatorSO 实例未设置

**解决：**
1. 确保在获取服务前已调用 `ServiceLocator.Register()`
2. 确保 ServiceLocatorSO 资源在 Resources 文件夹中，或手动设置 `ServiceLocator.Instance`

### 问题：服务注册失败

**原因：** 服务实例为 null 或类型不匹配

**解决：**
1. 确保服务实例不为 null
2. 检查类型是否正确

## 示例代码

完整示例请参考 `ServiceLocatorExample.cs` 文件。

