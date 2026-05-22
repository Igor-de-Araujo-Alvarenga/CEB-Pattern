namespace CEB.Domain;

using CEB.Core;

// ── Order events ──────────────────────────────────────────────────────────────
public record OrderReceived(
    string OrderId,
    string CustomerEmail,
    string Product,
    decimal Amount) : BaseEvent
{
    public override string Type => "order.received";
}

public record OrderValidated(string OrderId, string CustomerEmail, string Product, decimal Amount) : BaseEvent
{
    public override string Type => "order.validated";
}

public record OrderFailed(string OrderId, string Reason) : BaseEvent
{
    public override string Type => "order.failed";
}

// ── Payment events ────────────────────────────────────────────────────────────
public record PaymentRequested(string OrderId, string CustomerEmail, decimal Amount) : BaseEvent
{
    public override string Type => "payment.requested";
}

public record PaymentProcessed(string OrderId, string TransactionId) : BaseEvent
{
    public override string Type => "payment.processed";
}

// ── Notification events ───────────────────────────────────────────────────────
public record NotificationRequested(string To, string Subject, string Body) : BaseEvent
{
    public override string Type => "notification.requested";
}

// ── AI — simple prompt events ─────────────────────────────────────────────────
public record PromptRequested(
    string CorrelationId,
    string TemplateName,
    Dictionary<string, string> Variables) : BaseEvent
{
    public override string Type => "ai.prompt_requested";
}

public record PromptCompleted(
    string CorrelationId,
    string TemplateName,
    string Result) : BaseEvent
{
    public override string Type => "ai.prompt_completed";
}

// ── AI — streaming chat events ────────────────────────────────────────────────
public record ChatMessageReceived(
    string SessionId,
    string CustomerEmail,
    string Message) : BaseEvent
{
    public override string Type => "chat.message_received";
}

public record ChatStreamStarted(string SessionId) : BaseEvent
{
    public override string Type => "chat.stream_started";
}

public record ChatStreamChunk(string SessionId, string Chunk, bool IsFinal) : BaseEvent
{
    public override string Type => "chat.stream_chunk";
}
