using System.Collections.Concurrent;

namespace CEB.Core;

public sealed class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Func<IEvent, CancellationToken, Task>>> _handlers = new();

    public void Subscribe<T>(Func<T, CancellationToken, Task> handler) where T : IEvent
    {
        var list = _handlers.GetOrAdd(typeof(T), _ => []);
        list.Add((e, ct) => handler((T)e, ct));
    }

    public async Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IEvent
    {
        Console.WriteLine($"  [Bus] {{{@event.Type}}}");

        if (!_handlers.TryGetValue(typeof(T), out var handlers)) return;

        foreach (var handler in handlers)
            await handler(@event, ct);
    }
}
