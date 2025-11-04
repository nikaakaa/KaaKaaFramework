using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBuffTicker
{
    int DurationTimer { get; set; }//持续多少回合
    int TickTimer { get; set; }//每多少回合触发一次
    int CurStack { get; set; }//当前层数
}
