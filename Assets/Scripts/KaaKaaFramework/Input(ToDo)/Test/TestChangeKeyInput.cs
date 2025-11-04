using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// InputSystem 改键系统测试脚本 - 精简版
/// 功能：改键、重置所有
/// </summary>
public class TestChangeKeyInput : MonoBehaviour
{
    [Header("系统配置")]
    [SerializeField] private InputActionAsset inputActionAsset;

    [Header("改键按钮")]
    [SerializeField] private Button moveButton;
    [SerializeField] private Button fireButton;
    [SerializeField] private Button lookButton;

    [Header("功能按钮")]
    [SerializeField] private Button resetAllButton;

    [Header("UI文本")]
    [SerializeField] private TextMeshProUGUI moveButtonText;
    [SerializeField] private TextMeshProUGUI fireButtonText;
    [SerializeField] private TextMeshProUGUI lookButtonText;
    [SerializeField] private TextMeshProUGUI statusText;

    private PlayerKeyBindings keyBindings = new PlayerKeyBindings();

    void Start()
    {
        // 初始化系统
        InputMgr.Instance.Init(inputActionAsset);

        if (!InputMgr.Instance.InitBindingData(keyBindings))
        {
            Debug.LogError("按键绑定初始化失败！");
            enabled = false;
            return;
        }

        // 加载保存的配置
        InputMgr.Instance.LoadBindings(keyBindings);

        // 绑定按钮事件
        if (moveButton != null)
            moveButton.onClick.AddListener(() => OnRebindKey("Move", moveButtonText));
        if (fireButton != null)
            fireButton.onClick.AddListener(() => OnRebindKey("Fire", fireButtonText));
        if (lookButton != null)
            lookButton.onClick.AddListener(() => OnRebindKey("Look", lookButtonText));
        if (resetAllButton != null)
            resetAllButton.onClick.AddListener(OnResetAll);

        // 更新 UI
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

    void OnResetAll()
    {
        if (InputMgr.Instance.ResetAllBindings(keyBindings))
        {
            UpdateAllButtonTexts();
            if (statusText != null) 
                statusText.text = "所有按键已重置到默认值";
        }
    }

    void UpdateAllButtonTexts()
    {
        if (moveButtonText != null)
            moveButtonText.text = InputMgr.Instance.GetBindingDisplayString("Move");
        if (fireButtonText != null)
            fireButtonText.text = InputMgr.Instance.GetBindingDisplayString("Fire");
        if (lookButtonText != null)
            lookButtonText.text = InputMgr.Instance.GetBindingDisplayString("Look");
    }

    IEnumerator RestoreButtonText(string fieldName, TextMeshProUGUI buttonText)
    {
        yield return new WaitForSeconds(1.5f);
        buttonText.text = InputMgr.Instance.GetBindingDisplayString(fieldName);
    }
}

