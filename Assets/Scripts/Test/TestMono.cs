using UnityEngine;
using UnityEngine.InputSystem;

public class TestMono : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActionAsset; // 使用与 InputMgr 相同的 asset
    
    private InputActionMap playerMap;
    private InputAction moveAction;
    private InputAction fireAction;
    private InputAction lookAction;
    
    // 保存回调引用以便正确取消订阅
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> onMovePerformed;
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> onFirePerformed;
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> onLookPerformed;
    
    private void Start()
    {
        // 优先使用序列化的 asset，如果没指定则从 InputMgr 获取
        // 这样就能确保使用与 InputMgr 相同的实例，改键会立即生效
        if (inputActionAsset == null)
        {
            inputActionAsset = InputMgr.Instance.GetInputActionAsset();
            if (inputActionAsset == null)
            {
                Debug.LogError("TestMono: 无法获取 InputActionAsset！请确保 InputMgr 已初始化");
                return;
            }
        }
        
        // 直接从 asset 获取 ActionMap 和 Actions
        playerMap = inputActionAsset.FindActionMap("Player");
        if (playerMap != null)
        {
            playerMap.Enable();
            
            moveAction = playerMap.FindAction("Move");
            fireAction = playerMap.FindAction("Fire");
            lookAction = playerMap.FindAction("Look");
            
            // 创建回调方法并保存引用
            onMovePerformed = (context) => { print("Move"); };
            onFirePerformed = (context) => { print("Fire"); };
            onLookPerformed = (context) => { print("Look"); };
            
            if (moveAction != null)
            {
                moveAction.performed += onMovePerformed;
            }
            
            if (fireAction != null)
            {
                fireAction.performed += onFirePerformed;
            }
            
            //if (lookAction != null)
            //{
            //    lookAction.performed += onLookPerformed;
            //}
        }
        else
        {
            Debug.LogError("TestMono: 找不到 Player ActionMap");
        }
    }
    
    private void OnDisable()
    {
        // 取消订阅事件
        if (moveAction != null && onMovePerformed != null)
        {
            moveAction.performed -= onMovePerformed;
        }
        if (fireAction != null && onFirePerformed != null)
        {
            fireAction.performed -= onFirePerformed;
        }
        if (lookAction != null && onLookPerformed != null)
        {
            lookAction.performed -= onLookPerformed;
        }
        
        // 禁用 ActionMap（禁用后事件不会再触发）
        if (playerMap != null)
        {
            playerMap.Disable();
        }
    }





    public void TestSave()
    {
        // 确保所有数据都写入到同一个合并文件，例如 "MyMasterSave"
        const string MERGED_FILE_NAME = "MyMasterSave";

        // --- 1. 保存/更新操作：创建 10 个独立的键 ---
        for (int i = 0; i < 10; i++)
        {
            TestSaveMgrData testSaveMgrData = new TestSaveMgrData();
            // 注意：因为每次循环都是新对象，所以 name 和 dic 里的数据都只包含 i 对应的值
            testSaveMgrData.name = "fuck";
            testSaveMgrData.dic.Add("张三" + i, new TestSaveMgrData.InClass { name = "张三", id = i });

            // 正确用法：
            // fileName = 统一的合并文件名
            // mergedKey = 每个数据片的独立 key
            string dataKey = "DataKey" + i; // 为每个数据片使用唯一的 Key

            SaveMgr.Instance.SaveData(testSaveMgrData, MERGED_FILE_NAME, mergedKey: dataKey);
        }

        // 此时，MERGED_FILE_NAME.json 文件中应该包含了 10 个键：DataKey0, DataKey1, ... DataKey9。

        // --- 2. 加载操作 ---

        // 加载 DataKey2 键下的数据
        string keyToLoad = "DataKey2";
        var testb = SaveMgr.Instance.LoadData<TestSaveMgrData>(MERGED_FILE_NAME, mergedKey: keyToLoad);

        // 预期输出：
        // 1. testb.name 为 "fuck"
        // 2. testb.dic 字典中只有一个键："张三2"

        print(testb.name);

        // 尝试访问张三2
        if (testb.dic.ContainsKey("张三2"))
        {
            print(testb.dic["张三2"].id); // 应该打印 2
        }
        else
        {
            Debug.LogError("字典中没有找到 key '张三2'，可能加载失败。");
        }
    }
}