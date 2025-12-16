using UnityEngine;

namespace KaaKaaFramework.HFSM
{
    /// <summary>
    /// HFSM运行器组件 - 挂载到GameObject上运行状态机
    /// </summary>
    public class HFSMRunner : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField]
        private HFSMConfigAsset configAsset;

        [SerializeField]
        private HFSMBehaviourConfigAsset behaviourConfigAsset;

        [SerializeField]
        private bool useCodeConfig = false;

        [Header("运行设置")]
        [SerializeField]
        private bool autoStart = true;

        [SerializeField]
        private bool enableFixedUpdate = false;

        [SerializeField]
        private bool enableLateUpdate = false;

        [Header("调试")]
        [SerializeField]
        private bool debugMode = false;

        private HierarchicalStateMachine _stateMachine;
        private bool _isRunning = false;

        public HierarchicalStateMachine StateMachine => _stateMachine;
        public HFSMBlackboard Blackboard => _stateMachine?.Blackboard;
        public bool IsRunning => _isRunning;
        public string CurrentStateName => _stateMachine?.CurrentState?.StateName ?? "None";

        #region Unity Lifecycle

        private void Awake()
        {
            if (!useCodeConfig)
            {
                InitializeFromConfig();
            }
        }

        private void Start()
        {
            if (autoStart && _stateMachine != null)
            {
                StartStateMachine();
            }
        }

        private void Update()
        {
            if (_isRunning && _stateMachine != null)
            {
                _stateMachine.OnUpdate();
            }
        }

        private void FixedUpdate()
        {
            if (enableFixedUpdate && _isRunning && _stateMachine != null)
            {
                _stateMachine.OnFixedUpdate();
            }
        }

        private void LateUpdate()
        {
            if (enableLateUpdate && _isRunning && _stateMachine != null)
            {
                _stateMachine.OnLateUpdate();
            }
        }

        private void OnDestroy()
        {
            StopStateMachine();
        }

        #endregion

        #region Public API

        /// <summary>
        /// 从配置资源初始化状态机
        /// </summary>
        public void InitializeFromConfig()
        {
            if (behaviourConfigAsset != null)
            {
                _stateMachine = behaviourConfigAsset.CreateStateMachine(gameObject);
            }
            else if (configAsset != null)
            {
                _stateMachine = configAsset.CreateStateMachine();
            }
            else
            {
                Debug.LogWarning("[HFSM Runner] No config asset assigned!");
            }
        }

        /// <summary>
        /// 使用代码配置初始化状态机
        /// </summary>
        public void Initialize(HierarchicalStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        /// <summary>
        /// 启动状态机
        /// </summary>
        public void StartStateMachine()
        {
            if (_stateMachine == null)
            {
                Debug.LogError("[HFSM Runner] State machine is not initialized!");
                return;
            }

            if (_isRunning)
            {
                Debug.LogWarning("[HFSM Runner] State machine is already running!");
                return;
            }

            _isRunning = true;
            _stateMachine.OnEnter();

            if (debugMode)
            {
                Debug.Log($"[HFSM Runner] Started state machine: {_stateMachine.StateName}");
            }
        }

        /// <summary>
        /// 停止状态机
        /// </summary>
        public void StopStateMachine()
        {
            if (!_isRunning || _stateMachine == null)
                return;

            _stateMachine.OnExit();
            _isRunning = false;

            if (debugMode)
            {
                Debug.Log($"[HFSM Runner] Stopped state machine: {_stateMachine.StateName}");
            }
        }

        /// <summary>
        /// 重启状态机
        /// </summary>
        public void RestartStateMachine()
        {
            StopStateMachine();
            StartStateMachine();
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        public void ChangeState(string stateName)
        {
            _stateMachine?.ChangeState(stateName);
        }

        #endregion

        #region Blackboard Helpers

        public void SetBool(string key, bool value) => Blackboard?.Set(key, value);
        public void SetInt(string key, int value) => Blackboard?.Set(key, value);
        public void SetFloat(string key, float value) => Blackboard?.Set(key, value);
        public void SetString(string key, string value) => Blackboard?.Set(key, value);
        
        public bool GetBool(string key, bool defaultValue = false) => Blackboard?.Get(key, defaultValue) ?? defaultValue;
        public int GetInt(string key, int defaultValue = 0) => Blackboard?.Get(key, defaultValue) ?? defaultValue;
        public float GetFloat(string key, float defaultValue = 0f) => Blackboard?.Get(key, defaultValue) ?? defaultValue;
        public string GetString(string key, string defaultValue = "") => Blackboard?.Get(key, defaultValue) ?? defaultValue;

        /// <summary>
        /// 触发Trigger参数
        /// </summary>
        public void SetTrigger(string key)
        {
            Blackboard?.Set(key, true);
        }

        /// <summary>
        /// 重置Trigger参数
        /// </summary>
        public void ResetTrigger(string key)
        {
            Blackboard?.Set(key, false);
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!debugMode || _stateMachine == null)
                return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label($"State Machine: {_stateMachine.StateName}");
            GUILayout.Label($"Current State: {CurrentStateName}");
            GUILayout.Label($"Is Running: {_isRunning}");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}
