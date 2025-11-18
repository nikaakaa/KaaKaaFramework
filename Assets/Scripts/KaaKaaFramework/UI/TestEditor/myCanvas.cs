using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class myCanvas : MonoBehaviour
{
    public Button testBtn;
    public Button testBtn1;
    public Button testBtn2;
    public Button testBtn3;
    public Button testBtn4;
    public Button testSSS;
    public Button asdf;
    public Button button;
    public ScrollRect scroll_View;
    public TextMeshProUGUI qqqq;

    private void Awake()
    {
        testBtn = transform.Find("testBtn").GetComponent<Button>();
        testBtn1 = transform.Find("testBtn1").GetComponent<Button>();
        testBtn2 = transform.Find("testBtn2").GetComponent<Button>();
        testBtn3 = transform.Find("testBtn3").GetComponent<Button>();
        testBtn4 = transform.Find("testBtn4").GetComponent<Button>();
        testSSS = transform.Find("as/Scroll_View/Viewport/Content/testSSS").GetComponent<Button>();
        asdf = transform.Find("as/asdf").GetComponent<Button>();
        button = transform.Find("Button").GetComponent<Button>();
        scroll_View = transform.Find("as/Scroll_View").GetComponent<ScrollRect>();
        qqqq = transform.Find("as/asdf/qqqq").GetComponent<TextMeshProUGUI>();
    }
}
