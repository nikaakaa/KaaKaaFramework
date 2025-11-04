using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSaveMgrData
{
    public string name;
    public int id;
    public bool sex;
    public Dictionary<string, InClass> dic = new();
    public class InClass
    {
        public string name;
        public int id;
        public bool sex;
    }

}

