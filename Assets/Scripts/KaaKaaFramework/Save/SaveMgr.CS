using System;
using LitJson;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 保存数据类型枚举
/// </summary>
public enum SaveType
{
    /// <summary>
    /// JSON文件保存
    /// </summary>
    Json,
    /// <summary>
    /// Unity PlayerPrefs保存（适用于简单数据）
    /// </summary>
    PlayerPrefs,
    /// <summary>
    /// 二进制文件保存（未来扩展）
    /// </summary>
    Binary,
}

/// <summary>
/// 通用保存管理器
/// 支持多种保存方式：JSON文件、PlayerPrefs等
/// </summary>
public class SaveMgr : BaseManager<SaveMgr>
{
    /// <summary>
    /// 默认保存路径
    /// </summary>
    public string DefaultSavePath => Path.Combine(Application.persistentDataPath, "SaveData");

    /// <summary>
    /// 默认StreamingAssets路径
    /// </summary>
    public string DefaultStreamingAssetsPath => Path.Combine(Application.streamingAssetsPath, "SaveData");

    private SaveMgr()
    {
        // 确保保存目录存在
        if (!Directory.Exists(DefaultSavePath))
        {
            Directory.CreateDirectory(DefaultSavePath);
        }
    }

    #region --- JSON文件保存（内部实现，对外隐藏）---

