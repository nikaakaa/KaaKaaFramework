using System;
using System.Collections.Generic;
using UnityEngine;

#region Core

#region Transitions
public abstract class Transition<T>
{
    public State<T> FromState { get; set; }
    public State<T> ToState { get; set; }
    public int Priority { get; set; }
    public abstract bool CanTransition(T ctx);
}
public class LambdaTransition<T> : Transition<T>
{
    private Func<T, bool> conditionFunc;
    public LambdaTransition(State<T> from, State<T> to, Func<T, bool> condition, int priority = 0)
    {
        FromState = from;
        ToState = to;
        Priority = priority;
        conditionFunc = condition;
    }
    public override bool CanTransition(T ctx)
    {
        return conditionFunc(ctx);
    }
}
#endregion

#region State

public abstract class State<T>
{
    public string Name { get; set; }
    public ComposeState<T> Parent { get; set; }

    protected abstract void OnEnter(T ctx);
    protected abstract void OnExit(T ctx);
    protected abstract void OnUpdate(T ctx);
    public abstract void Enter(T ctx);
    public abstract void Exit(T ctx);
    public abstract void Update(T ctx);

    /// <summary>
    /// 子状态请求切换到兄弟状态
    /// </summary>
    protected void RequestTransition(State<T> target, T ctx)
    {
        Parent?.ChangeToTheSubState(target, ctx);
    }
}

public class ComposeState<T> : State<T>
{
    public State<T> CurrentSubState { get; set; }

    /// <summary>
    /// 递归获取当前激活的叶子状态
    /// </summary>
    public State<T> CurrentLeafState
    {
        get
        {
            if (CurrentSubState is ComposeState<T> compose)
                return compose.CurrentLeafState;
            return CurrentSubState == null ? throw new Exception("CurrentSubState is null") : CurrentSubState;
        }
    }

    public Dictionary<string, State<T>> subStates = new();
    public List<Transition<T>> transitions = new();
    protected override void OnEnter(T ctx) { }
    protected override void OnExit(T ctx) { }
    protected override void OnUpdate(T ctx) { }
    public sealed override void Enter(T ctx)
    {
        OnEnter(ctx);
        CurrentSubState?.Enter(ctx);
    }

    public sealed override void Exit(T ctx)
    {
        CurrentSubState?.Exit(ctx);
        OnExit(ctx);
    }
    public sealed override void Update(T ctx)
    {
        if (TryTransitions(ctx)) return;//这里可调,可以不return
        OnUpdate(ctx);
        CurrentSubState?.Update(ctx);
    }

    public bool TryTransitions(T ctx)
    {
        foreach (var transition in transitions)
        {
            // 只检查从当前状态出发的转换
            if (transition.FromState != CurrentSubState) continue;

            if (transition.CanTransition(ctx))
            {
                ChangeToTheSubState(transition.ToState, ctx);
                return true;
            }
        }
        return false;
    }
    public void ChangeToTheSubState(State<T> toState, T ctx)
    {
        CurrentSubState?.Exit(ctx);
        CurrentSubState = toState;
        CurrentSubState.Enter(ctx);
    }
    public ComposeState<T> RegisterSubState(State<T> state)
    {
        if (!subStates.ContainsKey(state.Name))
        {
            subStates.Add(state.Name, state);
            state.Parent = this;  // 设置父节点引用
            return this;
        }
        throw new Exception($"SubState with name {state.Name} already exists in ComposeState");
    }
    public ComposeState<T> RegisterTransition(Transition<T> transition)
    {
        transitions.Add(transition);
        transitions.Sort((a, b) => b.Priority - a.Priority);
        return this;
    }
}
public class LeafState<T> : State<T>
{
    public sealed override void Enter(T ctx) => OnEnter(ctx);
    public sealed override void Exit(T ctx) => OnExit(ctx);
    public sealed override void Update(T ctx) => OnUpdate(ctx);
    protected override void OnEnter(T ctx) { }
    protected override void OnExit(T ctx) { }
    protected override void OnUpdate(T ctx) { }
}

public class LambdaLeafState<T> : LeafState<T>
{
    private Action<T> onEnter;
    private Action<T> onExit;
    private Action<T> onUpdate;
    public LambdaLeafState(string name, Action<T> onEnter = null, Action<T> onExit = null, Action<T> onUpdate = null)
    {
        Name = name;
        this.onEnter = onEnter;
        this.onExit = onExit;
        this.onUpdate = onUpdate;
    }

