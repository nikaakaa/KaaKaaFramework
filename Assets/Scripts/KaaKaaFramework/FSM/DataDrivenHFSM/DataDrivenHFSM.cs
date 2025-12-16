using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public abstract class Transition: ScriptableObject
{
    public StateData fromStateData;
    public StateData toStateData;
    public Func<bool> condition;
    public abstract bool TryTransition();
}

public abstract class StateData : ScriptableObject
{
    public string stateName;
    public Dictionary<string,StateData> subStates = new Dictionary<string, StateData>();
    
}
public class HFSMRootMachineData:ScriptableObject
{
    
}
public class HFSMRootMachine
{
    
}