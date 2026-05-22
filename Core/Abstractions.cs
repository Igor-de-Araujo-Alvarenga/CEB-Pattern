namespace CEB.Core;

// =============================================================================
// EVENT — the language of the system
// Every piece of information that flows between layers is an event.
// Immutable. Typed. Carries a unique ID for idempotency.
// =============================================================================
public interface IEvent
{
    Guid   Id          { get; }
    string Type        { get; }
    DateTimeOffset OccurredAt { get; }
}

public abstract record BaseEvent : IEvent
{
    public Guid   Id          { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public abstract string Type { get; }
}


// =============================================================================
// EVENT BUS — the spine of the system
// Nothing calls anything directly. Everything flows through here.
// =============================================================================
public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IEvent;
    void Subscribe<T>(Func<T, CancellationToken, Task> handler)   where T : IEvent;
}


// =============================================================================
// CHANNEL — protocol translation only
//
// Responsibility:
//   Receive external input in ANY protocol (REST, SOAP, gRPC, GraphQL,
//   queue message, webhook, CLI arg, file upload…) and translate it into
//   exactly ONE typed domain event on the bus.
//
// Rules:
//   ✓ Read the incoming request
//   ✓ Map fields to a domain event
//   ✓ Publish the event onto the bus
//   ✗ No business logic
//   ✗ No validation beyond what is needed to build the event
//   ✗ No knowledge of Effects or Bridges
//
// Adding a new integration protocol = adding a new Channel.
// Nothing else changes.
// =============================================================================
public interface IChannel<TInput, TEvent> where TEvent : IEvent
{
    Task<TEvent> TranslateAsync(TInput input, CancellationToken ct = default);
}


// =============================================================================
// EFFECT — pure business logic
//
// Responsibility:
//   Receive a typed event, run deterministic business rules,
//   and emit zero or more new events onto the bus.
//
// Rules:
//   ✓ Read the event payload
//   ✓ Apply business rules (validate, decide, transform, route)
//   ✓ Publish new events onto the bus
//   ✗ No I/O of any kind (no HTTP, no DB, no file, no clock, no random)
//   ✗ No knowledge of Channels or Bridges
//   ✗ No constructor dependencies beyond IEventBus
//
// Because Effects are pure, they are trivially unit-testable:
//   event in → events out. No mocks needed.
// =============================================================================
public interface IEffect<TEvent> where TEvent : IEvent
{
    Task HandleAsync(TEvent @event, IEventBus bus, CancellationToken ct = default);
}


// =============================================================================
// BRIDGE — the only place with I/O
//
// Responsibility:
//   Speak to external systems. Translate domain events into external calls
//   (REST, SOAP, gRPC, GraphQL, message queue, LLM API…) and emit the
//   result back onto the bus as a new event.
//
// Rules:
//   ✓ Call external systems (HTTP, gRPC, queue, LLM, DB…)
//   ✓ Emit result events back onto the bus
//   ✗ No business logic — if it has an if/else, it belongs in an Effect
//   ✗ No knowledge of Channels or other Bridges
//   ✗ Never calls another Bridge directly
//
// Adding a new external system = adding a new Bridge.
// Nothing else changes.
// =============================================================================
public interface IBridge<TEvent> where TEvent : IEvent
{
    Task ExecuteAsync(TEvent @event, IEventBus bus, CancellationToken ct = default);
}
