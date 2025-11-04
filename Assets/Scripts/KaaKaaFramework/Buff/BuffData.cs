using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// buff静态配置数据
/// </summary>
[CreateAssetMenu(fileName = "BuffData", menuName = "Buff/Config/BuffData")]
public class BuffData : ScriptableObject
{
    public int id;
    public string buffName;
    public string description;
    public string iconName;
    public int priority;
    public int maxStack;
    public bool isVisible;
    public List<string> tags;
    //时间信息
    public bool isForever;     //策略模式控制参数，是否为永久Buff，控制Buff是否需要更新持续时间
    public int duration;      //持续多少回合
    public int tickTime = 1; //每多少回合触发一次
    //更新方式
    public E_BuffUpdateTime buffUpdateTime;//如何更新
    public E_BuffRemoveStackUpdate buffRemoveStackUpdate;//如何移除
    public bool triggerTickOnCreate; //策略模式控制参数，是否在OnCreate的时候也执行一次OnTick
    public List<BuffModuleBase> buffModuleList = new();// Buff模块列表

    public bool ExistBuffModule(E_BuffCallBackType buffCallBackType)
    {
        return buffModuleList.Exists(buff => buff.buffCallBackType == buffCallBackType);
    }

    public bool ExistTag(string tag)
    {
        return tags.Contains(tag);
    }

    /// <summary>
    /// 存在就触发,不存在的回调这里面已经判断
    /// 按priority顺序触发
    /// </summary>
    /// <param name="buffCallBackType"></param>
    /// <param name="runTimeInfo"></param>
    /// <param name="paramList"></param>
    public void TriggerBuffModuleOrder(E_BuffCallBackType buffCallBackType, BuffInfo runTimeInfo, params object[] paramList)
    {
        runTimeInfo.TriggerBuffAction(buffCallBackType);

        foreach (var buffModule in buffModuleList)
        {
            if (buffModule.buffCallBackType == buffCallBackType)
            {
                buffModule.Apply(runTimeInfo, paramList);
            }
        }
    }
}
