using UnityEngine;

namespace KaaKaaFramework.HFSM.Examples
{
    /// <summary>
    /// HFSM使用示例 - 分层状态机
    /// 演示嵌套的子状态机
    /// </summary>
    public class HFSMHierarchyExample : MonoBehaviour
    {
        private HFSMRunner _runner;

        private void Start()
        {
            // 创建带有子状态机的分层状态机
            var fsm = new HFSMBuilder("EnemyAI")
                // 巡逻状态（子状态机）
                .SubStateMachine("Patrol", patrol =>
                {
                    patrol
                        .State<PatrolWalkState>("Walk")
                        .State<PatrolWaitState>("Wait")
                        .DefaultState("Walk")
                        
                        .Transition("Walk", "Wait")
                            .When("ReachedWaypoint", true)
                            .End()
                            
                        .Transition("Wait", "Walk")
                            .When("WaitComplete", true)
                            .End();
                })
                
                // 追击状态（子状态机）
                .SubStateMachine("Chase", chase =>
                {
                    chase
                        .State<ChaseRunState>("Run")
                        .State<ChaseSearchState>("Search")
                        .DefaultState("Run")
                        
                        .Transition("Run", "Search")
                            .When("LostTarget", true)
                            .End()
                            
                        .Transition("Search", "Run")
                            .When("FoundTarget", true)
                            .End();
                })
                
                // 战斗状态（子状态机）
                .SubStateMachine("Combat", combat =>
                {
                    combat
                        .State<CombatAttackState>("Attack")
                        .State<CombatDefendState>("Defend")
                        .State<CombatEvadeState>("Evade")
                        .DefaultState("Attack")
                        
                        .Transition("Attack", "Defend")
                            .When("ShouldDefend", true)
                            .End()
                            
                        .Transition("Defend", "Evade")
                            .When("ShouldEvade", true)
                            .End()
                            
                        .Transition("Evade", "Attack")
                            .When("EvadeComplete", true)
                            .End()
                            
                        .Transition("Defend", "Attack")
                            .When("DefendComplete", true)
                            .End();
                })
                
                // 死亡状态
                .State<EnemyDeadState>("Dead")
                
                .DefaultState("Patrol")
                
                // 顶层转换
                .Transition("Patrol", "Chase")
                    .When("PlayerDetected", true)
                    .End()
                    
                .Transition("Chase", "Patrol")
                    .When("LostPlayer", true)
                    .End()
                    
                .Transition("Chase", "Combat")
                    .When("InAttackRange", true)
                    .End()
                    
                .Transition("Combat", "Chase")
                    .When("OutOfAttackRange", true)
                    .End()
                    
                // 任何状态都可以死亡
                .AnyTransition("Dead")
                    .WhenInt("Health", CompareType.LessOrEqual, 0)
                    .End()
                
                .Build();

            _runner = gameObject.AddComponent<HFSMRunner>();
            _runner.Initialize(fsm);
            _runner.StartStateMachine();
        }

        private void Update()
        {
            // 模拟AI行为触发
            if (Input.GetKeyDown(KeyCode.P))
            {
                _runner.SetBool("PlayerDetected", !_runner.GetBool("PlayerDetected"));
                Debug.Log($"PlayerDetected: {_runner.GetBool("PlayerDetected")}");
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                _runner.SetBool("InAttackRange", !_runner.GetBool("InAttackRange"));
                Debug.Log($"InAttackRange: {_runner.GetBool("InAttackRange")}");
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                var health = _runner.GetInt("Health", 100);
                _runner.SetInt("Health", health - 50);
                Debug.Log($"Health: {_runner.GetInt("Health", 100)}");
            }
        }
    }

    #region Patrol States

    public class PatrolWalkState : HFSMState
    {
        public PatrolWalkState() : base("Walk") { }

        public override void OnEnter()
        {
            base.OnEnter();
            Blackboard?.Set("ReachedWaypoint", false);
        }

        public override void OnUpdate()
        {
            // 模拟到达航点
            if (Random.value < 0.01f)
            {
                Blackboard?.Set("ReachedWaypoint", true);
            }
        }
    }

    public class PatrolWaitState : TimedState
    {
        public PatrolWaitState() : base("Wait", 2f) { }

        protected override void OnTimeComplete()
        {
            Blackboard?.Set("ReachedWaypoint", false);
            Blackboard?.Set("WaitComplete", true);
        }

        public override void OnExit()
        {
            Blackboard?.Set("WaitComplete", false);
            base.OnExit();
        }
    }

    #endregion

    #region Chase States

    public class ChaseRunState : HFSMState
    {
        public ChaseRunState() : base("Run") { }

        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("追击：奔跑");
        }
    }

    public class ChaseSearchState : HFSMState
    {
        public ChaseSearchState() : base("Search") { }

        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("追击：搜索");
        }
    }

    #endregion

    #region Combat States

    public class CombatAttackState : HFSMState
    {
        public CombatAttackState() : base("Attack") { }

        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("战斗：攻击");
        }
    }

    public class CombatDefendState : HFSMState
    {
        public CombatDefendState() : base("Defend") { }

        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("战斗：防御");
        }
    }

    public class CombatEvadeState : HFSMState
    {
        public CombatEvadeState() : base("Evade") { }

        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("战斗：闪避");
        }
    }

    #endregion

    #region Other States

    public class EnemyDeadState : HFSMState
    {
        public EnemyDeadState() : base("Dead") { }

        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("敌人死亡");
        }
    }

    #endregion
}
