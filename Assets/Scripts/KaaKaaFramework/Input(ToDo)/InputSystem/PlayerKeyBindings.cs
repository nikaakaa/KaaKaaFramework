using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家按键配置数据结构示例
/// 只需要定义字段，使用特性标记对应的Action信息
/// </summary>
[Serializable]
public class PlayerKeyBindings : IKeyBindingData
{
    [KeyBinding("Player", "Move", 0, "移动")]
    public string Move = "";
    
    [KeyBinding("Player", "Look", 0, "视角")]
    public string Look = "";
    
    [KeyBinding("Player", "Fire", 0, "开火")]
    public string Fire = "";
    
    // 实现接口方法
    private Dictionary<string, string> bindings = new Dictionary<string, string>();
    
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

