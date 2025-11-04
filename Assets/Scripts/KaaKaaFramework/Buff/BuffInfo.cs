using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// buff运行时数据 
/// </summary>
[Serializable]
public class BuffInfo:IBuffTicker
{
    public BuffData buffData; // Buff数据
    public GameObject creator;// Buff的创建者
    public GameObject target; // Buff的目标
    public int DurationTimer => ((IBuffTicker)this).DurationTimer;
    public int TickTimer => ((IBuffTicker)this).TickTimer;//每多少回合触发一次
    public int CurStack => ((IBuffTicker)this).CurStack;

    int IBuffTicker.DurationTimer { get; set; }
    int IBuffTicker.TickTimer { get; set; } = 1;
    int IBuffTicker.CurStack { get; set; } = 1; // 默认栈数为1

    private Dictionary<E_BuffCallBackType,Action> _buffActions = new();//动态注册的委托

    /// <summary>
    /// 动态注册委托提高灵活性
    /// </summary>
    /// <param name="buffCallBackType"></param>
    /// <param name="action"></param>
    public void RegisterBuffAction(E_BuffCallBackType buffCallBackType, Action action)
    {
        if (!_buffActions.TryAdd(buffCallBackType, action))
        {
            _buffActions[buffCallBackType] += action;
        }
    }
    /// <summary>
    /// 出发运行时添加的Action
    /// </summary>
    /// <param name="buffCallBackType"></param>
    public void TriggerBuffAction(E_BuffCallBackType buffCallBackType)
    {
        if (_buffActions.TryGetValue(buffCallBackType, out var action))
        {
            action?.Invoke();
        }
    }
}

