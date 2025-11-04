# SaveMgr 统一接口说明

## 📋 设计原则

**只暴露统一的接口，隐藏所有内部实现细节**

所有内部类和方法都已封装为 `private`，用户只需要知道以下统一的接口即可。

---

## 🎯 对外统一接口（只需要记住这5个方法）

### 1. **SaveData** - 统一保存接口

#### 单个数据保存
```csharp
// 普通保存（单个文件）
SaveMgr.Instance.SaveData(playerData, "PlayerData");

// PlayerPrefs保存
SaveMgr.Instance.SaveData(settings, "Settings", SaveType.PlayerPrefs);

// 更新合并文件中的单个数据
SaveMgr.Instance.SaveData(playerData, "GameSave", mergedKey: "PlayerData");
```

#### 合并保存（多个数据到一个文件）
```csharp
// 保存多个数据到合并文件（无需创建字典）
SaveMgr.Instance.SaveData("GameSave",
    ("PlayerData", playerData),
    ("InventoryData", inventoryData),
    ("QuestData", questData)
);
```

### 2. **LoadData** - 统一加载接口

```csharp
// 普通加载
PlayerData player = SaveMgr.Instance.LoadData<PlayerData>("PlayerData");

// PlayerPrefs加载
SettingsData settings = SaveMgr.Instance.LoadData<SettingsData>("Settings", SaveType.PlayerPrefs);

// 从合并文件加载
PlayerData player = SaveMgr.Instance.LoadData<PlayerData>("GameSave", mergedKey: "PlayerData");
```

### 3. **DataExists** - 统一检查接口

```csharp
// 检查普通文件
bool exists = SaveMgr.Instance.DataExists("PlayerData");

// 检查合并文件中的key
bool exists = SaveMgr.Instance.DataExists("GameSave", mergedKey: "PlayerData");
```

### 4. **DeleteData** - 统一删除接口

```csharp
// 删除普通文件
SaveMgr.Instance.DeleteData("PlayerData");

// 删除合并文件中的key
SaveMgr.Instance.DeleteData("GameSave", mergedKey: "PlayerData");
```

### 5. **工具方法**（可选）

```csharp
// 获取所有保存文件
string[] files = SaveMgr.Instance.GetAllSaveFiles();

// 获取文件大小
long size = SaveMgr.Instance.GetFileSize("PlayerData");
```

---

## 📊 接口对比表

| 功能 | 统一接口 | 说明 |
|-----|---------|------|
| 保存单个数据 | `SaveData<T>(data, fileName, ...)` | 支持Json/PlayerPrefs |
| 保存多个数据 | `SaveData(fileName, (key, value)...)` | 合并保存，无需字典 |
| 更新合并文件 | `SaveData<T>(data, fileName, mergedKey: key)` | 通过mergedKey参数 |
| 加载数据 | `LoadData<T>(fileName, ...)` | 统一加载接口 |
| 从合并文件加载 | `LoadData<T>(fileName, mergedKey: key)` | 通过mergedKey参数 |
| 检查存在 | `DataExists(fileName, mergedKey: key)` | 统一检查接口 |
| 删除数据 | `DeleteData(fileName, mergedKey: key)` | 统一删除接口 |

---

## 🔒 已隐藏的内部实现

以下方法和类已封装为 `private`，用户无需关心：

### 内部类
- ❌ `MergedSaveData` - 合并保存数据结构
- ❌ `MergedSaveItem` - 合并保存项

### 内部方法
- ❌ `SaveJson` - JSON保存实现
- ❌ `LoadJson` - JSON加载实现
- ❌ `SaveMergedJson` - 合并保存实现
- ❌ `LoadFromMergedJson` - 从合并文件加载实现
- ❌ `UpdateMergedJson` - 更新合并文件实现
- ❌ `RemoveFromMergedJson` - 删除合并文件数据实现
- ❌ `ExistsInMergedJson` - 检查合并文件key实现
- ❌ `SavePlayerPrefsObject` - PlayerPrefs保存实现
- ❌ `LoadPlayerPrefsObject` - PlayerPrefs加载实现
- ❌ 其他所有内部辅助方法

---

## 💡 使用示例

### 完整示例

```csharp
using UnityEngine;

public class GameSaveExample : MonoBehaviour
{
    [Serializable]
    public class PlayerData
    {
        public string playerName;
        public int level;
    }

    [Serializable]
    public class InventoryData
    {
        public List<string> items;
    }

    void Start()
    {
        // === 1. 保存游戏数据（合并保存） ===
        PlayerData player = new PlayerData { playerName = "玩家1", level = 10 };
        InventoryData inventory = new InventoryData { items = new List<string> { "剑", "盾" } };
        
        SaveMgr.Instance.SaveData("GameSave",
            ("PlayerData", player),
            ("InventoryData", inventory)
        );

        // === 2. 加载游戏数据 ===
        PlayerData loadedPlayer = SaveMgr.Instance.LoadData<PlayerData>("GameSave", mergedKey: "PlayerData");
        InventoryData loadedInventory = SaveMgr.Instance.LoadData<InventoryData>("GameSave", mergedKey: "InventoryData");

        // === 3. 更新单个数据 ===
        loadedPlayer.level++;
        SaveMgr.Instance.SaveData(loadedPlayer, "GameSave", mergedKey: "PlayerData");

        // === 4. 检查数据是否存在 ===
        if (SaveMgr.Instance.DataExists("GameSave", mergedKey: "PlayerData"))
        {
            Debug.Log("玩家数据存在");
        }

        // === 5. 删除数据 ===
        SaveMgr.Instance.DeleteData("GameSave", mergedKey: "OldData");
    }
}
```

---

## 🎯 总结

**只需要记住5个接口：**

1. ✅ **SaveData** - 保存（支持单个/多个/合并）
2. ✅ **LoadData** - 加载（支持单个/合并）
3. ✅ **DataExists** - 检查存在
4. ✅ **DeleteData** - 删除
5. ✅ **GetAllSaveFiles / GetFileSize** - 工具方法（可选）

**所有内部实现都已隐藏，接口统一、简洁、易用！**

