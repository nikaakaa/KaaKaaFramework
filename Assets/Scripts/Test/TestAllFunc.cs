using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestAllFunc : MonoBehaviour
{
    public void TestSave()
    {
        // 确保所有数据都写入到同一个合并文件，例如 "MyMasterSave"
        const string MERGED_FILE_NAME = "MyMasterSave";

        // --- 1. 保存/更新操作：创建 10 个独立的键 ---
        for (int i = 0; i < 10; i++)
        {
            TestSaveMgrData testSaveMgrData = new TestSaveMgrData();
            testSaveMgrData.name = "fuck";
            testSaveMgrData.dic.Add("张三" + i, new TestSaveMgrData.InClass { name = "张三", id = i });

            string dataKey = "DataKey" + i;

            SaveMgr.Instance.SaveData(testSaveMgrData, MERGED_FILE_NAME, mergedKey: dataKey);
        }

        // --- 2. 加载操作 ---
        string keyToLoad = "DataKey2";
        var testb = SaveMgr.Instance.LoadData<TestSaveMgrData>(MERGED_FILE_NAME, mergedKey: keyToLoad);

        print(testb.name);

        if (testb.dic.ContainsKey("张三2"))
        {
            print(testb.dic["张三2"].id);
        }
        else
        {
            Debug.LogError("字典中没有找到 key '张三2'，可能加载失败。");
        }
    }
}
