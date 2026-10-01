using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DesktopFarmIdle.Core.Events;

public interface IGameEvent
{
    DateTime Timestamp { get; }
}

public interface IEventBus
{
    void Subscribe<T>(Action<T> handler) where T : IGameEvent;
    void Unsubscribe<T>(Action<T> handler) where T : IGameEvent;
    void Publish<T>(T gameEvent) where T : IGameEvent;
}

public class GameEventBus : IEventBus
{
    private static GameEventBus? _instance;
    public static GameEventBus Instance => _instance ??= new GameEventBus();

    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
    private readonly object _lock = new();

    public void Subscribe<T>(Action<T> handler) where T : IGameEvent
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _handlers[type] = list;
            }
            if (!list.Contains(handler))
            {
                list.Add(handler);
            }
        }
    }

    public void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var list))
            {
                list.Remove(handler);
            }
        }
    }

    public void Publish<T>(T gameEvent) where T : IGameEvent
    {
        Delegate[]? handlersCopy = null;
        lock (_lock)
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
            {
                handlersCopy = list.ToArray();
            }
        }

        if (handlersCopy != null)
        {
            foreach (var del in handlersCopy)
            {
                try
                {
                    ((Action<T>)del)(gameEvent);
                }
                catch (Exception ex)
                {
                    Logging.GameLogger.Instance.Error($"Error handling event {typeof(T).Name}: {ex.Message}", ex, "Events");
                }
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _handlers.Clear();
        }
    }
}
