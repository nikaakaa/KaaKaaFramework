using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIPanelToolShort : EditorWindow
{
    private const string SAVED_PATH_KEY = "UIPanelToolShort_SavedPath";
    
    private string generatedCode = "";
    private string panelName = "";
    private Vector2 scrollPos;
    
    // 默认过滤名称列表
    private List<string> defaultName = new List<string>() { 
        "Image", "Text (TMP)", "RawImage", "Background", "Checkmark", 
        "Label", "Text (Legacy)", "Arrow", "Placeholder", "Fill", 
        "Handle", "Viewport", "Scrollbar Horizontal", "Scrollbar Vertical"
    };
    
    // 控件信息结构
    private class ControlInfo
    {
        public string name;
        public Type type;
        public string path;
    }
    
    [MenuItem("Tools/UI/简单UI控件绑定")]
    private static void CreateToolPanel()
    {
        UIPanelToolShort win = EditorWindow.GetWindow<UIPanelToolShort>("简单UI控件绑定");
        win.Show();
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("请先在Hierarchy中选择一个GameObject作为UI面板根对象", MessageType.Info);
        EditorGUILayout.Space(10);
        
        // 选择对象按钮
        if (GUILayout.Button("扫描选中对象", GUILayout.Height(30)))
        {
            ScanSelectedObject();
        }
        
        EditorGUILayout.Space(10);
        
        // 显示生成的代码
        if (!string.IsNullOrEmpty(generatedCode))
        {
            EditorGUILayout.LabelField("生成的代码:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
            EditorGUILayout.TextArea(generatedCode, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            
            // 保存按钮
            if (GUILayout.Button("选择保存路径", GUILayout.Height(30)))
            {
                SaveCode();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("请先选择对象并点击扫描", MessageType.Warning);
        }
    }
    
    /// <summary>
    /// 扫描选中的对象
    /// </summary>
    private void ScanSelectedObject()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            EditorUtility.DisplayDialog("错误", "请先在Hierarchy中选择一个GameObject", "确定");
            return;
        }
        
        panelName = obj.name;
        List<ControlInfo> controls = new List<ControlInfo>();
        Dictionary<string, Type> controlTypeDict = new Dictionary<string, Type>();
        
        // 扫描所有控件类型
        ScanControlType<Button>(obj, controls, controlTypeDict);
        ScanControlType<Toggle>(obj, controls, controlTypeDict);
        ScanControlType<Slider>(obj, controls, controlTypeDict);
        ScanControlType<InputField>(obj, controls, controlTypeDict);
        ScanControlType<Dropdown>(obj, controls, controlTypeDict);
        ScanControlType<ScrollRect>(obj, controls, controlTypeDict);
        ScanControlType<Text>(obj, controls, controlTypeDict);
        ScanControlType<TextMeshProUGUI>(obj, controls, controlTypeDict);
        ScanControlType<Image>(obj, controls, controlTypeDict);
        ScanControlType<RawImage>(obj, controls, controlTypeDict);
        
        if (controls.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "未找到任何UI控件", "确定");
            generatedCode = "";
            return;
        }
        
        // 生成代码
        generatedCode = GenerateCode(panelName, controls);
    }
    
    /// <summary>
    /// 扫描指定类型的控件
    /// </summary>
    private void ScanControlType<T>(GameObject root, List<ControlInfo> controls, Dictionary<string, Type> controlTypeDict) where T : UIBehaviour
    {
        T[] foundControls = root.GetComponentsInChildren<T>();
        Type type = typeof(T);
        
        foreach (var control in foundControls)
        {
            string controlName = control.gameObject.name;
            
            // 过滤默认名称
            if (defaultName.Contains(controlName) || controlName == root.name)
                continue;
            
            // 检查重复：同名同类型报错
            if (controlTypeDict.ContainsKey(controlName))
            {
                if (controlTypeDict[controlName] == type)
                {
                    EditorUtility.DisplayDialog("重复控件", $"存在多个相同类型的控件: {controlName}", "确定");
                    return;
                }
                // 同名不同类型，跳过（只保留第一个）
                continue;
            }
            
            // 记录控件信息
            controlTypeDict[controlName] = type;
            string path = GetPath(control.transform, root.transform);
            
            controls.Add(new ControlInfo
            {
                name = controlName,
                type = type,
                path = path
            });
        }
    }
    
    /// <summary>
    /// 获取控件相对于根对象的路径
    /// </summary>
    private string GetPath(Transform obj, Transform root)
    {
        string path = obj.name;
        Transform current = obj.parent;
        
        while (current != null && current != root)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        
        return path;
    }
    
    /// <summary>
    /// 将字符串首字母转换为小写
    /// </summary>
    private string ToLowerFirstChar(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;
        
        if (str.Length == 1)
            return str.ToLower();
        
        return char.ToLower(str[0]) + str.Substring(1);
    }
    
    /// <summary>
    /// 生成代码
    /// </summary>
    private string GenerateCode(string className, List<ControlInfo> controls)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        // 添加using语句
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("using TMPro;");
        sb.AppendLine();
        
        // 类声明
        sb.AppendLine($"public class {className} : MonoBehaviour");
        sb.AppendLine("{");
        
        // 控件声明（字段名首字母小写）
        foreach (var control in controls)
        {
            string fieldName = ToLowerFirstChar(control.name);
            sb.AppendLine($"    public {control.type.Name} {fieldName};");
        }
        
        sb.AppendLine();
        
        // Awake方法
        sb.AppendLine("    private void Awake()");
        sb.AppendLine("    {");
        
        // 查找代码（字段名首字母小写）
        foreach (var control in controls)
        {
            string fieldName = ToLowerFirstChar(control.name);
            sb.AppendLine($"        {fieldName} = transform.Find(\"{control.path}\").GetComponent<{control.type.Name}>();");
        }
        
        sb.AppendLine("    }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// 保存代码到文件
    /// </summary>
    private void SaveCode()
    {
        if (string.IsNullOrEmpty(generatedCode))
        {
            EditorUtility.DisplayDialog("错误", "没有可保存的代码", "确定");
            return;
        }
        
        // 从EditorPrefs读取上次保存的路径
        string savedPath = EditorPrefs.GetString(SAVED_PATH_KEY, "");
        string defaultPath = Application.dataPath + "/Scripts/";
        
        // 如果存在保存的路径且文件夹存在，则使用保存的路径
        if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
        {
            defaultPath = savedPath;
        }
        
        string path = EditorUtility.SaveFilePanel("保存脚本", defaultPath, panelName, "cs");
        
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, generatedCode);
            AssetDatabase.Refresh();
            
            // 保存选择的文件夹路径（从完整文件路径中提取文件夹路径）
            string directoryPath = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                EditorPrefs.SetString(SAVED_PATH_KEY, directoryPath);
            }
            
            EditorUtility.DisplayDialog("成功", $"代码已保存到: {path}", "确定");
        }
    }
}
