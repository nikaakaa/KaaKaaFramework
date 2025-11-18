using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestCanvas : MonoBehaviour
{
    public Button btn1;
    public Button btn2;

    private void Awake()
    {
        btn1 = transform.Find("btn1").GetComponent<Button>();
        btn2 = transform.Find("btn2").GetComponent<Button>();
    }
}
