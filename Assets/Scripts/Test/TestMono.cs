using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMono : MonoBehaviour
{
    private PropertyHandler handler;
    private void Awake()
    {
        handler = GetComponent<PropertyHandler>();
        new BasicProperty<int>(100, "test_value_config").Register(handler)
            .NotifyParentOnDirty("test_value");
        new BasicProperty<int>(0, "test_value_buff").Register(handler)
            .NotifyParentOnDirty("test_value");
        new BasicProperty<int>(0, "test_value_other").Register(handler)
            .NotifyParentOnDirty("test_value");

        new BasicProperty<float>(0f,"test_mul_buff").Register(handler)
            .NotifyParentOnDirty("test_mul");
        new BasicProperty<float>(1f, "test_mul_other").Register(handler)
            .NotifyParentOnDirty("test_mul");

        new ComputedProperty<int>(() =>
            handler.GetProperty<int>("test_value_config").GetValue() +
            handler.GetProperty<int>("test_value_buff").GetValue() +
            handler.GetProperty<int>("test_value_other").GetValue()
            , "test_value").Register(handler).NotifyParentOnDirty("test");
        new ComputedProperty<float>(() =>
            (1 + handler.GetProperty<float>("test_mul_buff").GetValue())
            *handler.GetProperty<float>("test_mul_other").GetValue()
            , "test_mul").Register(handler).NotifyParentOnDirty("test");
        new ComputedProperty<float>(() =>
            handler.GetProperty<int>("test_value").GetValue()
            * handler.GetProperty<float>("test_mul").GetValue()
            , "test")
            .Register(handler)
            .AddModifier(new ClampModifier<float>(0f,500f));
    }
    private void Start()
    {
        
        
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            print(handler.GetProperty<float>("test").GetValue());
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            StartCoroutine(AddTest());
        }
        
    }
    public IEnumerator AddTest()
    {
        var theMod = new AdditiveModifier<int>(50,100);
        handler.GetProperty<int>("test_value_buff").AddModifier(theMod);
        print(handler.GetProperty<float>("test").GetValue());
        yield return new WaitForSeconds(2f);
        handler.GetProperty<int>("test_value_buff").RemoveModifier(theMod);
        print(handler.GetProperty<float>("test").GetValue());
    }
    

}