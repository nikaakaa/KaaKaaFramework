using System.Text;
using UnityEngine;

namespace KaaKaaFramework.HFSM
{
    /// <summary>
    /// HFSM调试监视器 - 在运行时显示状态机状态
    /// </summary>
    public class HFSMDebugMonitor : MonoBehaviour
    {
        [Header("目标")]
        [SerializeField]
        private HFSMRunner targetRunner;

        [Header("显示设置")]
        [SerializeField]
        private bool showGUI = true;

        [SerializeField]
        private KeyCode toggleKey = KeyCode.F1;

        [SerializeField]
        private Vector2 windowPosition = new Vector2(10, 10);

        [SerializeField]
        private Vector2 windowSize = new Vector2(300, 400);

        [SerializeField]
        private bool showBlackboard = true;

        [SerializeField]
        private bool showTransitions = true;

        [SerializeField]
        private bool showHierarchy = true;

        private Vector2 _scrollPosition;
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;

        private void Start()
        {
            if (targetRunner == null)
            {
                targetRunner = GetComponent<HFSMRunner>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showGUI = !showGUI;
            }
        }

        private void OnGUI()
        {
            if (!showGUI || targetRunner == null || targetRunner.StateMachine == null)
                return;

            InitStyles();

            var rect = new Rect(windowPosition.x, windowPosition.y, windowSize.x, windowSize.y);
            GUI.Box(rect, "");

            GUILayout.BeginArea(rect);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawCurrentState();

            if (showHierarchy)
            {
                DrawHierarchy();
            }

            if (showBlackboard)
            {
                DrawBlackboard();
            }

            if (showTransitions)
            {
                DrawTransitions();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void InitStyles()
        {
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(5, 5, 5, 5)
                };
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12
                };
            }

            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                };
            }
        }

        private void DrawHeader()
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label($"HFSM Monitor", _headerStyle);
            GUILayout.Label($"State Machine: {targetRunner.StateMachine.StateName}", _labelStyle);
            GUILayout.Label($"Running: {targetRunner.IsRunning}", _labelStyle);
            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawCurrentState()
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Current State", _headerStyle);

            var state = targetRunner.StateMachine.CurrentState;
            if (state != null)
            {
                DrawStateInfo(state, 0);
            }
            else
            {
                GUILayout.Label("None", _labelStyle);
            }

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawStateInfo(IHFSMState state, int depth)
        {
            var indent = new string(' ', depth * 2);
            GUILayout.Label($"{indent}► {state.StateName}", _labelStyle);

            // 如果是子状态机，递归显示
            if (state is HierarchicalStateMachine subFsm && subFsm.CurrentState != null)
            {
                DrawStateInfo(subFsm.CurrentState, depth + 1);
            }
        }

        private void DrawHierarchy()
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("State Hierarchy", _headerStyle);

            DrawStateMachineHierarchy(targetRunner.StateMachine, 0);

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawStateMachineHierarchy(HierarchicalStateMachine fsm, int depth)
        {
            var indent = new string(' ', depth * 2);

            foreach (var state in fsm.States)
            {
                var isCurrent = state == fsm.CurrentState;
                var prefix = isCurrent ? "● " : "○ ";
                
                GUILayout.Label($"{indent}{prefix}{state.StateName}", _labelStyle);

                if (state is HierarchicalStateMachine subFsm)
                {
                    DrawStateMachineHierarchy(subFsm, depth + 1);
                }
            }
        }

        private void DrawBlackboard()
        {
            var blackboard = targetRunner.Blackboard;
            if (blackboard == null)
                return;

            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Blackboard", _headerStyle);

            foreach (var key in blackboard.GetAllKeys())
            {
                if (blackboard.TryGet<object>(key, out var value))
                {
                    GUILayout.Label($"{key}: {value}", _labelStyle);
                }
            }

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawTransitions()
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Available Transitions", _headerStyle);

            var fsm = targetRunner.StateMachine;
            var currentStateName = fsm.CurrentState?.StateName ?? "";

            // 显示当前状态可用的转换
            foreach (var transition in fsm.Transitions)
            {
                if (transition.FromStateName == currentStateName)
                {
                    var canTransition = transition.CanTransition();
                    var color = canTransition ? "green" : "red";
                    GUILayout.Label($"→ {transition.ToStateName} [{(canTransition ? "✓" : "✗")}]", _labelStyle);
                }
            }

            // 显示Any转换
            foreach (var anyTransition in fsm.AnyTransitions)
            {
                var canTransition = anyTransition.CanTransition();
                GUILayout.Label($"Any → {anyTransition.ToStateName} [{(canTransition ? "✓" : "✗")}]", _labelStyle);
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        /// 获取状态机状态的文本表示
        /// </summary>
        public string GetStateReport()
        {
            if (targetRunner?.StateMachine == null)
                return "No state machine";

            var sb = new StringBuilder();
            sb.AppendLine($"State Machine: {targetRunner.StateMachine.StateName}");
            sb.AppendLine($"Current State: {targetRunner.CurrentStateName}");
            sb.AppendLine($"Running: {targetRunner.IsRunning}");
            
            return sb.ToString();
        }
    }
}
