using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region Architecture
public interface IArchitecture
{

}

public class Architecture<T> : IArchitecture where T : Architecture<T>,new()
{

}
#endregion