using UnityEngine;

public class HFSMTest : MonoBehaviour
{
    public ComposeState<TestContext> rootState;
    private TestContext context=new();

    void Start()
    {
        ComposeState<TestContext> aState = HFSMBuilder<TestContext>.Create()
            .BeginCompose(out var normalState)
                .Leaf(out var idleState,
                    onEnter: _ => Debug.Log("Enter IdleState"),
                    onUpdate: _ => Debug.Log("Update IdleState"),
                    onExit: _ => Debug.Log("Exit IdleState"))
                .Leaf<WalkState>(out var walkState)
                .Transition(idleState, walkState, _ => Input.GetKeyDown(KeyCode.W))
                .AnyTransition(idleState, _ => Input.GetKeyUp(KeyCode.W))
            .EndCompose()
            .BeginCompose<FuckState>(out var fuckState)
                .Leaf(out var subIdleState,
                    onEnter: _ => Debug.Log("Enter SubIdleState"),
                    onUpdate: _ => Debug.Log("Update SubIdleState"),
                    onExit: _ => Debug.Log("Exit SubIdleState"))
                .Leaf(out var subWalkState,
                    onEnter: _ => Debug.Log("Enter SubWalkState"),
                    onUpdate: _ => Debug.Log("Update SubWalkState"),
                    onExit: _ => Debug.Log("Exit SubWalkState"))
                .Transition(subIdleState, subWalkState, _ => Input.GetKeyDown(KeyCode.F))
                .AnyTransition(subIdleState, _ => Input.GetKeyUp(KeyCode.F))
            .EndCompose()
            // 根层初始进入 normal（你的写法保留）
            .Initial(normalState)
            // 根层切换：Tab 进入/退出 fuck
            .Transition(normalState, fuckState, _ => Input.GetKeyDown(KeyCode.Tab), 100)
            .Transition(fuckState, normalState, _ => Input.GetKeyDown(KeyCode.Tab), 100)
            .Build();

        var bState = HFSMBuilder<TestContext>.Create()
            .Leaf(out var lonelyState,
                onEnter: _ => Debug.Log("Enter LonelyState"),
                onUpdate: _ => Debug.Log("Update LonelyState"),
                onExit: _ => Debug.Log("Exit LonelyState"))
            .Build();
        rootState = HFSMBuilder<TestContext>.Create()
            .BeginCompose(out var rootCompose)
                .SubState(aState)
                .SubState(bState)
                .Transition(aState, bState, _ => Input.GetKeyDown(KeyCode.L), 50)
                .Transition(bState, aState, _ => Input.GetKeyDown(KeyCode.L), 50)
            .EndCompose()
            .Build();
        rootState.Enter(context); // 必须
    }

    private void Update()
    {
        rootState.Update(context);
    }
    private void OnDisable()
    {
        rootState.Exit(context);
    }

}
public class TestContext
{
    public int value;
}
public class FuckState : ComposeState<TestContext>
{
    protected override void OnEnter(TestContext ctx)
    {
        Debug.Log("Enter FuckState");
    }
    protected override void OnUpdate(TestContext ctx)
    {
        Debug.Log("Update FuckState");
    }
    protected override void OnExit(TestContext ctx)
    {
        Debug.Log("Exit FuckState");
    }
}
public class WalkState: LeafState<TestContext>
{
    protected override void OnEnter(TestContext ctx)
    {
        Debug.Log("Enter WalkState");
    }
    protected override void OnUpdate(TestContext ctx)
    {
        Debug.Log("Update WalkState");
    }
    protected override void OnExit(TestContext ctx)
    {
        Debug.Log("Exit WalkState");
    }
}