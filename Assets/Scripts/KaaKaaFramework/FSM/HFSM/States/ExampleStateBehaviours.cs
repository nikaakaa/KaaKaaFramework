using UnityEngine;

namespace KaaKaaFramework.HFSM
{
    /// <summary>
    /// 示例：空闲状态行为
    /// </summary>
    [CreateAssetMenu(fileName = "IdleStateBehaviour", menuName = "KaaKaaFramework/HFSM/States/Idle", order = 0)]
    public class IdleStateBehaviour : HFSMStateBehaviour
    {
        [Header("空闲设置")]
        [SerializeField]
        private float idleTime = 0f;

        private float _timer;

        public override void OnEnter()
        {
            base.OnEnter();
            _timer = 0f;
        }

        public override void OnUpdate()
        {
            _timer += Time.deltaTime;
            
            // 更新空闲时间到黑板
            Blackboard?.Set("IdleTime", _timer);
        }
    }

    /// <summary>
    /// 示例：移动状态行为
    /// </summary>
    [CreateAssetMenu(fileName = "MoveStateBehaviour", menuName = "KaaKaaFramework/HFSM/States/Move", order = 1)]
    public class MoveStateBehaviour : HFSMStateBehaviour
    {
        [Header("移动设置")]
        [SerializeField]
        private float moveSpeed = 5f;

        [SerializeField]
        private string speedParameterName = "Speed";

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnUpdate()
        {
            if (Owner == null)
                return;

            // 从黑板获取移动输入
            var moveInput = Blackboard?.Get<Vector3>("MoveInput", Vector3.zero) ?? Vector3.zero;
            
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Owner.transform.position += moveInput.normalized * moveSpeed * Time.deltaTime;
                
                // 更新速度参数
                Blackboard?.Set(speedParameterName, moveInput.magnitude);
            }
            else
            {
                Blackboard?.Set(speedParameterName, 0f);
            }
        }

        public override void OnExit()
        {
            Blackboard?.Set(speedParameterName, 0f);
            base.OnExit();
        }
    }

    /// <summary>
    /// 示例：攻击状态行为
    /// </summary>
    [CreateAssetMenu(fileName = "AttackStateBehaviour", menuName = "KaaKaaFramework/HFSM/States/Attack", order = 2)]
    public class AttackStateBehaviour : HFSMStateBehaviour
    {
        [Header("攻击设置")]
        [SerializeField]
        private float attackDuration = 0.5f;

        [SerializeField]
        private float damage = 10f;

        [SerializeField]
        private float attackRange = 2f;

        private float _timer;
        private bool _hasDealtDamage;

        public override void OnEnter()
        {
            base.OnEnter();
            _timer = 0f;
            _hasDealtDamage = false;
            
            Blackboard?.Set("IsAttacking", true);
        }

        public override void OnUpdate()
        {
            _timer += Time.deltaTime;

            // 在攻击中间点造成伤害
            if (!_hasDealtDamage && _timer >= attackDuration * 0.5f)
            {
                _hasDealtDamage = true;
                DealDamage();
            }

            // 攻击完成
            if (_timer >= attackDuration)
            {
                Blackboard?.Set("AttackComplete", true);
            }
        }

        private void DealDamage()
        {
            // 这里可以实现伤害逻辑
            // 从黑板获取目标等
            Blackboard?.Set("LastDamageDealt", damage);
            
#if UNITY_EDITOR
            Debug.Log($"[Attack] Dealt {damage} damage!");
#endif
        }

        public override void OnExit()
        {
            Blackboard?.Set("IsAttacking", false);
            Blackboard?.Set("AttackComplete", false);
            base.OnExit();
        }
    }

    /// <summary>
    /// 示例：受伤状态行为
    /// </summary>
    [CreateAssetMenu(fileName = "HurtStateBehaviour", menuName = "KaaKaaFramework/HFSM/States/Hurt", order = 3)]
    public class HurtStateBehaviour : HFSMStateBehaviour
    {
        [Header("受伤设置")]
        [SerializeField]
        private float hurtDuration = 0.3f;

        [SerializeField]
        private bool canBeInterrupted = false;

        private float _timer;

        public override void OnEnter()
        {
            base.OnEnter();
            _timer = 0f;
            
            Blackboard?.Set("IsHurt", true);
        }

        public override void OnUpdate()
        {
            _timer += Time.deltaTime;

            if (_timer >= hurtDuration)
            {
                Blackboard?.Set("HurtComplete", true);
            }
        }

        public override void OnExit()
        {
            Blackboard?.Set("IsHurt", false);
            Blackboard?.Set("HurtComplete", false);
            base.OnExit();
        }
    }

    /// <summary>
    /// 示例：死亡状态行为
    /// </summary>
    [CreateAssetMenu(fileName = "DeadStateBehaviour", menuName = "KaaKaaFramework/HFSM/States/Dead", order = 4)]
    public class DeadStateBehaviour : HFSMStateBehaviour
    {
        [Header("死亡设置")]
        [SerializeField]
        private float respawnDelay = 3f;

        [SerializeField]
        private bool canRespawn = true;

        private float _timer;

        public override void OnEnter()
        {
            base.OnEnter();
            _timer = 0f;
            
            Blackboard?.Set("IsDead", true);
            
            // 禁用控制
            if (Owner != null)
            {
                // 可以禁用一些组件
            }
        }

        public override void OnUpdate()
        {
            if (!canRespawn)
                return;

            _timer += Time.deltaTime;

            if (_timer >= respawnDelay)
            {
                Blackboard?.Set("CanRespawn", true);
            }
        }

        public override void OnExit()
        {
            Blackboard?.Set("IsDead", false);
            Blackboard?.Set("CanRespawn", false);
            base.OnExit();
        }
    }
}
