using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BuffModuleBase : ScriptableObject
{
    public E_BuffCallBackType buffCallBackType;
    public abstract void Apply(BuffInfo buffInfo, params object[] customInfo);
}
