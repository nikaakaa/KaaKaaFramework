using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

/// <summary>
/// 通用InputSystem管理器，支持数据驱动的改键系统
/// 只需定义一个数据结构类，系统自动处理所有改键逻辑
/// </summary>
public class InputBindingMgr : BaseManager<InputBindingMgr>
{
    private InputActionAsset inputActionAsset;
    
    private struct RebindingInfo
    {
        public string fieldName;
        public string actionMapName;
        public string actionName;
        public int bindingIndex;
        public Action<bool, string> callback;
        public IKeyBindingData bindingData;
    }
    
    private RebindingInfo? currentRebinding;
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
    
    private Dictionary<string, (string actionMapName, string actionName, int bindingIndex)> fieldActionMap = new Dictionary<string, (string, string, int)>();
    private Dictionary<string, string> defaultBindings = new Dictionary<string, string>();
    
    private InputBindingMgr() { }
    
    /// <summary>
    /// 初始化InputActionAsset
    /// </summary>
    public void Init(InputActionAsset asset)
    {
        if (asset == null)
        {
            Debug.LogError("InputActionAsset不能为空！");
            return;
        }
        
        inputActionAsset = asset;
        
        foreach (var actionMap in inputActionAsset.actionMaps)
        {
            actionMap.Enable();
        }
    }
    
    /// <summary>
    /// 获取当前管理的 InputActionAsset（用于其他脚本直接使用）
    /// </summary>
    public InputActionAsset GetInputActionAsset()
    {
        return inputActionAsset;
    }
    
    /// <summary>
    /// 获取指定的 Action（用于直接使用 Action）
    /// </summary>
    public InputAction GetActionDirect(string actionMapName, string actionName)
    {
        return GetAction(actionMapName, actionName);
    }
    
    /// <summary>
    /// 初始化按键绑定数据（解析特性，建立映射）
    /// </summary>
    /// <typeparam name="T">按键绑定数据类型</typeparam>
    /// <param name="bindingData">绑定数据实例</param>
    /// <returns>是否初始化成功</returns>
    public bool InitBindingData<T>(T bindingData) where T : class, IKeyBindingData
    {
        if (bindingData == null)
        {
            Debug.LogError("绑定数据不能为空！");
            return false;
        }

        if (inputActionAsset == null)
        {
            Debug.LogWarning("请先初始化InputActionAsset！");
            return false;
        }

        fieldActionMap.Clear();
        defaultBindings.Clear();
        
        Type type = typeof(T);
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        
        int validCount = 0;
        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<KeyBindingAttribute>();
            if (attribute == null)
                continue;
            
            string fieldName = field.Name;
            string actionMapName = attribute.ActionMapName;
            string actionName = attribute.ActionName ?? fieldName;
            int bindingIndex = attribute.BindingIndex;
            
            // 验证Action是否存在
            var action = GetAction(actionMapName, actionName);
            if (action == null)
            {
                Debug.LogWarning($"字段 {fieldName} 对应的Action不存在: {actionMapName}/{actionName}");
                continue;
            }

            // 验证绑定索引是否有效
            var bindings = GetKeyboardMouseBindings(actionMapName, actionName);
            if (bindingIndex >= bindings.Count)
            {
                Debug.LogWarning($"字段 {fieldName} 的绑定索引 {bindingIndex} 超出范围 (总数: {bindings.Count})");
                continue;
            }
            
            // 缓存映射关系
            fieldActionMap[fieldName] = (actionMapName, actionName, bindingIndex);
            
            // 缓存默认绑定值（从InputActionAsset读取）
            string defaultPath = bindings[bindingIndex].path;
            if (!string.IsNullOrEmpty(defaultPath))
            {
                defaultBindings[fieldName] = defaultPath;
            }
            
            validCount++;
        }
        