    protected override void OnEnter(T ctx) => onEnter?.Invoke(ctx);
    protected override void OnExit(T ctx) => onExit?.Invoke(ctx);
    protected override void OnUpdate(T ctx) => onUpdate?.Invoke(ctx);
}
public class LambdaComposeState<T> : ComposeState<T>
{
    private Action<T> onEnter;
    private Action<T> onExit;
    private Action<T> onUpdate;
    public LambdaComposeState(string name,
        Action<T> onEnter = null, Action<T> onExit = null, Action<T> onUpdate = null)
    {
        Name = name;
        this.onEnter = onEnter;
        this.onExit = onExit;
        this.onUpdate = onUpdate;
    }
    protected override void OnEnter(T ctx) => onEnter?.Invoke(ctx);
    protected override void OnExit(T ctx) => onExit?.Invoke(ctx);
    protected override void OnUpdate(T ctx) => onUpdate?.Invoke(ctx);
}
#endregion

#endregion

#region Builder (Public API: only State<T>/ComposeState<T>, supports SubState)
public sealed class HFSMBuilder<T>
{
    private readonly Stack<ComposeState<T>> _stack = new();
    private readonly Dictionary<State<T>, ComposeState<T>> _ownerOf = new(); // 内部归属追踪
    private readonly ComposeState<T> _root;
    private int _autoId;

    private ComposeState<T> Current => _stack.Peek();

    private HFSMBuilder()
    {
        _root = new LambdaComposeState<T>($"Root_{++_autoId}");
        _stack.Push(_root);
    }

    public static HFSMBuilder<T> Create() => new HFSMBuilder<T>();

    // ------------------------
    // Compose
    // ------------------------
    public HFSMBuilder<T> BeginCompose(
        out ComposeState<T> compose,
        bool isDefault = false,
        Action<T> onEnter = null,
        Action<T> onUpdate = null,
        Action<T> onExit = null)
    {
        compose = new LambdaComposeState<T>($"Compose_{++_autoId}", onEnter, onExit, onUpdate);
        AddChildToCurrent(compose, isDefault);
        _stack.Push(compose);
        return this;
    }

    public HFSMBuilder<T> BeginCompose<TCompose>(
        out TCompose compose,
        bool isDefault = false,
        Action<TCompose> init = null)
        where TCompose : ComposeState<T>, new()
    {
        compose = new TCompose { Name = $"{typeof(TCompose).Name}_{++_autoId}" };
        init?.Invoke(compose);

        AddChildToCurrent(compose, isDefault);
        _stack.Push(compose);
        return this;
    }

    public HFSMBuilder<T> EndCompose()
    {
        if (_stack.Count <= 1)
            throw new InvalidOperationException("已经是根层，不能再 EndCompose()");
        _stack.Pop();
        return this;
    }

    public HFSMBuilder<T> Up() => EndCompose();

    // ------------------------
    // Leaf
    // ------------------------
    public HFSMBuilder<T> Leaf(
        out State<T> leaf,
        Action<T> onEnter = null,
        Action<T> onUpdate = null,
        Action<T> onExit = null,
        bool isDefault = false)
    {
        leaf = new LambdaLeafState<T>($"Leaf_{++_autoId}", onEnter, onExit, onUpdate);
        AddChildToCurrent(leaf, isDefault);
        return this;
    }

    public HFSMBuilder<T> Leaf<TLeaf>(
        out TLeaf leaf,
        bool isDefault = false,
        Action<TLeaf> init = null)
        where TLeaf : LeafState<T>, new()
    {
        leaf = new TLeaf { Name = $"{typeof(TLeaf).Name}_{++_autoId}" };
        init?.Invoke(leaf);

        AddChildToCurrent(leaf, isDefault);
        return this;
    }

    // ------------------------
    // SubState (关键：挂外部子树)
    // ------------------------
    public HFSMBuilder<T> SubState(State<T> subtreeRoot, bool isDefault = false, string overrideName = null)
    {
        if (subtreeRoot == null) throw new ArgumentNullException(nameof(subtreeRoot));

        if (!string.IsNullOrEmpty(overrideName))
            subtreeRoot.Name = overrideName;

        AddExternalChildToCurrent(subtreeRoot, isDefault);
        return this;
    }

