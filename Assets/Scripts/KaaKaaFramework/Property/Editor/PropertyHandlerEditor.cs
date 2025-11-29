using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
[CustomEditor(typeof(PropertyHandler))]
public class PropertyHandlerEditor : Editor
{
    private string _searchFilter = "";
    private Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();

    private string _debugPropertyName = "";
    private float _debugValue = 10f;
    private float _debugClampMin = 0f;
    private float _debugClampMax = 100f;

    public override void OnInspectorGUI()
    {
        var handler = (PropertyHandler)target;

        DrawHeader(handler);
        DrawSearchBar();
        DrawPropertyList(handler);
        DrawDebugPanel(handler);

        if (Application.isPlaying)
            Repaint();
    }

    private void DrawHeader(PropertyHandler handler)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        var count = handler.PropertyDict?.Count ?? 0;
        EditorGUILayout.LabelField($"属性管理器 - 共 {count} 个属性", EditorStyles.boldLabel);

        if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            if (handler.PropertyDict != null)
            {
                foreach (var prop in handler.PropertyDict.Values)
                    prop.SetDirty();
            }
        }

        if (GUILayout.Button("折叠", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            _foldouts.Clear();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSearchBar()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
        _searchFilter = EditorGUILayout.TextField(_searchFilter);
        if (GUILayout.Button("✕", GUILayout.Width(20)))
            _searchFilter = "";
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
    }

    private void DrawPropertyList(PropertyHandler handler)
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("▶ 请运行游戏查看属性", MessageType.Info);
            return;
        }

        if (handler.PropertyDict == null || handler.PropertyDict.Count == 0)
        {
            EditorGUILayout.HelpBox("暂无属性", MessageType.Info);
            return;
        }

        var properties = handler.PropertyDict.Values
            .Where(p => string.IsNullOrEmpty(_searchFilter) ||
                        p.PropertyName.ToLower().Contains(_searchFilter.ToLower()))
            .GroupBy(p => GetGroupName(p.PropertyName))
            .OrderBy(g => g.Key);

        foreach (var group in properties)
        {
            DrawPropertyGroup(group.Key, group.ToList());
        }
    }

    private string GetGroupName(string propertyName)
    {
        var dashIndex = propertyName.IndexOf('-');
        return dashIndex > 0 ? propertyName.Substring(0, dashIndex) : propertyName;
    }

    private void DrawPropertyGroup(string groupName, List<IProperty> properties)
    {
        if (!_foldouts.ContainsKey(groupName))
            _foldouts[groupName] = true;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        _foldouts[groupName] = EditorGUILayout.Foldout(_foldouts[groupName], "", true);
        EditorGUILayout.LabelField($"📁 {groupName}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"({properties.Count})", GUILayout.Width(30));
        EditorGUILayout.EndHorizontal();

        if (_foldouts[groupName])
        {
            EditorGUI.indentLevel++;
            foreach (var prop in properties.OrderBy(p => p.PropertyName))
            {
                DrawPropertyItem(prop);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPropertyItem(IProperty prop)
    {
        var isComputed = IsComputedProperty(prop);
        var icon = isComputed ? "⚙" : "📊";
        var isSelected = _debugPropertyName == prop.PropertyName;

        // 获取整行区域用于点击检测
        var rect = EditorGUILayout.BeginHorizontal();

        // 选中时绘制高亮背景
        if (isSelected)
        {
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.5f, 0.9f, 0.3f));
        }

        // 鼠标悬停效果
        if (rect.Contains(Event.current.mousePosition))
        {
            EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.1f));
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
        }

        // 图标 + 名称
        var labelStyle = new GUIStyle(EditorStyles.label);
        if (isSelected)
        {
            labelStyle.fontStyle = FontStyle.Bold;
            GUI.color = Color.cyan;
        }

        EditorGUILayout.LabelField($"{icon} {prop.PropertyName}", labelStyle, GUILayout.Width(220));
        GUI.color = Color.white;

        // 值显示
        GUI.color = isComputed ? Color.cyan : Color.white;
        var valueStr = GetValueDisplay(prop);
        EditorGUILayout.LabelField(valueStr, EditorStyles.boldLabel, GUILayout.Width(120));
        GUI.color = Color.white;

        // 修改器数量
        if (prop.Modifiers.Count > 0)
        {
            GUI.color = Color.yellow;
            EditorGUILayout.LabelField($"[+{prop.Modifiers.Count}]", GUILayout.Width(40));
            GUI.color = Color.white;
        }

        EditorGUILayout.EndHorizontal();

        // 点击选中
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            _debugPropertyName = prop.PropertyName;
            Event.current.Use();
            Repaint();
        }

        // 展开显示修改器
        DrawModifierList(prop);
    }

    private bool IsComputedProperty(IProperty prop)
    {
        var type = prop.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ComputedProperty<>);
    }

    private string GetValueDisplay(IProperty prop)
    {
        if (prop is IProperty<float> fp)
        {
            var val = fp.GetValue();
            if (prop is BasicProperty<float> bp)
                return $"{bp.BaseValue:F1} → {val:F1}";
            return $"(计算) {val:F2}";
        }
        if (prop is IProperty<int> ip)
        {
            var val = ip.GetValue();
            if (prop is BasicProperty<int> bpi)
                return $"{bpi.BaseValue} → {val}";
            return $"(计算) {val}";
        }
        if (prop is IProperty<bool> bp2)
        {
            return bp2.GetValue() ? "✓" : "✗";
        }
        return "?";
    }

    private void DrawModifierList(IProperty prop)
    {
        if (prop.Modifiers.Count == 0) return;

        var modKey = prop.PropertyName + "_mods";
        if (!_foldouts.ContainsKey(modKey))
            _foldouts[modKey] = false;

        EditorGUI.indentLevel += 2;
        _foldouts[modKey] = EditorGUILayout.Foldout(_foldouts[modKey], $"修改器 ({prop.Modifiers.Count})", true);

        if (_foldouts[modKey])
        {
            foreach (var mod in prop.Modifiers)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("  ├─", GUILayout.Width(30));

                GUI.color = GetModifierColor(mod.ModifierType);
                EditorGUILayout.LabelField($"[{mod.ModifierType}]", GUILayout.Width(100));
                GUI.color = Color.white;

                EditorGUILayout.LabelField($"P:{mod.Priority}", GUILayout.Width(50));

                var valueStr = GetModifierValueString(mod);
                if (!string.IsNullOrEmpty(valueStr))
                    EditorGUILayout.LabelField(valueStr, GUILayout.Width(100));

                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUI.indentLevel -= 2;
    }

    private string GetModifierValueString(IPropertyModifier mod)
    {
        if (mod is IPropertyAdditiveModifier<float> addF)
            return $"+{addF.AddValue:F1}";
        if (mod is IPropertyMultiplicativeModifier<float> mulF)
            return $"×{mulF.MultiValue:F2}";
        if (mod is IPropertyClampModifier<float> clampF)
            return $"[{clampF.Min:F1}, {clampF.Max:F1}]";
        if (mod is IPropertyOverrideModifier<float> overF)
            return $"={overF.OverrideValue:F1}";
        return "";
    }

    private Color GetModifierColor(E_PropertyModifierType type)
    {
        return type switch
        {
            E_PropertyModifierType.Additive => Color.green,
            E_PropertyModifierType.Multiplicative => new Color(0.5f, 0.8f, 1f),
            E_PropertyModifierType.Clamp => Color.yellow,
            E_PropertyModifierType.Override => new Color(1f, 0.5f, 0.5f),
            _ => Color.white
        };
    }

    private void DrawDebugPanel(PropertyHandler handler)
    {
        if (!Application.isPlaying) return;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🔧 调试面板", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 显示当前选中属性
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("选中:", GUILayout.Width(40));

        GUI.enabled = false;
        EditorGUILayout.TextField(_debugPropertyName);
        GUI.enabled = true;

        if (GUILayout.Button("✕", GUILayout.Width(25)))
        {
            _debugPropertyName = "";
        }
        EditorGUILayout.EndHorizontal();

        // 检查属性状态
        var selectedProp = handler.GetProperty(_debugPropertyName);
        if (!string.IsNullOrEmpty(_debugPropertyName))
        {
            if (selectedProp == null)
            {
                EditorGUILayout.HelpBox($"❌ 找不到属性: {_debugPropertyName}", MessageType.Error);
            }
            else
            {
                var isComputed = IsComputedProperty(selectedProp);
                var currentValue = GetValueDisplay(selectedProp);

                GUI.color = isComputed ? Color.cyan : Color.green;
                var msg = $"✓ 当前值: {currentValue} | 修改器: {selectedProp.Modifiers.Count}";
                if (isComputed) msg += "\n⚠ 计算属性仅支持 Clamp / Override";
                EditorGUILayout.HelpBox(msg, MessageType.Info);
                GUI.color = Color.white;
            }
        }
        else
        {
            EditorGUILayout.HelpBox("💡 点击上方属性列表选择调试目标", MessageType.Info);
        }

        EditorGUILayout.Space(5);

        // ========== 加法 / 乘法 / 覆盖 ==========
        EditorGUILayout.LabelField("数值修改器", EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("值:", GUILayout.Width(40));
        _debugValue = EditorGUILayout.FloatField(_debugValue);
        EditorGUILayout.EndHorizontal();

        var isComputed2 = selectedProp != null && IsComputedProperty(selectedProp);

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(selectedProp == null || isComputed2);
        if (GUILayout.Button("+ 加法"))
        {
            selectedProp.AddModifier(new AdditiveModifier<float>(_debugValue));
            Debug.Log($"[Debug] 添加 Additive +{_debugValue} → {_debugPropertyName}");
        }
        if (GUILayout.Button("× 乘法"))
        {
            selectedProp.AddModifier(new MultiplicativeModifier<float>(_debugValue));
            Debug.Log($"[Debug] 添加 Multiplicative ×{_debugValue} → {_debugPropertyName}");
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(selectedProp == null);
        if (GUILayout.Button("= 覆盖"))
        {
            selectedProp.AddModifier(new OverrideModifier<float>(_debugValue));
            Debug.Log($"[Debug] 添加 Override ={_debugValue} → {_debugPropertyName}");
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // ========== Clamp 限制 ==========
        EditorGUILayout.LabelField("范围限制 (Clamp)", EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Min:", GUILayout.Width(30));
        _debugClampMin = EditorGUILayout.FloatField(_debugClampMin, GUILayout.Width(60));
        EditorGUILayout.LabelField("Max:", GUILayout.Width(30));
        _debugClampMax = EditorGUILayout.FloatField(_debugClampMax, GUILayout.Width(60));

        EditorGUI.BeginDisabledGroup(selectedProp == null);
        if (GUILayout.Button("添加 Clamp"))
        {
            selectedProp.AddModifier(new ClampModifier<float>(_debugClampMin, _debugClampMax));
            Debug.Log($"[Debug] 添加 Clamp [{_debugClampMin}, {_debugClampMax}] → {_debugPropertyName}");
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // ========== 清除 ==========
        EditorGUI.BeginDisabledGroup(selectedProp == null || selectedProp.Modifiers.Count == 0);
        if (GUILayout.Button("清除所有修改器"))
        {
            var mods = selectedProp.Modifiers.ToList();
            foreach (var m in mods)
                selectedProp.RemoveModifier(m);
            Debug.Log($"[Debug] 清除 {mods.Count} 个修改器");
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndVertical();
    }
}
#endif