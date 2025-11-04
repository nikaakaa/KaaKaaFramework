using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 挂载在目标GameObject上的Buff处理器
/// </summary>
public class BuffHandler : MonoBehaviour
{
    private readonly List<BuffInfo> buffInfoList = new();
    private readonly Queue<BuffInfo> buffToAddQueue = new();
    [SerializeField] private readonly Queue<BuffInfo> buffToRemoveQueue = new();



    private void Update()
    {
        while (buffToAddQueue.Count > 0)
        {
            BuffInfo newBuff = buffToAddQueue.Dequeue();
            AddBuffSorted(newBuff);
        }
        while (buffToRemoveQueue.Count > 0)
        {
            BuffInfo buffInfo = buffToRemoveQueue.Dequeue();
            buffInfoList.Remove(buffInfo);
        }

        foreach(var buffInfo in buffInfoList)
        {
            buffInfo.buffData.TriggerBuffModuleOrder(E_BuffCallBackType.Tick, buffInfo);
        }
    }

    public IReadOnlyList<BuffInfo> BuffInfoList => buffInfoList.ToList();

    public void ClearBuff() => buffInfoList.Clear();

    public bool BuffExist(int id)=>buffInfoList.Any(buffList => buffList.buffData.id == id);

    public void TriggerCustom(E_BuffCallBackType buffCallBackType, params object[] customInfo)
    {
        foreach (var buffInfo in buffInfoList)
        {
            buffInfo.buffData.TriggerBuffModuleOrder(buffCallBackType, buffInfo, customInfo);
        }
    }

    

    public BuffInfo FindBuff(int buffId)
    {
        foreach (var buff in buffInfoList)
        {
            if (buff.buffData.id == buffId)
            {
                return buff;
            }
        }
        return null;
    }
    public bool AddBuff(string buffName, GameObject creator,int addStackCount= 0, int addTime = 0)
    {
        BuffData buffData = AddressablesMgr.Instance.LoadAssetSync<BuffData>(buffName);
        if (buffData != null)
        {
            return AddBuff(new BuffInfo
            {
                buffData = buffData,
                creator = creator,
                target = gameObject,
            },addStackCount,addTime);
        }

        return false;
    }
    /// <summary>
    /// 添加Buff,可以再分指责
    /// </summary>
    /// <param name="buffInfo"></param>
    /// <returns></returns>
    private bool AddBuff(BuffInfo buffInfo,int addStackCount=0,int addTime=0)
    {
        BuffInfo findBuffInfo = FindBuff(buffInfo.buffData.id);
        if(findBuffInfo != null)//buff存在
        {
            var ticker = (IBuffTicker)findBuffInfo;
            switch (findBuffInfo.buffData.buffUpdateTime)
            {
                case E_BuffUpdateTime.AddDuration:
                    ticker.DurationTimer += addTime;
                    break;
                case E_BuffUpdateTime.Replace:
                    ticker.DurationTimer = buffInfo.buffData.duration;
                    findBuffInfo.buffData.TriggerBuffModuleOrder(E_BuffCallBackType.Create, findBuffInfo);
                    break;
                case E_BuffUpdateTime.KeepAndAddStack:
                    if (ticker.CurStack < findBuffInfo.buffData.maxStack)
                    {
                        ticker.CurStack +=addStackCount;
                        findBuffInfo.buffData.TriggerBuffModuleOrder(E_BuffCallBackType.AddStack, findBuffInfo);
                    }
                    break;
            }
        }
        else//buff不存在CreateBuff
        {
            buffToAddQueue.Enqueue(buffInfo);
            var ticker = (IBuffTicker)buffInfo;
            ticker.TickTimer = buffInfo.buffData.tickTime;
            ticker.CurStack = addStackCount;
            ticker.DurationTimer = addTime;
            buffInfo.buffData.TriggerBuffModuleOrder(E_BuffCallBackType.Create, buffInfo);
            if(buffInfo.buffData.triggerTickOnCreate)
                buffInfo.buffData.TriggerBuffModuleOrder(E_BuffCallBackType.Tick, buffInfo);
        }
        return true;
    }

    // 将新的 BuffInfo 按优先级插入到列表中。
    private void AddBuffSorted(BuffInfo newBuff)
    {
        // 如果列表为空，直接添加
        if (buffInfoList.Count == 0)
        {
            buffInfoList.Add(newBuff);
            return;
        }

        // 遍历列表，找到正确的插入位置
        // 假设优先级越高，数值越大，排在越前面
        for (int i = 0; i < buffInfoList.Count; i++)
        {
            if (newBuff.buffData.priority > buffInfoList[i].buffData.priority)
            {
                // 找到了一个优先级比新 Buff 低的元素，在其索引处插入
                buffInfoList.Insert(i, newBuff);
                return;
            }
        }

        // 如果新 Buff 的优先级最低，添加到列表末尾
        buffInfoList.Add(newBuff);
    }

    /// <summary>
    /// 实际上是根据E_BuffRemoveStackUpdate来减少Buff的层数或者直接移除Buff
    /// </summary>
    /// <param name="buffInfo"></param>
    /// <returns></returns>
    public bool ReduceBuff(int id,int reduceStackCount=0,int reduceTime=0)
    {
        BuffInfo findBuffInfo = FindBuff(id);
        if (findBuffInfo == null)
        {
            return false;
        }

        IBuffTicker ticker = (IBuffTicker)findBuffInfo;
        switch (findBuffInfo.buffData.buffRemoveStackUpdate)
        {
            case E_BuffRemoveStackUpdate.ClearStack:
                ticker.CurStack = 0;
                ticker.DurationTimer = 0;
                findBuffInfo.buffData.TriggerBuffModuleOrder(E_BuffCallBackType.ReduceStack, findBuffInfo);  
                findBuffInfo.buffData.TriggerBuffModuleOrder(E_BuffCallBackType.Remove, findBuffInfo);
                buffToRemoveQueue.Enqueue(findBuffInfo);
                break;
            case E_BuffRemoveStackUpdate.ReduceStack:
                ticker.CurStack -= reduceStackCount;
                if (ticker.CurStack < 0)
                    ticker.CurStack = 0;//这里是因为TriggerBuffModule要用
                ticker.DurationTimer -= reduceTime;
                if (ticker.DurationTimer < 0)
                    ticker.DurationTimer = 0;
                findBuffInfo.buffData.TriggerBuffModuleOrder(E_BuffCallBackType.ReduceStack, findBuffInfo);
                if(ticker.CurStack<=0 || (!findBuffInfo.buffData.isForever && ticker.DurationTimer <= 0))
                {
                    findBuffInfo.buffData.TriggerBuffModuleOrder(E_BuffCallBackType.Remove, findBuffInfo);
                    buffToRemoveQueue.Enqueue(findBuffInfo);
                }
                break;
        }
        return true;
    }
    public bool RemoveBuff(int id)
    {
        BuffInfo findBuffInfo = FindBuff(id);
        if (findBuffInfo == null)
        {
            return false;
        }
        IBuffTicker ticker = (IBuffTicker)findBuffInfo;
        ticker.CurStack = 0;
        ticker.DurationTimer = 0;

        findBuffInfo.buffData.TriggerBuffModuleOrder(E_BuffCallBackType.ReduceStack, findBuffInfo);
        findBuffInfo.buffData.TriggerBuffModuleOrder(E_BuffCallBackType.Remove, findBuffInfo);
        buffToRemoveQueue.Enqueue(findBuffInfo);

        return true;
    }
}