        if (validCount == 0)
        {
            Debug.LogError("没有找到有效的按键绑定配置！请检查特性标记和InputActionAsset配置");
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// 加载并应用按键绑定
    /// </summary>
    /// <typeparam name="T">按键绑定数据类型</typeparam>
    /// <param name="bindingData">绑定数据实例</param>
    /// <returns>是否加载成功</returns>
    public bool LoadBindings<T>(T bindingData) where T : class, IKeyBindingData, new()
    {
        if (bindingData == null)
        {
            Debug.LogError("绑定数据不能为空！");
            return false;
        }

        if (fieldActionMap.Count == 0)
        {
            Debug.LogWarning("请先调用InitBindingData初始化映射表！");
            return false;
        }

        try
        {
            // 从SaveMgr加载数据
            T savedData = SaveMgr.Instance.LoadData<T>("KeyBindings");
            if (savedData != null)
            {
                // 复制保存的数据到当前实例
                CopyBindingData(savedData, bindingData);
            }
            
            // 应用绑定
            ApplyBindings(bindingData);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"加载按键绑定失败: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 保存按键绑定
    /// </summary>
    /// <typeparam name="T">按键绑定数据类型</typeparam>
    /// <param name="bindingData">绑定数据实例</param>
    /// <returns>是否保存成功</returns>
    public bool SaveBindings<T>(T bindingData) where T : class, IKeyBindingData
    {
        if (bindingData == null)
        {
            Debug.LogError("绑定数据不能为空！");
            return false;
        }

        try
        {
            SaveMgr.Instance.SaveData(bindingData, "KeyBindings");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"保存按键绑定失败: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 应用所有绑定到InputActionAsset
    /// </summary>
    public void ApplyBindings<T>(T bindingData) where T : class, IKeyBindingData
    {
        if (inputActionAsset == null)
        {
            Debug.LogWarning("InputActionAsset未初始化！");
            return;
        }
        
        foreach (var kvp in fieldActionMap)
        {
            string fieldName = kvp.Key;
            var (actionMapName, actionName, bindingIndex) = kvp.Value;
            
            string bindingPath = bindingData.GetBindingPath(fieldName);
            if (string.IsNullOrEmpty(bindingPath))
                continue;
            
            var action = GetAction(actionMapName, actionName);
            if (action == null)
                continue;
            
            var bindings = GetKeyboardMouseBindings(actionMapName, actionName);
            if (bindingIndex >= bindings.Count)
                continue;
            
            // 找到在原始 action.bindings 中的索引
            Guid targetBindingId = bindings[bindingIndex].id;
            int originalIndex = -1;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].id == targetBindingId)
                {
                    originalIndex = i;
                    break;
                }
            }
            
            if (originalIndex >= 0)
            {
                try
                {
                    action.ApplyBindingOverride(originalIndex, bindingPath);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[应用绑定] 应用 {fieldName} 失败: {e.Message}");
                }
            }
        }
    }
    
    /// <summary>
    /// 开始改键（简化版，推荐使用）
    /// </summary>
    /// <typeparam name="T">按键绑定数据类型</typeparam>
    /// <param name="fieldName">数据结构中的字段名</param>
    /// <param name="bindingData">绑定数据实例</param>
    /// <param name="onComplete">完成回调，返回详细结果信息</param>
    public void StartRebinding<T>(string fieldName, T bindingData, Action<RebindingResult> onComplete) where T : class, IKeyBindingData
    {
        StartRebinding(fieldName, bindingData, (success, path) =>
        {
            var result = new RebindingResult
            {
                Success = success,
                NewPath = path
            };
            
            if (success)
            {
                result.DisplayName = GetKeyDisplayName(path);
            }
            else
            {
                result.ErrorMessage = "改键被取消";
            }
            
            onComplete?.Invoke(result);
        });
    }

    /// <summary>
    /// 开始改键（使用字段名）
    /// </summary>
    /// <typeparam name="T">按键绑定数据类型</typeparam>
    /// <param name="fieldName">数据结构中的字段名</param>
    /// <param name="bindingData">绑定数据实例</param>
    /// <param name="onComplete">完成回调（成功，新按键路径）</param>
    public void StartRebinding<T>(string fieldName, T bindingData, Action<bool, string> onComplete) where T : class, IKeyBindingData
    {
        if (bindingData == null)
        {
            Debug.LogError("绑定数据不能为空！");
            onComplete?.Invoke(false, null);
            return;
        }

        if (string.IsNullOrEmpty(fieldName))
        {
            Debug.LogError("字段名不能为空！");
            onComplete?.Invoke(false, null);
            return;
        }

        if (!fieldActionMap.ContainsKey(fieldName))
        {
            Debug.LogError($"字段 {fieldName} 没有对应的Action配置！请先调用InitBindingData");
            onComplete?.Invoke(false, null);
            return;
        }
        
        var (actionMapName, actionName, bindingIndex) = fieldActionMap[fieldName];
        
        var action = GetAction(actionMapName, actionName);
        if (action == null)
        {
            Debug.LogError($"找不到Action: {actionMapName}/{actionName}");
            onComplete?.Invoke(false, null);
            return;
        }
        
        var bindings = GetKeyboardMouseBindings(actionMapName, actionName);
        if (bindingIndex >= bindings.Count)
        {
            Debug.LogError($"绑定索引超出范围: {bindingIndex} (总数: {bindings.Count})");
            onComplete?.Invoke(false, null);
            return;
        }
        
        InputBinding targetBinding = bindings[bindingIndex];
        Guid targetBindingId = targetBinding.id;
        
        // 检查是否是复合绑定
        if (targetBinding.isComposite)
        {
            Debug.LogError($"无法改键：字段 {fieldName} 对应的是复合绑定，不能直接改键");
            onComplete?.Invoke(false, null);
            return;
        }
        
        // 找到在原始 action.bindings 中的索引
        int originalIndex = -1;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (action.bindings[i].id == targetBindingId)
            {
                originalIndex = i;
                break;
            }
        }
        
        if (originalIndex < 0 || action.bindings[originalIndex].isComposite)
        {
            Debug.LogError($"找不到对应的绑定索引或绑定是复合绑定: {fieldName}");
            onComplete?.Invoke(false, null);
            return;
        }
        
        // 取消之前的改键
        if (rebindingOperation != null)
        {
            try
            {
                rebindingOperation.Cancel();
                rebindingOperation.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"取消之前的改键操作时发生错误: {e.Message}");
            }
            rebindingOperation = null;
        }
        
        // 改键前必须禁用 Action
        bool wasEnabled = action.enabled;
        if (wasEnabled)
        {
            action.Disable();
        }
        
        currentRebinding = new RebindingInfo
        {
            fieldName = fieldName,
            actionMapName = actionMapName,
            actionName = actionName,
            bindingIndex = bindingIndex,
            callback = onComplete,
            bindingData = bindingData
        };
        
        // 开始改键
        try
        {
            rebindingOperation = action.PerformInteractiveRebinding(originalIndex)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnCancel(operation =>
                {
                    try
                    {
                        var actionToEnable = GetAction(actionMapName, actionName);
                        if (wasEnabled && actionToEnable != null)
                        {
                            actionToEnable.Enable();
                        }
                        if (operation != null)
                        {
                            operation.Dispose();
                        }
                        rebindingOperation = null;
                        currentRebinding?.callback?.Invoke(false, null);
                        currentRebinding = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"改键取消回调中发生错误: {ex.Message}");
                        rebindingOperation = null;
                        currentRebinding = null;
                    }
                })
                .OnComplete(operation =>
                {
                    try
                    {
                        if (operation == null || operation.action == null)
                        {
                            var actionToEnable = GetAction(actionMapName, actionName);
                            if (wasEnabled && actionToEnable != null)
                            {
                                actionToEnable.Enable();
                            }
                            currentRebinding?.callback?.Invoke(false, null);
                            currentRebinding = null;
                            return;
                        }
                        
                        if (originalIndex < 0 || originalIndex >= operation.action.bindings.Count)
                        {
                            var actionToEnable = GetAction(actionMapName, actionName);
                            if (wasEnabled && actionToEnable != null)
                            {
                                actionToEnable.Enable();
                            }
                            if (operation != null)
                            {
                                operation.Dispose();
                            }
                            rebindingOperation = null;
                            currentRebinding?.callback?.Invoke(false, null);
                            currentRebinding = null;
                            return;
                        }
                        
                        string newPath = operation.action.bindings[originalIndex].effectivePath;
                        
                        if (string.IsNullOrEmpty(newPath))
                        {
                            var actionToEnable = GetAction(actionMapName, actionName);
                            if (wasEnabled && actionToEnable != null)
                            {
                                actionToEnable.Enable();
                            }
                            if (operation != null)
                            {
                                operation.Dispose();
                            }
                            rebindingOperation = null;
                            currentRebinding?.callback?.Invoke(false, null);
                            currentRebinding = null;
                            return;
                        }
                        
                        if (bindingData != null)
                        {
                            bindingData.SetBindingPath(fieldName, newPath);
                            
                            var actionToEnable = GetAction(actionMapName, actionName);
                            if (wasEnabled && actionToEnable != null)
                            {
                                actionToEnable.Enable();
                            }
                            
                            var actionToApply = GetAction(actionMapName, actionName);
                            if (actionToApply != null)
                            {
                                try
                                {
                                    actionToApply.ApplyBindingOverride(originalIndex, newPath);
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogError($"应用绑定覆盖失败: {ex.Message}");
                                }
                            }
                            
                            SaveBindings(bindingData);
                            ApplyBindings(bindingData);
                        }
                        
                        if (operation != null)
                        {
                            operation.Dispose();
                        }
                        rebindingOperation = null;
                        
                        if (currentRebinding.HasValue)
                        {
                            var rebinding = currentRebinding.Value;
                            rebinding.callback?.Invoke(true, newPath);
                        }
                        currentRebinding = null;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"改键完成时发生错误: {e.Message}");
                        var actionToEnable = GetAction(actionMapName, actionName);
                        if (wasEnabled && actionToEnable != null)
                        {
                            actionToEnable.Enable();
                        }
                        try
                        {
                            if (operation != null)
                            {
                                operation.Dispose();
                            }
                        }
                        catch { }
                        rebindingOperation = null;
                        currentRebinding?.callback?.Invoke(false, null);
                        currentRebinding = null;
                    }
                })
                .Start();
        }
        catch (Exception e)
        {
            Debug.LogError($"启动改键操作失败: {e.Message}");
            if (wasEnabled)
            {
                var actionToEnable = GetAction(actionMapName, actionName);
                if (actionToEnable != null)
                {
                    actionToEnable.Enable();
                }
            }
            rebindingOperation = null;
            currentRebinding = null;
            onComplete?.Invoke(false, null);
        }
    }
    
    /// <summary>
    /// 取消改键
    /// </summary>
    public void CancelRebinding()
    {
        if (rebindingOperation != null)
        {
            rebindingOperation.Cancel();
            rebindingOperation.Dispose();
            rebindingOperation = null;
            
            // 重新启用被禁用的 Action
            if (currentRebinding.HasValue)
            {
                var rebinding = currentRebinding.Value;
                var action = GetAction(rebinding.actionMapName, rebinding.actionName);
                if (action != null && !action.enabled)
                {
                    action.Enable();
                }
            }
            
            currentRebinding = null;
        }
    }
    
    /// <summary>
    /// 获取按键显示名称（使用字段名）
    /// </summary>
    public string GetBindingDisplayString(string fieldName)
    {
        if (!fieldActionMap.ContainsKey(fieldName))
            return "未绑定";
        
        var (actionMapName, actionName, bindingIndex) = fieldActionMap[fieldName];
        return GetBindingDisplayString(actionMapName, actionName, bindingIndex);
    }
    
    /// <summary>
    /// 重置所有绑定到默认值
    /// </summary>
    /// <typeparam name="T">按键绑定数据类型</typeparam>
    /// <param name="bindingData">绑定数据实例</param>
    /// <returns>是否重置成功</returns>
    public bool ResetAllBindings<T>(T bindingData) where T : class, IKeyBindingData
    {
        if (bindingData == null)
        {
            Debug.LogError("绑定数据不能为空！");
            return false;
        }

        if (inputActionAsset == null)
        {
            Debug.LogWarning("InputActionAsset未初始化！");
            return false;
        }
        
        try
        {
            // 移除所有覆盖
            foreach (var actionMap in inputActionAsset.actionMaps)
            {
                foreach (var action in actionMap.actions)
                {
                    action.RemoveAllBindingOverrides();
                }
            }
            
            // 重置数据结构到默认值
            foreach (var fieldName in fieldActionMap.Keys)
            {
                // 恢复到缓存的默认值（如果有），否则设为空
                string defaultPath = defaultBindings.ContainsKey(fieldName) ? defaultBindings[fieldName] : "";
                bindingData.SetBindingPath(fieldName, defaultPath);
            }
            
            SaveBindings(bindingData);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"重置按键绑定失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 改键结果信息
    /// </summary>
    public struct RebindingResult
    {
        public bool Success;
        public string NewPath;
        public string DisplayName;        // 按键显示名称
        public string ErrorMessage;       // 错误信息（如果有）
    }
    
    /// <summary>
    /// 获取按键的显示名称（人类可读格式）
    /// </summary>
    /// <param name="bindingPath">按键路径（如 "<Keyboard>/w"）</param>
    /// <returns>显示名称（如 "W"）</returns>
    public string GetKeyDisplayName(string bindingPath)
    {
        if (string.IsNullOrEmpty(bindingPath))
            return "未绑定";
        
        try
        {
            return InputControlPath.ToHumanReadableString(
                bindingPath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }
        catch
        {
            return bindingPath; // 如果转换失败，返回原始路径
        }
    }
    
    #region --- 私有辅助方法 ---
    
    private InputAction GetAction(string actionMapName, string actionName)
    {
        if (inputActionAsset == null)
            return null;
        
        var actionMap = inputActionAsset.FindActionMap(actionMapName);
        if (actionMap == null)
            return null;
        
        return actionMap.FindAction(actionName);
    }
    
    private List<InputBinding> GetKeyboardMouseBindings(string actionMapName, string actionName)
    {
        var action = GetAction(actionMapName, actionName);
        if (action == null)
            return new List<InputBinding>();
        
        // 只返回可以改键的绑定：
        // 1. 排除复合绑定本身（isComposite == true，不能直接改键）
        // 2. 包含复合绑定的子绑定（isPartOfComposite == true，可以改键）
        // 3. 包含单个按键绑定（既不是复合也不是子绑定，可以改键）
        return action.bindings.Where(b => 
            !b.isComposite && // 排除复合绑定本身
            (b.groups.Contains("Keyboard&Mouse") || 
             string.IsNullOrEmpty(b.groups))
        ).ToList();
    }
    
    private string GetBindingDisplayString(string actionMapName, string actionName, int bindingIndex)
    {
        var action = GetAction(actionMapName, actionName);
        if (action == null)
            return "未绑定";

        var bindings = GetKeyboardMouseBindings(actionMapName, actionName);
        if (bindingIndex >= bindings.Count)
            return "未绑定";

        // 优先使用effectivePath（实际生效的绑定，包括覆盖）
        string path = bindings[bindingIndex].effectivePath;
        if (string.IsNullOrEmpty(path))
        {
            // 如果没有有效路径，尝试使用path（默认绑定）
            path = bindings[bindingIndex].path;
        }

        if (string.IsNullOrEmpty(path))
            return "未绑定";

        try
        {
            return InputControlPath.ToHumanReadableString(
                path,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }
        catch
        {
            return path; // 如果转换失败，返回原始路径
        }
    }

    private void CopyBindingData<T>(T source, T target) where T : class, IKeyBindingData
    {
        Type type = typeof(T);
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(string))
            {
                string value = source.GetBindingPath(field.Name);
                target.SetBindingPath(field.Name, value);
            }
        }
    }
    
    #endregion
}


