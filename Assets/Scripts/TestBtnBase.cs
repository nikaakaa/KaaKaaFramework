using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestBtnBaseBase : MonoBehaviour
{
    //自动生成的控件声明
    public Button testBtn;


    protected virtual void Start()
    {
        //控件引用已在编辑器模式下自动绑定，直接使用即可

        //自动生成的进行对应控件的事件监听
        testBtn.onClick.AddListener(OntestBtnClick);

    }

    //自动生成的对应进行监听事件的响应函数
    protected virtual void OntestBtnClick(){}


}