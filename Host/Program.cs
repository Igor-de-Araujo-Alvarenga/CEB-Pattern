using CEB.Bridges;
using CEB.Channels;
using CEB.Core;
using CEB.Domain;
using CEB.Effects;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("════════════════════════════════════════════");
Console.WriteLine("  CEB Pattern — Channel · Effect · Bridge  ");
Console.WriteLine("════════════════════════════════════════════\n");

// ── Composition root ──────────────────────────────────────────────────────────
// All wiring happens here. No layer knows about any other layer directly.

var bus = new EventBus();

// Effects (pure logic) — subscribe to domain events
bus.Subscribe<OrderReceived>   ((e, ct) => new ValidateOrderEffect().HandleAsync(e, bus, ct));
bus.Subscribe<OrderValidated>  ((e, ct) => new RouteToPaymentEffect().HandleAsync(e, bus, ct));
bus.Subscribe<PaymentProcessed>((e, ct) => new ConfirmAfterPaymentEffect().HandleAsync(e, bus, ct));
bus.Subscribe<PromptCompleted> ((e, ct) => new RoutePromptResultEffect().HandleAsync(e, bus, ct));

// Bridges (I/O) — subscribe to domain events
bus.Subscribe<PaymentRequested>    ((e, ct) => new PaymentBridge().ExecuteAsync(e, bus, ct));
bus.Subscribe<NotificationRequested>((e, ct) => new EmailBridge().ExecuteAsync(e, bus, ct));
bus.Subscribe<OrderValidated>      ((e, ct) => new ErpBridge().ExecuteAsync(e, bus, ct));
bus.Subscribe<OrderValidated>      ((e, ct) => new InventoryBridge().ExecuteAsync(e, bus, ct));
bus.Subscribe<PaymentProcessed>    ((e, ct) => new AnalyticsBridge().ExecuteAsync(e, bus, ct));
bus.Subscribe<PromptRequested>     ((e, ct) => new AiPromptBridge().ExecuteAsync(e, bus, ct));
bus.Subscribe<ChatMessageReceived> ((e, ct) => new AiStreamingChatBridge().ExecuteAsync(e, bus, ct));

// ── Scenario 1: REST order ────────────────────────────────────────────────────
Console.WriteLine("── Scenario 1: Order via REST/JSON ──────────────────────\n");

var restChannel = new RestOrderChannel();
var restEvent   = await restChannel.TranslateAsync(
    new RestOrderInput("alice@email.com", "Laptop Pro", 1299m));
await bus.PublishAsync(restEvent);

// ── Scenario 2: Same order, different protocol — SOAP ────────────────────────
Console.WriteLine("\n── Scenario 2: Same order via SOAP ──────────────────────\n");

var soapChannel = new SoapOrderChannel();
var soapEvent   = await soapChannel.TranslateAsync(
    new SoapOrderEnvelope("bob@email.com", "LAPTOP-PRO", "899.00"));
await bus.PublishAsync(soapEvent);

// ── Scenario 3: Order via message queue ──────────────────────────────────────
Console.WriteLine("\n── Scenario 3: Order from Queue (SQS/RabbitMQ) ─────────\n");

var queueChannel = new QueueOrderChannel();
var queueEvent   = await queueChannel.TranslateAsync(new QueueMessage(
    MessageId: $"MSG-{Guid.NewGuid():N}"[..8].ToUpper(),
    Source:    "rabbitmq",
    Payload:   new() {
        ["email"]   = "carol@email.com",
        ["product"] = "Wireless Mouse",
        ["amount"]  = "49.90"
    }));
await bus.PublishAsync(queueEvent);

// ── Scenario 4: AI — prompt with template ────────────────────────────────────
Console.WriteLine("\n── Scenario 4: AI — Prompt with Template ────────────────\n");

await bus.PublishAsync(new PromptRequested(
    CorrelationId: Guid.NewGuid().ToString(),
    TemplateName:  "fraud_check",
    Variables:     new() {
        ["product"] = "Gaming GPU",
        ["amount"]  = "2499.00",
        ["country"] = "US"
    }));

// ── Scenario 5: AI — Streaming Chat ──────────────────────────────────────────
Console.WriteLine("\n── Scenario 5: AI — Streaming Chat ─────────────────────\n");

var chatChannel = new ChatChannel();
var chatEvent   = await chatChannel.TranslateAsync(
    new ChatInput("SESSION-001", "dave@email.com", "I want to track my order"));
await bus.PublishAsync(chatEvent);

Console.WriteLine("\n════════════════════════════════════════════");
Console.WriteLine("  Done. All five protocols, same bus.");
Console.WriteLine("════════════════════════════════════════════");
