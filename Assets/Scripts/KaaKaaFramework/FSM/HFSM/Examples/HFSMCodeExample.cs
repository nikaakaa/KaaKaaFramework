using UnityEngine;

namespace KaaKaaFramework.HFSM.Examples
{
    /// <summary>
    /// HFSM使用示例 - 代码构建
    /// </summary>
    public class HFSMCodeExample : MonoBehaviour
    {
        private HFSMRunner _runner;

        private void Start()
        {
            // 使用构建器创建状态机
            var fsm = new HFSMBuilder("PlayerFSM")
                // 添加状态
                .State<IdleState>("Idle")
                .State<MoveState>("Move")
                .State<AttackState>("Attack")
                .State<HurtState>("Hurt")
                .State<DeadState>("Dead")
                
                // 设置默认状态
                .DefaultState("Idle")
                
                // 初始化参数
                .WithParameter("Speed", 0f)
                .WithParameter("IsAttacking", false)
                .WithParameter("Health", 100)
                
                // 配置转换
                .Transition("Idle", "Move")
                    .WhenFloat("Speed", CompareType.Greater, 0.1f)
                    .End()
                    
                .Transition("Move", "Idle")
                    .WhenFloat("Speed", CompareType.LessOrEqual, 0.1f)
                    .End()
                    
                .Transition("Idle", "Attack")
                    .WhenTrigger("AttackTrigger")
                    .End()
                    
                .Transition("Move", "Attack")
                    .WhenTrigger("AttackTrigger")
                    .End()
                    
                .Transition("Attack", "Idle")
                    .When("AttackComplete", true)
                    .End()
                
                // Any转换 - 从任何状态都可以触发
                .AnyTransition("Hurt")
                    .WhenTrigger("HurtTrigger")
                    .End()
                    
                .AnyTransition("Dead")
                    .WhenInt("Health", CompareType.LessOrEqual, 0)
                    .End()
                
                .Transition("Hurt", "Idle")
                    .When("HurtComplete", true)
                    .End()
                    
                .Build();

            // 使用HFSMRunner运行
            _runner = gameObject.AddComponent<HFSMRunner>();
            _runner.Initialize(fsm);
            _runner.StartStateMachine();
        }

        private void Update()
        {
            // 模拟输入
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            var speed = new Vector3(horizontal, 0, vertical).magnitude;
            _runner.SetFloat("Speed", speed);
            
            // 攻击
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _runner.SetTrigger("AttackTrigger");
            }
            
            // 模拟受伤
            if (Input.GetKeyDown(KeyCode.H))
            {
                _runner.SetTrigger("HurtTrigger");
                var health = _runner.GetInt("Health");
                _runner.SetInt("Health", health - 20);
            }
        }
    }

    #region Example States

    public class IdleState : HFSMState
    {
        public IdleState() : base("Idle") { }

        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("进入空闲状态");
        }

        public override void OnUpdate()
        {
            // 空闲逻辑
        }
    }

    public class MoveState : HFSMState
    {
        public MoveState() : base("Move") { }

        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("进入移动状态");
        }

        public override void OnUpdate()
        {
            // 移动逻辑
        }
    }

    public class AttackState : HFSMState
    {
        private float _timer;
        private const float AttackDuration = 0.5f;

        public AttackState() : base("Attack") { }

        public override void OnEnter()
        {
            base.OnEnter();
            _timer = 0f;
            Blackboard?.Set("AttackComplete", false);
            Debug.Log("进入攻击状态");
        }

        public override void OnUpdate()
        {
            _timer += Time.deltaTime;
            
            if (_timer >= AttackDuration)
            {
                Blackboard?.Set("AttackComplete", true);
            }
        }
    }

    public class HurtState : HFSMState
    {
        private float _timer;
        private const float HurtDuration = 0.3f;

        public HurtState() : base("Hurt") { }

        public override void OnEnter()
        {
            base.OnEnter();
            _timer = 0f;
            Blackboard?.Set("HurtComplete", false);
            Debug.Log("进入受伤状态");
        }

        public override void OnUpdate()
        {
            _timer += Time.deltaTime;
            
            if (_timer >= HurtDuration)
            {
                Blackboard?.Set("HurtComplete", true);
            }
        }
    }

    public class DeadState : HFSMState
    {
        public DeadState() : base("Dead") { }

        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("进入死亡状态");
        }
    }

    #endregion
}
