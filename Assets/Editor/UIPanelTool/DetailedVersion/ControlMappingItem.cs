using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 控件映射项数据结构
/// </summary>
public class ControlMappingItem
{
    public string controlName;      // 控件原始名称
    public string fieldName;        // 自定义字段名（可编辑）
    public Type controlType;        // 控件类型
    public string path;             // 控件路径
    public bool isSelected;         // 是否选中绑定
    public GameObject gameObject;   // 控件对象引用
    public Component component;     // 控件组件引用
    
    // 编译后处理需要的信息
    public string panelName;        // 面板名称
    public string targetObjectPath; // 目标对象路径

    // 树状结构引用
    public ControlMappingItem parent;           // 父节点引用
    public List<ControlMappingItem> children;   // 子节点列表

    // UI控件标识
    public bool isUIControl;                    // 是否为UI控件

    public ControlMappingItem(string controlName, Type controlType, string path, GameObject gameObject, Component component)
    {
        this.controlName = controlName;
        this.fieldName = controlName; // 默认字段名使用控件名
        this.controlType = controlType;
        this.path = path;
        this.gameObject = gameObject;
        this.component = component;
        this.isSelected = true; // 默认选中
        this.parent = null;
        this.children = new List<ControlMappingItem>();
        this.isUIControl = false; // 默认为false，需要在创建时设置
    }
}

