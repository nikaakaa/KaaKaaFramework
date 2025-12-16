using UnityEngine;

public class TestSimpleHFSM : MonoBehaviour
{
    public State rootState = new("RootState");
    
    private void Start()
    {
        // 第一层：Root State
        // 创建第二层状态 StateA 和 StateB
        var stateA = new State("StateA",
            onEnter: () => Debug.Log("Enter StateA"),
            onUpdate: () => Debug.Log("Update StateA"),
            onExit: () => Debug.Log("Exit StateA")
        );
        
        var stateB = new State("StateB",
            onEnter: () => Debug.Log("Enter StateB"),
            onUpdate: () => Debug.Log("Update StateB"),
            onExit: () => Debug.Log("Exit StateB")
        );
        
        // 第二层：StateA 的子状态
        // 为 StateA 添加子状态 StateA1 和 StateA2
        stateA
            .RegisterState("StateA1", new State("StateA1",
                onEnter: () => Debug.Log("  Enter StateA1"),
                onUpdate: () => Debug.Log("  Update StateA1"),
                onExit: () => Debug.Log("  Exit StateA1")
            ))
            .RegisterState("StateA2", new State("StateA2",
                onEnter: () => Debug.Log("  Enter StateA2"),
                onUpdate: () => Debug.Log("  Update StateA2"),
                onExit: () => Debug.Log("  Exit StateA2")
            ))
            .RegisterTransition(new Transition(
                parentState: stateA,
                fromStateName: "StateA1",
                toStateName: "StateA2",
                condition: () => Input.GetKeyDown(KeyCode.Alpha1)
            ));
        
        // 第三层：StateA1 的子状态
        // 为 StateA1 添加子状态 StateA1a 和 StateA1b
        var stateA1 = stateA.StatesDic["StateA1"];
        stateA1
            .RegisterState("StateA1a", new State("StateA1a",
                onEnter: () => Debug.Log("    Enter StateA1a"),
                onUpdate: () => Debug.Log("    Update StateA1a"),
                onExit: () => Debug.Log("    Exit StateA1a")
            ))
            .RegisterState("StateA1b", new State("StateA1b",
                onEnter: () => Debug.Log("    Enter StateA1b"),
                onUpdate: () => Debug.Log("    Update StateA1b"),
                onExit: () => Debug.Log("    Exit StateA1b")
            ))
            .RegisterTransition(new Transition(
                parentState: stateA1,
                fromStateName: "StateA1a",
                toStateName: "StateA1b",
                condition: () => Input.GetKeyDown(KeyCode.Q)
            ))
            .RegisterTransition(new Transition(
                parentState: stateA1,
                fromStateName: "StateA1b",
                toStateName: "StateA1a",
                condition: () => Input.GetKeyDown(KeyCode.W)
            ));
        
        // 第三层：StateA2 的子状态
        var stateA2 = stateA.StatesDic["StateA2"];
        stateA2
            .RegisterState("StateA2a", new State("StateA2a",
                onEnter: () => Debug.Log("    Enter StateA2a"),
                onUpdate: () => Debug.Log("    Update StateA2a"),
                onExit: () => Debug.Log("    Exit StateA2a")
            ))
            .RegisterState("StateA2b", new State("StateA2b",
                onEnter: () => Debug.Log("    Enter StateA2b"),
                onUpdate: () => Debug.Log("    Update StateA2b"),
                onExit: () => Debug.Log("    Exit StateA2b")
            ))
            .RegisterTransition(new Transition(
                parentState: stateA2,
                fromStateName: "StateA2a",
                toStateName: "StateA2b",
                condition: () => Input.GetKeyDown(KeyCode.E)
            ));
        
        // 第二层：StateB 的子状态
        stateB
            .RegisterState("StateB1", new State("StateB1",
                onEnter: () => Debug.Log("  Enter StateB1"),
                onUpdate: () => Debug.Log("  Update StateB1"),
                onExit: () => Debug.Log("  Exit StateB1")
            ))
            .RegisterState("StateB2", new State("StateB2",
                onEnter: () => Debug.Log("  Enter StateB2"),
                onUpdate: () => Debug.Log("  Update StateB2"),
                onExit: () => Debug.Log("  Exit StateB2")
            ))
            .RegisterTransition(new Transition(
                parentState: stateB,
                fromStateName: "StateB1",
                toStateName: "StateB2",
                condition: () => Input.GetKeyDown(KeyCode.Alpha2)
            ));
        
        // 注册第一层状态和转换
        rootState
            .RegisterState("StateA", stateA)
            .RegisterState("StateB", stateB)
            .RegisterTransition(new Transition(
                parentState: rootState,
                fromStateName: "StateA",
                toStateName: "StateB",
                condition: () => Input.GetKeyDown(KeyCode.Space)
            ))
            .RegisterTransition(new Transition(
                parentState: rootState,
                fromStateName: "StateB",
                toStateName: "StateA",
                condition: () => Input.GetKeyDown(KeyCode.Backspace)
            ));
        
        // 初始化状态层次结构
        rootState.ChangeToTheSubState("StateA"); // Root 激活 A，触发 A.OnEnter
        stateA.ChangeToTheSubState("StateA1");   // A 激活 A1，触发 A1.OnEnter
        stateA1.ChangeToTheSubState("StateA1a"); // A1 激活 A1a，触发 A1a.OnEnter
        
        // 对于此时未激活的 StateB，直接设置默认子状态，避免触发 OnEnter
        if (stateB.StatesDic.TryGetValue("StateB1", out var b1))
        {
            stateB.CurrentSubState = b1;
        }
        
        Debug.Log("=== HFSM 测试用例已初始化 ===");
        Debug.Log("按键说明：");
        Debug.Log("Space: StateA -> StateB");
        Debug.Log("Backspace: StateB -> StateA");
        Debug.Log("1: StateA1 -> StateA2");
        Debug.Log("2: StateB1 -> StateB2");
        Debug.Log("Q: StateA1a -> StateA1b");
        Debug.Log("W: StateA1b -> StateA1a");
        Debug.Log("E: StateA2a -> StateA2b");
    }

    private void Update()
    {
        rootState.Tick();
    }
}
