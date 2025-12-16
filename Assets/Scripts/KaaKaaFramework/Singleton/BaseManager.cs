using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
///// <summary>
///// 单例模式基类 主要目的是避免代码的冗余 方便我们实现单例模式的类
///// </summary>
///// <typeparam name="T"></typeparam>
//public abstract class BaseManager<T> where T:class//,new()
//{
//    private static T instance;

//    //判断单例模式对象 是否为null
//    protected bool InstanceisNull => instance == null;

//    //用于加锁的对象
//    protected static readonly object lockObj = new object();

//    //属性的方式
//    public static T Instance
//    {
//        get
//        {
//            if(instance == null)
//            {
//                lock (lockObj)
//                {
//                    if (instance == null)
//                    {
//                        //instance = new T();
//                        //利用反射得到无参私有的构造函数 来用于对象的实例化
//                        Type type = typeof(T);
//                        ConstructorInfo info = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
//                                                                    null,
//                                                                    Type.EmptyTypes,
//                                                                    null);
//                        if (info != null)
//                            instance = info.Invoke(null) as T;
//                        else
//                            Debug.LogError("没有得到对应的无参构造函数");

//                        //instance = Activator.CreateInstance(typeof(T), true) as T;
//                    }
//                }
//            }
//            return instance;
//        }
//    }


//    //方法的方式
//    //public static T GetInstance()
//    //{
//    //    if (instance == null)
//    //        instance = new T();
//    //    return instance;
//    //}
//}

/// <summary>
/// 单例模式基类 - CRTP 版本
/// </summary>
public abstract class BaseManager<T> where T : BaseManager<T>
{
    private static T instance;
    private static readonly object lockObj = new object();

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                lock (lockObj)
                {
                    if (instance == null)
                    {
                        Type type = typeof(T);
                        ConstructorInfo info = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
                                                                    null,
                                                                    Type.EmptyTypes,
                                                                    null);
                        if (info != null)
                            instance = info.Invoke(null) as T;
                        else
                            Debug.LogError("没有得到对应的无参构造函数");
                    }
                }
            }
            return instance;
        }
    }
}

public class GameManager: BaseManager<GameManager>
{
    private GameManager()
    {
        // 私有构造函数，防止外部实例化
    }
    public void PrintMessage()
    {
        Debug.Log("GameManager instance method called.");
    }
}

public class TestSingleton:MonoBehaviour
{
    public void Test()
    {
        GameManager.Instance.PrintMessage();
    }
}