    /// <summary>
    /// 保存数据为JSON文件（内部实现）
    /// </summary>
    private void SaveJson<T>(T data, string fileName, JsonType jsonType = JsonType.LitJson, bool useStreamingAssets = false)
    {
        try
        {
            string path = useStreamingAssets ? DefaultStreamingAssetsPath : DefaultSavePath;
            string fullPath = Path.Combine(path, fileName + ".json");
            
            // 确保目录存在
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // 序列化数据
            string jsonStr = "";
            switch (jsonType)
            {
                case JsonType.JsonUtlity:
                    jsonStr = JsonUtility.ToJson(data);
                    break;
                case JsonType.LitJson:
                    jsonStr = JsonMapper.ToJson(data);
                    break;
            }
            
            // 写入文件
            File.WriteAllText(fullPath, jsonStr);
            Debug.Log($"保存JSON成功: {fullPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"保存JSON失败: {fileName}, 错误: {e.Message}");
        }
    }

    /// <summary>
    /// 从JSON文件加载数据（内部实现）
    /// </summary>
    private T LoadJson<T>(string fileName, JsonType jsonType = JsonType.LitJson) where T : new()
    {
        try
        {
            // 优先从StreamingAssets读取（默认配置）
            string streamingPath = Path.Combine(DefaultStreamingAssetsPath, fileName + ".json");
            if (File.Exists(streamingPath))
            {
                T data = LoadJsonFromPath<T>(streamingPath, jsonType);
                if (data != null)
                    return data;
            }

            // 从PersistentDataPath读取（用户数据）
            string persistentPath = Path.Combine(DefaultSavePath, fileName + ".json");
            if (File.Exists(persistentPath))
            {
                return LoadJsonFromPath<T>(persistentPath, jsonType);
            }

            Debug.LogWarning($"JSON文件不存在，返回默认值: {fileName}");
            return new T();
        }
        catch (Exception e)
        {
            Debug.LogError($"加载JSON失败: {fileName}, 错误: {e.Message}");
            return new T();
        }
    }

    /// <summary>
    /// 从指定路径加载JSON
    /// </summary>
    private T LoadJsonFromPath<T>(string fullPath, JsonType jsonType) where T : new()
    {
        try
        {
            if (!File.Exists(fullPath))
            {
                return new T();
            }

            string jsonStr = File.ReadAllText(fullPath);
            if (string.IsNullOrEmpty(jsonStr))
            {
                return new T();
            }

            T data = default(T);
            
            switch (jsonType)
            {
                case JsonType.JsonUtlity:
                    data = JsonUtility.FromJson<T>(jsonStr);
                    break;
                case JsonType.LitJson:
                    data = JsonMapper.ToObject<T>(jsonStr);
                    break;
            }

            return data != null ? data : new T();
        }
        catch (Exception e)
        {
            Debug.LogError($"从路径加载JSON失败: {fullPath}, 错误: {e.Message}");
            return new T();
        }
    }

    /// <summary>
    /// 检查JSON文件是否存在（内部实现）
    /// </summary>
    private bool JsonFileExists(string fileName)
    {
        string streamingPath = Path.Combine(DefaultStreamingAssetsPath, fileName + ".json");
        string persistentPath = Path.Combine(DefaultSavePath, fileName + ".json");
        return File.Exists(streamingPath) || File.Exists(persistentPath);
    }

    /// <summary>
    /// 删除JSON文件（内部实现）
    /// </summary>
    private bool DeleteJsonFile(string fileName)
    {
        try
        {
            string path = Path.Combine(DefaultSavePath, fileName + ".json");
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"删除JSON文件成功: {fileName}");
                return true;
            }
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"删除JSON文件失败: {fileName}, 错误: {e.Message}");
            return false;
        }
    }

    #endregion

    #region --- 合并JSON文件保存（内部实现，对外隐藏）---

    /// <summary>
    /// 合并保存数据结构（内部使用，不对外暴露）
    /// </summary>
    [Serializable]
    private class MergedSaveData
    {
        /// <summary>
        /// 存储的数据字典：key=文件名，value=序列化后的JSON字符串
        /// </summary>
        private Dictionary<string, string> dataDict = new Dictionary<string, string>();

        /// <summary>
        /// 用于序列化的列表（LitJson不支持Dictionary直接序列化）
        /// </summary>
        public List<MergedSaveItem> dataList = new List<MergedSaveItem>();

        /// <summary>
        /// 获取或设置数据字典
        /// </summary>
        public Dictionary<string, string> DataDict => dataDict;

        /// <summary>
        /// 保存前转换为列表
        /// </summary>
        public void SaveToList()
        {
            dataList.Clear();
            foreach (var kvp in dataDict)
            {
                dataList.Add(new MergedSaveItem { key = kvp.Key, value = kvp.Value });
            }
        }

        /// <summary>
        /// 加载后从列表恢复字典
        /// </summary>
        public void LoadFromList()
        {
            dataDict.Clear();
            foreach (var item in dataList)
            {
                dataDict[item.key] = item.value;
            }
        }
    }

    /// <summary>
    /// 合并保存项（内部使用，用于序列化）
    /// </summary>
    [Serializable]
    private class MergedSaveItem
    {
        public string key;
        public string value;
    }

    /// <summary>
    /// 保存多个数据到合并的JSON文件（内部实现）
    /// </summary>
    private void SaveMergedJson(Dictionary<string, object> dataDict, string mergedFileName, JsonType jsonType = JsonType.LitJson, bool useStreamingAssets = false)
    {
        try
        {
            MergedSaveData mergedData = new MergedSaveData();

            // 将每个数据对象序列化为JSON字符串
            foreach (var kvp in dataDict)
            {
                string jsonStr = "";
                switch (jsonType)
                {
                    case JsonType.JsonUtlity:
                        jsonStr = JsonUtility.ToJson(kvp.Value);
                        break;
                    case JsonType.LitJson:
                        jsonStr = JsonMapper.ToJson(kvp.Value);
                        break;
                }
                mergedData.DataDict[kvp.Key] = jsonStr;
            }

            // 保存合并数据
            mergedData.SaveToList();
            SaveJson(mergedData, mergedFileName, jsonType, useStreamingAssets);
            Debug.Log($"合并保存成功: {mergedFileName}，包含 {dataDict.Count} 个数据");
        }
        catch (Exception e)
        {
            Debug.LogError($"合并保存失败: {mergedFileName}, 错误: {e.Message}");
        }
    }

    /// <summary>
    /// 从合并的JSON文件中读取单个数据（内部实现）
    /// </summary>
    private T LoadFromMergedJson<T>(string mergedFileName, string key, JsonType jsonType) where T : new()
    {
        try
        {
            // 加载合并数据
            MergedSaveData mergedData = LoadJson<MergedSaveData>(mergedFileName, jsonType);
            if (mergedData == null)
            {
                return new T();
            }

            mergedData.LoadFromList();

            // 查找指定key的数据
            if (!mergedData.DataDict.ContainsKey(key))
            {
                Debug.LogWarning($"合并文件中不存在key: {key}");
                return new T();
            }

            // 反序列化JSON字符串
            string jsonStr = mergedData.DataDict[key];
            T data = default(T);

            switch (jsonType)
            {
                case JsonType.JsonUtlity:
                    data = JsonUtility.FromJson<T>(jsonStr);
                    break;
                case JsonType.LitJson:
                    data = JsonMapper.ToObject<T>(jsonStr);
                    break;
            }

            return data != null ? data : new T();
        }
        catch (Exception e)
        {
            Debug.LogError($"从合并文件加载失败: {mergedFileName}, key: {key}, 错误: {e.Message}");
            return new T();
        }
    }

    /// <summary>
    /// 更新合并JSON文件中的单个数据（内部实现）
    /// </summary>
    private bool UpdateMergedJson<T>(string mergedFileName, string key, T data, JsonType jsonType, bool createIfNotExists)
    {
        try
        {
            // 加载现有的合并数据
            MergedSaveData mergedData = LoadJson<MergedSaveData>(mergedFileName, jsonType);
            if (mergedData == null)
            {
                mergedData = new MergedSaveData();
            }

            mergedData.LoadFromList();

            // 检查key是否存在
            if (!mergedData.DataDict.ContainsKey(key))
            {
                if (!createIfNotExists)
                {
                    Debug.LogWarning($"合并文件中不存在key: {key}，且不允许创建");
                    return false;
                }
            }

            // 序列化新数据
            string jsonStr = "";
            switch (jsonType)
            {
                case JsonType.JsonUtlity:
                    jsonStr = JsonUtility.ToJson(data);
                    break;
                case JsonType.LitJson:
                    jsonStr = JsonMapper.ToJson(data);
                    break;
            }

            // 更新数据
            mergedData.DataDict[key] = jsonStr;

            // 保存合并数据
            mergedData.SaveToList();
            SaveJson(mergedData, mergedFileName, jsonType);
            Debug.Log($"更新合并文件成功: {mergedFileName}, key: {key}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"更新合并文件失败: {mergedFileName}, key: {key}, 错误: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 从合并JSON文件中删除指定的数据（内部实现）
    /// </summary>
    private bool RemoveFromMergedJson(string mergedFileName, string key, JsonType jsonType = JsonType.LitJson)
    {
        try
        {
            // 加载现有的合并数据
            MergedSaveData mergedData = LoadJson<MergedSaveData>(mergedFileName, jsonType);
            if (mergedData == null)
            {
                Debug.LogWarning($"合并文件不存在: {mergedFileName}");
                return false;
            }

            mergedData.LoadFromList();

            // 删除指定key
            if (!mergedData.DataDict.Remove(key))
            {
                Debug.LogWarning($"合并文件中不存在key: {key}");
                return false;
            }

            // 保存合并数据
            mergedData.SaveToList();
            SaveJson(mergedData, mergedFileName, jsonType);
            Debug.Log($"从合并文件删除成功: {mergedFileName}, key: {key}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"从合并文件删除失败: {mergedFileName}, key: {key}, 错误: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 检查合并JSON文件中是否存在指定的key（内部实现）
    /// </summary>
    private bool ExistsInMergedJson(string mergedFileName, string key, JsonType jsonType = JsonType.LitJson)
    {
        try
        {
            MergedSaveData mergedData = LoadJson<MergedSaveData>(mergedFileName, jsonType);
            if (mergedData == null)
            {
                return false;
            }

            mergedData.LoadFromList();
            return mergedData.DataDict.ContainsKey(key);
        }
        catch (Exception e)
        {
            Debug.LogError($"检查合并文件key失败: {mergedFileName}, key: {key}, 错误: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取合并JSON文件中所有key的列表（内部实现）
    /// </summary>
    private List<string> GetAllKeysFromMergedJson(string mergedFileName, JsonType jsonType = JsonType.LitJson)
    {
        try
        {
            MergedSaveData mergedData = LoadJson<MergedSaveData>(mergedFileName, jsonType);
            if (mergedData == null)
            {
                return new List<string>();
            }

            mergedData.LoadFromList();
            return new List<string>(mergedData.DataDict.Keys);
        }
        catch (Exception e)
        {
            Debug.LogError($"获取合并文件keys失败: {mergedFileName}, 错误: {e.Message}");
            return new List<string>();
        }
    }

    #endregion

    #region --- PlayerPrefs保存（内部实现，对外隐藏）---

    /// <summary>
    /// 保存对象到PlayerPrefs（转换为JSON字符串）（内部实现）
    /// </summary>
    private void SavePlayerPrefsObject<T>(string key, T data, JsonType jsonType = JsonType.LitJson)
    {
        string jsonStr = "";
        switch (jsonType)
        {
            case JsonType.JsonUtlity:
                jsonStr = JsonUtility.ToJson(data);
                break;
            case JsonType.LitJson:
                jsonStr = JsonMapper.ToJson(data);
                break;
        }
        PlayerPrefs.SetString(key, jsonStr);
        PlayerPrefs.Save();
    }


    /// <summary>
    /// 从PlayerPrefs加载对象（从JSON字符串反序列化）（内部实现）
    /// </summary>
    private T LoadPlayerPrefsObject<T>(string key, JsonType jsonType = JsonType.LitJson) where T : new()
    {
        string jsonStr = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(jsonStr))
            return new T();

        T data = default(T);
        try
        {
            switch (jsonType)
            {
                case JsonType.JsonUtlity:
                    data = JsonUtility.FromJson<T>(jsonStr);
                    break;
                case JsonType.LitJson:
                    data = JsonMapper.ToObject<T>(jsonStr);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"PlayerPrefs反序列化失败: {key}, 错误: {e.Message}");
            return new T();
        }

        return data != null ? data : new T();
    }

    /// <summary>
    /// 检查PlayerPrefs中是否存在指定key（内部实现）
    /// </summary>
    private bool HasPlayerPrefsKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    /// <summary>
    /// 删除PlayerPrefs中的指定key（内部实现）
    /// </summary>
    private void DeletePlayerPrefsKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    #endregion

    #region --- 统一接口 ---

    #endregion

    #region --- 对外统一接口（只暴露这些方法给用户使用）---

    /// <summary>
    /// 【统一保存接口】保存数据
    /// 支持普通保存和合并保存两种模式
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="data">要保存的数据</param>
    /// <param name="fileName">文件名（普通保存）或合并文件名（合并保存）</param>
    /// <param name="saveType">保存类型（Json/PlayerPrefs/Binary）</param>
    /// <param name="jsonType">JSON序列化类型（仅Json保存时有效）</param>
    /// <param name="mergedKey">合并文件的key，如果提供则更新合并文件中的指定数据</param>
    /// <example>
    /// // 普通保存
    /// SaveData(playerData, "PlayerData");
    /// 
    /// // 更新合并文件中的数据
    /// SaveData(playerData, "GameSave", mergedKey: "PlayerData");
    /// </example>
    public void SaveData<T>(T data, string fileName, SaveType saveType = SaveType.Json, JsonType jsonType = JsonType.LitJson, string mergedKey = null)
    {
        if (data == null)
        {
            Debug.LogWarning($"保存数据失败: {fileName}，数据为null");
            return;
        }

        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("文件名不能为空");
            return;
        }

        // 如果提供了mergedKey，说明是更新合并文件中的数据
        if (!string.IsNullOrEmpty(mergedKey))
        {
            UpdateMergedJson(fileName, mergedKey, data, jsonType, true);
            return;
        }

        // 普通保存
        switch (saveType)
        {
            case SaveType.Json:
                SaveJson(data, fileName, jsonType);
                break;
            case SaveType.PlayerPrefs:
                SavePlayerPrefsObject(fileName, data, jsonType);
                break;
            case SaveType.Binary:
                Debug.LogWarning("二进制保存功能尚未实现");
                break;
        }
    }

    /// <summary>
    /// 【统一保存接口】保存多个数据到合并文件（无需手动创建字典）
    /// </summary>
    /// <param name="mergedFileName">合并后的文件名（不含扩展名）</param>
    /// <param name="items">要保存的数据项，使用元组语法 (key, value)</param>
    /// <example>
    /// SaveData("GameSave", 
    ///     ("PlayerData", playerData),
    ///     ("InventoryData", inventoryData),
    ///     ("QuestData", questData)
    /// );
    /// </example>
    public void SaveData(string mergedFileName, params (string key, object value)[] items)
    {
        if (items == null || items.Length == 0)
        {
            Debug.LogWarning("保存合并文件时，数据项数组为空");
            return;
        }

        // 将元组数组转换为字典
        Dictionary<string, object> dataDict = new Dictionary<string, object>();
        foreach (var (key, value) in items)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("保存合并文件时，发现空的key，已跳过");
                continue;
            }
            dataDict[key] = value;
        }

        if (dataDict.Count == 0)
        {
            Debug.LogWarning("保存合并文件时，没有有效的数据项");
            return;
        }

        SaveMergedJson(dataDict, mergedFileName, JsonType.LitJson, false);
    }

    /// <summary>
    /// 【统一加载接口】加载数据
    /// 支持普通加载和从合并文件加载两种模式
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="fileName">文件名（普通加载）或合并文件名（从合并文件加载）</param>
    /// <param name="saveType">保存类型（Json/PlayerPrefs/Binary）</param>
    /// <param name="jsonType">JSON反序列化类型（仅Json加载时有效）</param>
    /// <param name="mergedKey">合并文件的key，如果提供则从合并文件中读取指定数据</param>
    /// <returns>加载的数据，如果不存在返回默认值</returns>
    /// <example>
    /// // 普通加载
    /// PlayerData player = LoadData&lt;PlayerData&gt;("PlayerData");
    /// 
    /// // 从合并文件加载
    /// PlayerData player = LoadData&lt;PlayerData&gt;("GameSave", mergedKey: "PlayerData");
    /// </example>
    public T LoadData<T>(string fileName, SaveType saveType = SaveType.Json, JsonType jsonType = JsonType.LitJson, string mergedKey = null) where T : new()
    {
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("文件名不能为空");
            return new T();
        }

        // 如果提供了mergedKey，说明是从合并文件读取
        if (!string.IsNullOrEmpty(mergedKey))
        {
            return LoadFromMergedJson<T>(fileName, mergedKey, jsonType);
        }

        // 普通加载
        switch (saveType)
        {
            case SaveType.Json:
                return LoadJson<T>(fileName, jsonType);
            case SaveType.PlayerPrefs:
                return LoadPlayerPrefsObject<T>(fileName, jsonType);
            case SaveType.Binary:
                Debug.LogWarning("二进制加载功能尚未实现");
                return new T();
            default:
                return new T();
        }
    }

    /// <summary>
    /// 【统一接口】检查数据是否存在
    /// </summary>
    /// <param name="fileName">文件名或合并文件名</param>
    /// <param name="saveType">保存类型</param>
    /// <param name="mergedKey">合并文件的key，如果提供则检查合并文件中的指定数据</param>
    /// <returns>是否存在</returns>
    /// <example>
    /// // 检查普通文件
    /// bool exists = DataExists("PlayerData");
    /// 
    /// // 检查合并文件中的key
    /// bool exists = DataExists("GameSave", mergedKey: "PlayerData");
    /// </example>
    public bool DataExists(string fileName, SaveType saveType = SaveType.Json, string mergedKey = null)
    {
        // 如果提供了mergedKey，检查合并文件中的key
        if (!string.IsNullOrEmpty(mergedKey))
        {
            return ExistsInMergedJson(fileName, mergedKey, JsonType.LitJson);
        }

        // 检查普通文件
        switch (saveType)
        {
            case SaveType.Json:
                return JsonFileExists(fileName);
            case SaveType.PlayerPrefs:
                return HasPlayerPrefsKey(fileName);
            default:
                return false;
        }
    }

    /// <summary>
    /// 【统一接口】删除数据
    /// </summary>
    /// <param name="fileName">文件名或合并文件名</param>
    /// <param name="saveType">保存类型</param>
    /// <param name="mergedKey">合并文件的key，如果提供则删除合并文件中的指定数据</param>
    /// <returns>是否删除成功</returns>
    /// <example>
    /// // 删除普通文件
    /// DeleteData("PlayerData");
    /// 
    /// // 删除合并文件中的key
    /// DeleteData("GameSave", mergedKey: "PlayerData");
    /// </example>
    public bool DeleteData(string fileName, SaveType saveType = SaveType.Json, string mergedKey = null)
    {
        // 如果提供了mergedKey，删除合并文件中的key
        if (!string.IsNullOrEmpty(mergedKey))
        {
            return RemoveFromMergedJson(fileName, mergedKey, JsonType.LitJson);
        }

        // 删除普通文件
        switch (saveType)
        {
            case SaveType.Json:
                return DeleteJsonFile(fileName);
            case SaveType.PlayerPrefs:
                DeletePlayerPrefsKey(fileName);
                return true;
            default:
                return false;
        }
    }

    #endregion

    #region --- 工具方法 ---

    /// <summary>
    /// 获取保存目录下的所有文件
    /// </summary>
    public string[] GetAllSaveFiles(string extension = ".json")
    {
        if (!Directory.Exists(DefaultSavePath))
            return new string[0];

        string[] files = Directory.GetFiles(DefaultSavePath, "*" + extension);
        for (int i = 0; i < files.Length; i++)
        {
            files[i] = Path.GetFileNameWithoutExtension(files[i]);
        }
        return files;
    }

    /// <summary>
    /// 获取保存文件的大小（字节）
    /// </summary>
    public long GetFileSize(string fileName, SaveType saveType = SaveType.Json)
    {
        if (saveType == SaveType.Json)
        {
            string path = Path.Combine(DefaultSavePath, fileName + ".json");
            if (File.Exists(path))
            {
                FileInfo fileInfo = new FileInfo(path);
                return fileInfo.Length;
            }
        }
        return 0;
    }

    #endregion
}