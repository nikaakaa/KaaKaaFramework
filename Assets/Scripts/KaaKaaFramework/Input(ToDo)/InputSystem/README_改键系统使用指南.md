# 改键系统使用指南

## 目录

1. [系统概述](#系统概述)
2. [快速开始](#快速开始)
3. [KeyBindingAttribute 特性详解](#keybindingattribute-特性详解)
4. [数据结构定义](#数据结构定义)
5. [API 使用说明](#api-使用说明)
6. [复合绑定处理](#复合绑定处理)
7. [完整示例](#完整示例)
8. [常见问题](#常见问题)

---

## 系统概述

改键系统是一个基于 Unity Input System 的数据驱动改键解决方案。核心特点：

- **数据驱动**：只需定义数据结构，使用特性标记，系统自动处理所有改键逻辑
- **自动映射**：通过反射扫描字段特性，自动建立字段与 Action 的映射关系
- **支持复合绑定**：自动处理 WASD 等复合绑定的子绑定改键
- **自动保存**：改键后自动保存到本地，支持加载和重置

---

## 快速开始

### 1. 定义数据结构

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerKeyBindings : IKeyBindingData
{
    [KeyBinding("Player", "Move", 0, "移动")]
    public string Move = "";
    
    [KeyBinding("Player", "Fire", 0, "开火")]
    public string Fire = "";
    
    // 实现接口方法
    private Dictionary<string, string> bindings = new Dictionary<string, string>();
    
    public string GetBindingPath(string fieldName)
    {
        if (bindings.ContainsKey(fieldName))
            return bindings[fieldName];
        
        var field = GetType().GetField(fieldName);
        if (field != null && field.FieldType == typeof(string))
        {
            string value = (string)field.GetValue(this);
            if (!string.IsNullOrEmpty(value))
            {
                bindings[fieldName] = value;
                return value;
            }
        }
        
        return "";
    }
    
    public void SetBindingPath(string fieldName, string bindingPath)
    {
        bindings[fieldName] = bindingPath;
        
        var field = GetType().GetField(fieldName);
        if (field != null && field.FieldType == typeof(string))
        {
            field.SetValue(this, bindingPath);
        }
    }
}
```

### 2. 初始化系统

```csharp
void Start()
{
    // 1. 初始化 InputActionAsset
    InputMgr.Instance.Init(inputActionAsset);
    
    // 2. 创建绑定数据实例
    var keyBindings = new PlayerKeyBindings();
    
    // 3. 初始化绑定数据（解析特性，建立映射）
    if (!InputMgr.Instance.InitBindingData(keyBindings))
    {
        Debug.LogError("按键绑定初始化失败！");
        return;
    }
    
    // 4. 加载保存的配置
    InputMgr.Instance.LoadBindings(keyBindings);
}
```

### 3. 改键操作

```csharp
// 改键：通过字段名来改键
InputMgr.Instance.StartRebinding("Move", keyBindings, (result) => 
{
    if (result.Success)
    {
        Debug.Log($"改键成功: {result.DisplayName}");
    }
    else
    {
        Debug.Log("改键已取消");
    }
});
```

---

## KeyBindingAttribute 特性详解

### 特性定义

```csharp
[KeyBinding(actionMapName, actionName, bindingIndex, displayName)]
```

### 参数说明

| 参数 | 类型 | 必需 | 默认值 | 说明 |
|------|------|------|--------|------|
| `actionMapName` | `string` | ✅ 是 | - | ActionMap 名称，对应 `InputActionAsset` 中的 ActionMap |
| `actionName` | `string` | ❌ 否 | 字段名 | Action 名称，对应 ActionMap 中的 Action |
| `bindingIndex` | `int` | ❌ 否 | `0` | 绑定索引，用于指定该字段对应 Action 的第几个绑定 |
| `displayName` | `string` | ❌ 否 | `null` | 显示名称，用于 UI 显示（当前版本未使用） |

### 使用示例

#### 示例 1：单个按键绑定

```csharp
[KeyBinding("Player", "Fire", 0, "开火")]
public string Fire = "";
```

- **ActionMap**: `"Player"`
- **Action**: `"Fire"`
- **绑定索引**: `0`（第一个绑定）
- **字段名**: `"Fire"`（用于改键时识别）

#### 示例 2：复合绑定（WASD）

```csharp
[KeyBinding("Player", "Move", 0, "移动-上")]  // W 键
public string MoveUp = "";

[KeyBinding("Player", "Move", 2, "移动-下")]  // S 键
public string MoveDown = "";

[KeyBinding("Player", "Move", 4, "移动-左")]  // A 键
public string MoveLeft = "";

[KeyBinding("Player", "Move", 6, "移动-右")]  // D 键
public string MoveRight = "";
```

- 同一个 Action `"Move"`
- 不同的 `bindingIndex` 对应不同的子绑定
- 索引顺序：0=W, 2=S, 4=A, 6=D

---

## 数据结构定义

### 必需实现接口

所有绑定数据类必须实现 `IKeyBindingData` 接口：

```csharp
public interface IKeyBindingData
{
    string GetBindingPath(string fieldName);
    void SetBindingPath(string fieldName, string bindingPath);
}
```

### 标准实现模板

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class YourKeyBindings : IKeyBindingData
{
    // 定义绑定字段
    [KeyBinding("Player", "ActionName", 0, "显示名称")]
    public string FieldName = "";
    
    // 内部存储
    private Dictionary<string, string> bindings = new Dictionary<string, string>();
    
    // 获取绑定路径
    public string GetBindingPath(string fieldName)
    {
        if (bindings.ContainsKey(fieldName))
            return bindings[fieldName];
        
        // 使用反射获取字段值（作为默认值）
        var field = GetType().GetField(fieldName);
        if (field != null && field.FieldType == typeof(string))
        {
            string value = (string)field.GetValue(this);
            if (!string.IsNullOrEmpty(value))
            {
                bindings[fieldName] = value;
                return value;
            }
        }
        
        return "";
    }
    
    // 设置绑定路径
    public void SetBindingPath(string fieldName, string bindingPath)
    {
        bindings[fieldName] = bindingPath;
        
        // 同时更新字段值
        var field = GetType().GetField(fieldName);
        if (field != null && field.FieldType == typeof(string))
        {
            field.SetValue(this, bindingPath);
        }
    }
}
```

### 字段命名规则

- 字段名用于改键时识别，可以任意命名
- 建议使用有意义的名称，如 `MoveUp`, `Fire`, `Jump` 等
- 字段类型必须是 `string`

---

## API 使用说明

### InputMgr 核心 API

#### 1. 初始化系统

```csharp
/// <summary>
/// 初始化 InputActionAsset
/// </summary>
public void Init(InputActionAsset asset)
```

**使用示例：**
```csharp
InputMgr.Instance.Init(inputActionAsset);
```

---

#### 2. 初始化绑定数据

```csharp
/// <summary>
/// 初始化按键绑定数据（解析特性，建立映射）
/// </summary>
public bool InitBindingData<T>(T bindingData) where T : class, IKeyBindingData
```

**使用示例：**
```csharp
var keyBindings = new PlayerKeyBindings();
bool success = InputMgr.Instance.InitBindingData(keyBindings);
if (!success)
{
    Debug.LogError("初始化失败！");
}
```

**功能：**
- 扫描所有带 `[KeyBinding]` 特性的字段
- 建立字段名 → (ActionMap, Action, 绑定索引) 的映射
- 验证 Action 和绑定索引是否存在
- 缓存默认绑定值

---

#### 3. 加载绑定

```csharp
/// <summary>
/// 加载并应用按键绑定
/// </summary>
public bool LoadBindings<T>(T bindingData) where T : class, IKeyBindingData, new()
```

**使用示例：**
```csharp
var keyBindings = new PlayerKeyBindings();
InputMgr.Instance.LoadBindings(keyBindings);
```

**功能：**
- 从 `SaveMgr` 加载保存的绑定数据
- 自动应用绑定到 `InputActionAsset`

---

#### 4. 开始改键

```csharp
/// <summary>
/// 开始改键操作（带详细回调）
/// </summary>
public void StartRebinding<T>(string fieldName, T bindingData, Action<RebindingResult> onComplete) where T : class, IKeyBindingData

/// <summary>
/// 开始改键操作（简化回调）
/// </summary>
public void StartRebinding<T>(string fieldName, T bindingData, Action<bool, string> onComplete) where T : class, IKeyBindingData
```

**使用示例：**

```csharp
// 方式 1：详细回调
InputMgr.Instance.StartRebinding("Move", keyBindings, (result) => 
{
    if (result.Success)
    {
        Debug.Log($"改键成功: {result.DisplayName}");
        Debug.Log($"新路径: {result.NewPath}");
    }
    else
    {
        Debug.Log($"改键失败: {result.ErrorMessage}");
    }
});

// 方式 2：简化回调
InputMgr.Instance.StartRebinding("Move", keyBindings, (success, path) => 
{
    if (success)
    {
        Debug.Log($"改键成功: {path}");
    }
    else
    {
        Debug.Log("改键已取消");
    }
});
```

**RebindingResult 结构：**
```csharp
public struct RebindingResult
{
    public bool Success;           // 是否成功
    public string NewPath;         // 新的绑定路径
    public string DisplayName;     // 显示名称（如 "W", "鼠标左键"）
    public string ErrorMessage;    // 错误信息
}
```

**注意事项：**
- 改键过程中按 `ESC` 可以取消
- 改键成功后会自动保存
- 改键会自动应用到 `InputActionAsset`

---

#### 5. 获取绑定显示文本

```csharp
/// <summary>
/// 获取绑定的显示文本（通过字段名）
/// </summary>
public string GetBindingDisplayString(string fieldName)
```

**使用示例：**
```csharp
string displayText = InputMgr.Instance.GetBindingDisplayString("Move");
// 返回：如 "W" 或 "Q"（如果改过键）
```

**功能：**
- 将绑定路径转换为可读的文本
- 例如：`<Keyboard>/w` → `"W"`
- 例如：`<Mouse>/leftButton` → `"鼠标左键"`

---

#### 6. 重置所有绑定

```csharp
/// <summary>
/// 重置所有绑定到默认值
/// </summary>
public bool ResetAllBindings<T>(T bindingData) where T : class, IKeyBindingData
```

**使用示例：**
```csharp
if (InputMgr.Instance.ResetAllBindings(keyBindings))
{
    Debug.Log("所有按键已重置");
}
```

**功能：**
- 将所有绑定重置为 `InputActionAsset` 中的默认值
- 自动保存重置后的配置

---

#### 7. 保存绑定

```csharp
/// <summary>
/// 保存按键绑定
/// </summary>
public bool SaveBindings<T>(T bindingData) where T : class, IKeyBindingData
```

**使用示例：**
```csharp
InputMgr.Instance.SaveBindings(keyBindings);
```

**功能：**
- 将当前绑定数据保存到本地（通过 `SaveMgr`）
- 改键成功后会自动调用此方法

---

## 复合绑定处理

### 什么是复合绑定？

复合绑定（Composite Binding）是 Unity Input System 中的一种特殊绑定类型，用于将多个按键组合成一个输入。常见的例子：

- **WASD 移动**：W/S/A/D 四个键组合成 2D 向量输入
- **方向键**：上下左右四个键组合成 2D 向量输入

### 复合绑定的结构

在 `InputActionAsset` 中，复合绑定由以下部分组成：

1. **复合绑定本身**（`isComposite = true`）：不能直接改键
2. **子绑定**（`isPartOfComposite = true`）：可以单独改键

例如，WASD 复合绑定的结构：

```
Move Action:
├── [复合绑定] Dpad (WASD)
│   ├── [子绑定] up → W 键
│   ├── [子绑定] up → UpArrow
│   ├── [子绑定] down → S 键
│   ├── [子绑定] down → DownArrow
│   ├── [子绑定] left → A 键
│   ├── [子绑定] left → LeftArrow
│   ├── [子绑定] right → D 键
│   └── [子绑定] right → RightArrow
```

### 如何确定 bindingIndex？

`GetKeyboardMouseBindings` 方法会返回所有可改键的绑定（排除复合绑定本身），按顺序排列：

对于 Move Action，返回的顺序是：
- 索引 0: W 键（up 主）
- 索引 1: UpArrow（up 备）
- 索引 2: S 键（down 主）
- 索引 3: DownArrow（down 备）
- 索引 4: A 键（left 主）
- 索引 5: LeftArrow（left 备）
- 索引 6: D 键（right 主）
- 索引 7: RightArrow（right 备）

### 完整示例：WASD 改键

```csharp
[Serializable]
public class PlayerKeyBindings : IKeyBindingData
{
    // Move Action - WASD 复合绑定的各个方向
    [KeyBinding("Player", "Move", 0, "移动-上(主)")]  // W 键
    public string MoveUp = "";
    
    [KeyBinding("Player", "Move", 1, "移动-上(备)")]  // UpArrow
    public string MoveUpAlt = "";
    
    [KeyBinding("Player", "Move", 2, "移动-下(主)")]  // S 键
    public string MoveDown = "";
    
    [KeyBinding("Player", "Move", 3, "移动-下(备)")]  // DownArrow
    public string MoveDownAlt = "";
    
    [KeyBinding("Player", "Move", 4, "移动-左(主)")]  // A 键
    public string MoveLeft = "";
    
    [KeyBinding("Player", "Move", 5, "移动-左(备)")]  // LeftArrow
    public string MoveLeftAlt = "";
    
    [KeyBinding("Player", "Move", 6, "移动-右(主)")]  // D 键
    public string MoveRight = "";
    
    [KeyBinding("Player", "Move", 7, "移动-右(备)")]  // RightArrow
    public string MoveRightAlt = "";
    
    // 实现接口方法...
}
```

### 简化版本（只改主键）

如果只需要改 WASD 四个主键：

```csharp
[KeyBinding("Player", "Move", 0, "移动-上")]  // W 键
public string MoveUp = "";

[KeyBinding("Player", "Move", 2, "移动-下")]  // S 键
public string MoveDown = "";

[KeyBinding("Player", "Move", 4, "移动-左")]  // A 键
public string MoveLeft = "";

[KeyBinding("Player", "Move", 6, "移动-右")]  // D 键
public string MoveRight = "";
```

---

## 完整示例

### 完整代码示例

```csharp
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TestChangeKeyInput : MonoBehaviour
{
    [Header("系统配置")]
    [SerializeField] private InputActionAsset inputActionAsset;

    [Header("改键按钮")]
    [SerializeField] private Button moveButton;
    [SerializeField] private Button fireButton;

    [Header("UI文本")]
    [SerializeField] private TextMeshProUGUI moveButtonText;
    [SerializeField] private TextMeshProUGUI fireButtonText;
    [SerializeField] private TextMeshProUGUI statusText;

    private PlayerKeyBindings keyBindings = new PlayerKeyBindings();

    void Start()
    {
        // 1. 初始化系统
        InputMgr.Instance.Init(inputActionAsset);

        // 2. 初始化绑定数据
        if (!InputMgr.Instance.InitBindingData(keyBindings))
        {
            Debug.LogError("按键绑定初始化失败！");
            enabled = false;
            return;
        }

        // 3. 加载保存的配置
        InputMgr.Instance.LoadBindings(keyBindings);

        // 4. 绑定按钮事件
        if (moveButton != null)
            moveButton.onClick.AddListener(() => OnRebindKey("Move", moveButtonText));
        if (fireButton != null)
            fireButton.onClick.AddListener(() => OnRebindKey("Fire", fireButtonText));

        // 5. 更新 UI
        UpdateAllButtonTexts();

        if (statusText != null)
        {
            statusText.text = "系统已初始化 | 支持键盘和鼠标 | ESC取消改键";
        }
    }

    void OnRebindKey(string fieldName, TextMeshProUGUI buttonText)
    {
        buttonText.text = "按下任意键...";
        if (statusText != null) 
            statusText.text = $"等待 {fieldName} 的新按键输入 | 按ESC取消";

        InputMgr.Instance.StartRebinding(fieldName, keyBindings, (result) => 
        {
            if (result.Success)
            {
                UpdateAllButtonTexts();
                if (statusText != null) 
                    statusText.text = $"改键成功: {fieldName} → {result.DisplayName}";
            }
            else
            {
                buttonText.text = "已取消";
                if (statusText != null) 
                    statusText.text = "改键已取消";
                StartCoroutine(RestoreButtonText(fieldName, buttonText));
            }
        });
    }

    void UpdateAllButtonTexts()
    {
        if (moveButtonText != null)
            moveButtonText.text = InputMgr.Instance.GetBindingDisplayString("Move");
        if (fireButtonText != null)
            fireButtonText.text = InputMgr.Instance.GetBindingDisplayString("Fire");
    }

    IEnumerator RestoreButtonText(string fieldName, TextMeshProUGUI buttonText)
    {
        yield return new WaitForSeconds(1.5f);
        buttonText.text = InputMgr.Instance.GetBindingDisplayString(fieldName);
    }
}
```

---

## 常见问题

### Q1: 如何确定 bindingIndex？

**A:** 
1. 查看 `InputActionAsset` 中的绑定顺序
2. 使用 `GetKeyboardMouseBindings` 方法返回的顺序（排除复合绑定本身）
3. 从 0 开始计数

### Q2: 为什么改键没有生效？

**A:** 
- 确保已调用 `InitBindingData` 初始化映射
- 确保已调用 `LoadBindings` 加载保存的配置
- 检查字段名是否正确
- 检查 Action 和绑定索引是否存在

### Q3: 复合绑定可以改键吗？

**A:** 
- 复合绑定本身（`isComposite = true`）不能直接改键
- 但复合绑定的子绑定（`isPartOfComposite = true`）可以单独改键
- 每个子绑定需要单独定义字段，使用不同的 `bindingIndex`

### Q4: 如何调试绑定问题？

**A:** 
```csharp
// 检查字段映射
var bindings = InputMgr.Instance.GetKeyboardMouseBindings("Player", "Move");
for (int i = 0; i < bindings.Count; i++)
{
    Debug.Log($"索引 {i}: {bindings[i].path}");
}

// 检查当前绑定
string currentPath = InputMgr.Instance.GetBindingDisplayString("Move");
Debug.Log($"当前绑定: {currentPath}");
```

### Q5: 保存的绑定在哪里？

**A:** 
- 绑定数据通过 `SaveMgr` 保存到本地
- 保存的键名为 `"KeyBindings"`
- 数据类型为你的绑定数据类（如 `PlayerKeyBindings`）

### Q6: 可以同时改多个键吗？

**A:** 
- 不可以，改键操作是串行的
- 如果在上一个改键未完成时开始新的改键，会自动取消上一个改键操作

---

## 总结

改键系统的核心流程：

1. **定义数据结构** → 使用 `[KeyBinding]` 特性标记字段
2. **初始化系统** → `Init()` + `InitBindingData()`
3. **加载绑定** → `LoadBindings()`
4. **改键操作** → `StartRebinding()`
5. **自动保存** → 改键成功后自动调用 `SaveBindings()`

系统会自动处理：
- ✅ 特性解析和映射建立
- ✅ 绑定应用和覆盖
- ✅ 数据保存和加载
- ✅ 复合绑定处理
- ✅ 错误处理和验证

---

**最后更新：** 2024年

