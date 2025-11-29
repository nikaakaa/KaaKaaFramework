using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

///注意,此事件系统是强类型,触发和监听的类型必须完全相同,没有逆变协变

/// <summary>
/// 全局事件类型 枚举
/// </summary>

public enum E_GEventType
{

}

/// <summary>
/// 用于 里式替换原则 装载 子类的父类
/// </summary>
public abstract class EventInfoBase{ }

/// <summary>
/// 用来包裹 对应观察者 函数委托的 类
/// </summary>
/// <typeparam name="T"></typeparam>
public class EventInfo<T>:EventInfoBase
{
    //真正观察者 对应的 函数信息 记录在其中
    public UnityAction<T> actions;

    public EventInfo(UnityAction<T> action)
    {
        actions += action;
    }
}

/// <summary>
/// 主要用来记录无参无返回值委托
/// </summary>
public class EventInfo: EventInfoBase
{
    public UnityAction actions;
     
    public EventInfo(UnityAction action)
    {
        actions += action;
    }
}


/// <summary>
/// 事件中心模块 
/// </summary>
public class EventCenter: BaseManager<EventCenter>
{
    //用于记录对应事件 关联的 对应的逻辑
    private Dictionary<E_GEventType, EventInfoBase> eventDic = new Dictionary<E_GEventType, EventInfoBase>();

    private EventCenter() { }

    /// <summary>
    /// 触发事件 
    /// </summary>
    /// <param name="eventName">事件名字</param>
    public void EventTrigger<T>(E_GEventType eventName, T info)
    {
        //存在关心我的人 才通知别人去处理逻辑
        if(eventDic.ContainsKey(eventName))
        {
            //去执行对应的逻辑
            (eventDic[eventName] as EventInfo<T>).actions?.Invoke(info);
        }
    }

    /// <summary>
    /// 触发事件 无参数
    /// </summary>
    /// <param name="eventName"></param>
    public void EventTrigger(E_GEventType eventName)
    {
        //存在关心我的人 才通知别人去处理逻辑
        if (eventDic.ContainsKey(eventName))
        {
            //去执行对应的逻辑
            (eventDic[eventName] as EventInfo).actions?.Invoke();
        }
    }


    /// <summary>
    /// 添加事件监听者
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="func"></param>
    public void AddEventListener<T>(E_GEventType eventName, UnityAction<T> func)
    {
        //如果已经存在关心事件的委托记录 直接添加即可
        if (eventDic.ContainsKey(eventName))
        {
            (eventDic[eventName] as EventInfo<T>).actions += func;
        }
        else
        {
            eventDic.Add(eventName, new EventInfo<T>(func));
        }
    }

    public void AddEventListener(E_GEventType eventName, UnityAction func)
    {
        //如果已经存在关心事件的委托记录 直接添加即可
        if (eventDic.ContainsKey(eventName))
        {
            (eventDic[eventName] as EventInfo).actions += func;
        }
        else
        {
            eventDic.Add(eventName, new EventInfo(func));
        }
    }

    /// <summary>
    /// 移除事件监听者
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="func"></param>
    public void RemoveEventListener<T>(E_GEventType eventName, UnityAction<T> func)
    {
        if (eventDic.ContainsKey(eventName))
            (eventDic[eventName] as EventInfo<T>).actions -= func;
    }

    public void RemoveEventListener(E_GEventType eventName, UnityAction func)
    {
        if (eventDic.ContainsKey(eventName))
            (eventDic[eventName] as EventInfo).actions -= func;
    }

    /// <summary>
    /// 清空所有事件的监听
    /// </summary>
    public void ClearAll()
    {
        eventDic.Clear();
    }

    /// <summary>
    /// 清除指定某一个事件的所有监听
    /// </summary>
    /// <param name="eventName"></param>
    public void Clear(E_GEventType eventName)
    {
        if (eventDic.ContainsKey(eventName))
            eventDic.Remove(eventName);
    }
}


#region 局部事件
/// <summary>
/// 局部事件
/// </summary>
public enum E_SEventType
{

}


#endregion