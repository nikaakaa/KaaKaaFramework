using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test
{
    public void Print<T>(T t)
    {

    }
}

public class MySington
{
    private static MySington _instance;
    public static MySington Instance=>_instance ??= new MySington();
}

public class BaseSington<T> where T : class
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if(_instance != null) return _instance;

            _instance = Activator.CreateInstance(typeof(T), true) as T;
            return _instance;
        }
    }

}
