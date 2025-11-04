public enum E_BuffUpdateTime
{
    //增加持续时间
    AddDuration,
    //刷新持续时间，增加层数
    Replace,
    //保留持续时间，增加层数
    KeepAndAddStack
}
public enum E_BuffRemoveStackUpdate
{
    ClearStack,
    ReduceStack,
}
public enum E_BuffCallBackType
{
    //这5个都是当...时的回调,不是代表他们的功能
    Create,
    AddStack,
    ReduceStack,
    Remove,
    Tick,

    //下面四个的顺序也就是他们的调用顺序
    BeforeDoDamage,
    BeforeGetDamage,
    AfterGetDamage,
    AfterDoDamage,
}