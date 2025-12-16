using UnityEngine;

namespace KaaKaaFramework.HFSM.Examples
{
    /// <summary>
    /// HFSM使用示例 - 数据驱动配置
    /// 使用ScriptableObject配置状态机
    /// </summary>
    public class HFSMDataDrivenExample : MonoBehaviour
    {
        [Header("状态机配置")]
        [SerializeField]
        private HFSMConfigAsset configAsset;

        [SerializeField]
        private HFSMBehaviourConfigAsset behaviourConfigAsset;

        private HFSMRunner _runner;

        private void Start()
        {
            _runner = GetComponent<HFSMRunner>();
            
            if (_runner == null)
            {
                _runner = gameObject.AddComponent<HFSMRunner>();
            }

            // 方式1：使用普通配置资源
            if (configAsset != null)
            {
                var fsm = configAsset.CreateStateMachine();
                _runner.Initialize(fsm);
                _runner.StartStateMachine();
            }
            // 方式2：使用行为配置资源
            else if (behaviourConfigAsset != null)
            {
                var fsm = behaviourConfigAsset.CreateStateMachine(gameObject);
                _runner.Initialize(fsm);
                _runner.StartStateMachine();
            }
        }

        /// <summary>
        /// 示例：设置移动输入
        /// </summary>
        public void SetMoveInput(Vector3 input)
        {
            _runner?.Blackboard?.Set("MoveInput", input);
        }

        /// <summary>
        /// 示例：触发攻击
        /// </summary>
        public void TriggerAttack()
        {
            _runner?.SetTrigger("AttackTrigger");
        }

        /// <summary>
        /// 示例：造成伤害
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (_runner?.Blackboard == null) return;
            
            var health = _runner.Blackboard.Get<float>("Health", 100f);
            health -= damage;
            _runner.Blackboard.Set("Health", health);
            
            if (health > 0)
            {
                _runner.SetTrigger("HurtTrigger");
            }
        }
    }
}
