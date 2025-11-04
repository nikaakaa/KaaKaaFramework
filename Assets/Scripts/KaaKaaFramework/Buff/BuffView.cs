using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffView : MonoBehaviour
{
    private BuffHandler buffHandler;


    private void Awake()
    {
        buffHandler = GetComponent<BuffHandler>();
    }
}