    // ------------------------
    // Initial / Transition
    // ------------------------
    public HFSMBuilder<T> Initial(State<T> state)
    {
        EnsureDirectChildOfCurrent(state);
        Current.CurrentSubState = state;
        return this;
    }

    public HFSMBuilder<T> Transition(State<T> from, State<T> to, Func<T, bool> condition, int priority = 0)
    {
        if (condition == null) throw new ArgumentNullException(nameof(condition));
        EnsureDirectChildOfCurrent(from);
        EnsureDirectChildOfCurrent(to);

        Current.RegisterTransition(new LambdaTransition<T>(from, to, condition, priority));
        return this;
    }

    public HFSMBuilder<T> Transition<TTrans>(State<T> from, State<T> to, int priority = 0, Action<TTrans> init = null)
        where TTrans : Transition<T>, new()
    {
        EnsureDirectChildOfCurrent(from);
        EnsureDirectChildOfCurrent(to);

        var t = new TTrans
        {
            FromState = from,
            ToState = to,
            Priority = priority
        };
        init?.Invoke(t);

        Current.RegisterTransition(t);
        return this;
    }

    public HFSMBuilder<T> AnyTransition(State<T> to, Func<T, bool> condition, int priority = 100)
    {
        if (condition == null) throw new ArgumentNullException(nameof(condition));
        EnsureDirectChildOfCurrent(to);

        foreach (var kv in Current.subStates)
        {
            var from = kv.Value;
            if (ReferenceEquals(from, to)) continue;

            Current.RegisterTransition(new LambdaTransition<T>(from, to, condition, priority));
        }
        return this;
    }

    // ------------------------
    // Build
    // ------------------------
    public ComposeState<T> Build()
    {
        _stack.Clear();

        if (_root.CurrentSubState == null)
            Debug.LogWarning("[HFSM] Root has no initial sub state. Add at least one child or call Initial(...) once.");

        return _root;
    }

    // ------------------------
    // Internal
    // ------------------------
    private void AddChildToCurrent(State<T> child, bool isDefault)
    {
        if (child == null) throw new ArgumentNullException(nameof(child));

        if (string.IsNullOrEmpty(child.Name))
            child.Name = $"State_{++_autoId}";

        // 避免同层 Name 冲突
        child.Name = MakeUniqueName(Current, child.Name);

        Current.RegisterSubState(child);
        _ownerOf[child] = Current;

        // 每层自动默认 initial：第一个 child 即默认；isDefault:true 可覆盖
        if (isDefault || Current.CurrentSubState == null)
            Current.CurrentSubState = child;
    }

    private void AddExternalChildToCurrent(State<T> child, bool isDefault)
    {
        // 外部子树也要做同样的 Name 冲突处理
        if (string.IsNullOrEmpty(child.Name))
            child.Name = $"External_{++_autoId}";

        child.Name = MakeUniqueName(Current, child.Name);

        Current.RegisterSubState(child);
        _ownerOf[child] = Current;

        if (isDefault || Current.CurrentSubState == null)
            Current.CurrentSubState = child;
    }

    private void EnsureDirectChildOfCurrent(State<T> s)
    {
        if (s == null) throw new ArgumentNullException(nameof(s));

        // 如果是本 builder 创建或 SubState 挂进来的，都会登记 _ownerOf
        if (!_ownerOf.TryGetValue(s, out var owner) || !ReferenceEquals(owner, Current))
        {
            // 兜底：允许你直接传 Current.subStates 里的对象（例如你绕过了 builder）
            if (Current.subStates.ContainsValue(s))
            {
                _ownerOf[s] = Current;
                return;
            }

            throw new InvalidOperationException(
                $"状态不属于当前 Compose（疑似跨层误用或未通过 SubState 挂载）。\n" +
                $"State={s.Name}, Current={Current.Name}");
        }
    }

    private static string MakeUniqueName(ComposeState<T> owner, string desired)
    {
        if (!owner.subStates.ContainsKey(desired)) return desired;

        int i = 2;
        while (owner.subStates.ContainsKey($"{desired}_{i}")) i++;
        return $"{desired}_{i}";
    }
}
#endregion










