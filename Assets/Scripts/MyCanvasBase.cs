using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MyCanvasBaseBase : MonoBehaviour
{
    //自动生成的控件声明
    public Button testBtn;
    public Button testBtn1;
    public Button testBtn2;
    public Button testBtn3;
    public Button testBtn4;
    public Button testBtn4_1;
    public Button testBtn4_2;
    public Button testBtn4_3;
    public ScrollRect scroll_View;
    public Button testSSS;
    public Button aaaa;
    public TextMeshProUGUI qqqq;
    public Button button;


    protected virtual void OnEnable()
    {
        //控件引用已在编辑器模式下自动绑定，直接使用即可

        //自动生成的进行对应控件的事件监听
        testBtn.onClick.AddListener(OnTestBtnClick);
        testBtn1.onClick.AddListener(OnTestBtn1Click);
        testBtn2.onClick.AddListener(OnTestBtn2Click);
        testBtn3.onClick.AddListener(OnTestBtn3Click);
        testBtn4.onClick.AddListener(OnTestBtn4Click);
        testBtn4_1.onClick.AddListener(OnTestBtn4_1Click);
        testBtn4_2.onClick.AddListener(OnTestBtn4_2Click);
        testBtn4_3.onClick.AddListener(OnTestBtn4_3Click);
        testSSS.onClick.AddListener(OnTestSSSClick);
        aaaa.onClick.AddListener(OnAaaaClick);
        button.onClick.AddListener(OnButtonClick);

    }

    protected virtual void OnDisable()
    {
        //自动生成的移除对应控件的事件监听
        testBtn.onClick.RemoveListener(OnTestBtnClick);
        testBtn1.onClick.RemoveListener(OnTestBtn1Click);
        testBtn2.onClick.RemoveListener(OnTestBtn2Click);
        testBtn3.onClick.RemoveListener(OnTestBtn3Click);
        testBtn4.onClick.RemoveListener(OnTestBtn4Click);
        testBtn4_1.onClick.RemoveListener(OnTestBtn4_1Click);
        testBtn4_2.onClick.RemoveListener(OnTestBtn4_2Click);
        testBtn4_3.onClick.RemoveListener(OnTestBtn4_3Click);
        testSSS.onClick.RemoveListener(OnTestSSSClick);
        aaaa.onClick.RemoveListener(OnAaaaClick);
        button.onClick.RemoveListener(OnButtonClick);

    }

    //自动生成的对应进行监听事件的响应函数
    protected virtual void OnTestBtnClick(){}
    protected virtual void OnTestBtn1Click(){}
    protected virtual void OnTestBtn2Click(){}
    protected virtual void OnTestBtn3Click(){}
    protected virtual void OnTestBtn4Click(){}
    protected virtual void OnTestBtn4_1Click(){}
    protected virtual void OnTestBtn4_2Click(){}
    protected virtual void OnTestBtn4_3Click(){}
    protected virtual void OnTestSSSClick(){}
    protected virtual void OnAaaaClick() { }
    protected virtual void OnButtonClick(){}
    
}
