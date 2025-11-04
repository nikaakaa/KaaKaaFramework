using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件类型 枚举
/// </summary>
public enum E_EventType 
{
    #region 手牌
    //传入index
    E_HandCardHovered,
    //传入index
    E_HandCardHoveredExit,
    //传入index
    E_HandCardDel,
    //传入index
    E_HandCardLeaveHandCardDeck,
    //传入index
    E_HandCardSelected,
    //传入index
    E_HandCardStartDrag,
    //传入index
    E_HandCardOnDrag,
    //传入index
    E_HandCardEndDrag,
    //传入index
    E_HandCardPointDown,
    //传入index
    E_HandCardPointUp,
    //传入index
    E_HandCardVisualScaleToNormal,
    //不传入
    E_HandCardCancel,//用于取消所有选中

    //传入HandCardBase
    E_HandCardToTableCard,
    #endregion


    //传入index
    E_HandCardSlotDel,






    

    //传入TableCardBase
    E_TableCardAboutToHandCard,//这个是用来通知HandCardDeck来创建一个slot准备
    //传入TableCardBase
    E_TableCardToHandCard,


    








    //传入InteractableSceneObj
    E_PlayerEnterInteractableSceneObjFront,
    //传入InteractableSceneObj
    E_PlayerEnterInteractableSceneObj,
    //传入InteractableSceneObj
    E_PlayerExitInteractableSceneObj,
    //传入InteractableSceneObj
    E_PlayerExitInteractableSceneObjFront,







    //传入Player
    E_PlayerEndTurn,

}

