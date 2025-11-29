using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#region QF 风格局部事件

public interface IUnRegister
{
    void UnRegister();
}

internal sealed class ActionUnRegister : IUnRegister
{
    private Action onUnRegister;

    public ActionUnRegister(Action onUnRegister)
    {
        this.onUnRegister = onUnRegister;
    }

    public void UnRegister()
    {
        onUnRegister?.Invoke();
        onUnRegister = null;
    }
}

public sealed class UnRegisterOnDestroyTrigger : MonoBehaviour
{
    private readonly List<IUnRegister> unRegisters = new();

    public void AddUnRegister(IUnRegister unRegister)
    {
        if (unRegister == null)
            return;
        unRegisters.Add(unRegister);
    }

    private void OnDestroy()
    {
        for (var i = unRegisters.Count - 1; i >= 0; i--)
        {
            unRegisters[i].UnRegister();
        }

        unRegisters.Clear();
    }
}

public static class UnRegisterExtensions
{
    public static IUnRegister UnRegisterWhenGameObjectDestroyed(this IUnRegister unRegister, GameObject target)
    {
        if (unRegister == null || target == null)
            return unRegister;

        var trigger = target.GetComponent<UnRegisterOnDestroyTrigger>();
        if (trigger == null)
        {
            trigger = target.AddComponent<UnRegisterOnDestroyTrigger>();
        }

        trigger.AddUnRegister(unRegister);
        return unRegister;
    }

    public static IUnRegister UnRegisterWhenDestroyed(this IUnRegister unRegister, Component component)
    {
        return UnRegisterWhenGameObjectDestroyed(unRegister, component.gameObject);
    }
}

public abstract class ScopedGameEventBase : ScriptableObject
{
    [TextArea]
    [SerializeField] private string description;

    public string Description => description;

    public abstract void ClearListeners();
}

[CreateAssetMenu(menuName = "KaaKaaFramework/Scoped Events/ScopedGameEvent", fileName = "ScopedGameEvent")]
public sealed class ScopedGameEvent : ScopedGameEventBase
{
    private readonly List<UnityAction> listeners = new();

    public IUnRegister Register(UnityAction listener)
    {
        if (listener == null || listeners.Contains(listener))
            return null;

        listeners.Add(listener);
        return new ActionUnRegister(() => UnRegister(listener));
    }

    public void UnRegister(UnityAction listener)
    {
        if (listener == null)
            return;
        listeners.Remove(listener);
    }

    public void Raise()
    {
        if (listeners.Count == 0)
            return;

        var snapshot = listeners.ToArray();
        for (var i = 0; i < snapshot.Length; i++)
        {
            try
            {
                snapshot[i]?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    public override void ClearListeners()
    {
        listeners.Clear();
    }
}

public abstract class ScopedGameEvent<TPayload> : ScopedGameEventBase
{
    private readonly List<UnityAction<TPayload>> listeners = new();

    public IUnRegister Register(UnityAction<TPayload> listener)
    {
        if (listener == null || listeners.Contains(listener))
            return null;

        listeners.Add(listener);
        return new ActionUnRegister(() => UnRegister(listener));
    }

    public void UnRegister(UnityAction<TPayload> listener)
    {
        if (listener == null)
            return;

        listeners.Remove(listener);
    }

    public void Raise(TPayload payload)
    {
        if (listeners.Count == 0)
            return;

        var snapshot = listeners.ToArray();
        for (var i = 0; i < snapshot.Length; i++)
        {
            try
            {
                snapshot[i]?.Invoke(payload);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    public override void ClearListeners()
    {
        listeners.Clear();
    }
}

[Serializable]
public sealed class ScopedEventEntry
{
    public string key;
    public ScopedGameEventBase eventAsset;
}

public sealed class ScopedEventCenter : MonoBehaviour
{
    [SerializeField] private List<ScopedEventEntry> events = new();
    private readonly Dictionary<string, ScopedGameEventBase> runtimeEvents = new(StringComparer.Ordinal);

    private void Awake()
    {
        BuildRuntimeMap();
    }

    private void Reset()
    {
        BuildRuntimeMap();
    }

    private void OnDestroy()
    {
        foreach (var scopedEvent in runtimeEvents.Values)
        {
            scopedEvent?.ClearListeners();
        }
        runtimeEvents.Clear();
    }

    public void RegisterEvent(string key, ScopedGameEventBase scopedEvent)
    {
        if (string.IsNullOrEmpty(key) || scopedEvent == null)
            return;

        runtimeEvents[key] = scopedEvent;
        if (!events.Exists(e => e.key == key))
        {
            events.Add(new ScopedEventEntry { key = key, eventAsset = scopedEvent });
        }
    }

    public TEvent GetEvent<TEvent>(string key) where TEvent : ScopedGameEventBase
    {
        if (runtimeEvents.TryGetValue(key, out var evt))
        {
            if (evt is TEvent typed)
            {
                return typed;
            }

            throw new InvalidOperationException($"键 {key} 对应事件类型为 {evt.GetType().Name}，无法转换为 {typeof(TEvent).Name}。");
        }

        throw new KeyNotFoundException($"ScopedEventCenter 中未找到键 {key} 的事件。");
    }

    private void BuildRuntimeMap()
    {
        runtimeEvents.Clear();
        foreach (var entry in events)
        {
            if (entry == null || string.IsNullOrEmpty(entry.key) || entry.eventAsset == null)
                continue;

            runtimeEvents[entry.key] = entry.eventAsset;
        }
    }
}

/// <summary>
/// 用法示例：
/// 1. 在 Project 中创建 ScriptableObject（继承自 ScopedGameEvent 或自定义派生类）。
/// 2. 在宿主 GameObject 上挂载 ScopedEventCenter，将事件资源拖入列表并标记 key。
/// 3. 广播者：center.GetEvent<ScopedGameEvent>("HpChanged").Raise();
///    或自定义的 ScopedGameEvent<int> 子类 -> Raise(newValue)。
/// 4. 监听者：center.GetEvent<ScopedGameEvent>("HpChanged")
///    .Register(OnHpChanged).UnRegisterWhenDestroyed(this);
/// </summary>

#endregion