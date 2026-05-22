namespace CEB.Effects;

using CEB.Core;
using CEB.Domain;

// =============================================================================
// EFFECTS — pure business logic
// These run without any mock, without any external dependency.
// Input: typed event. Output: new events on the bus.
// =============================================================================


// ── Validate Order ────────────────────────────────────────────────────────────
// Receives: OrderReceived
// Emits:    OrderFailed | OrderValidated
public sealed class ValidateOrderEffect : IEffect<OrderReceived>
{
    public async Task HandleAsync(OrderReceived e, IEventBus bus, CancellationToken ct = default)
    {
        Console.WriteLine($"[Effect:Validate] Validating order {e.OrderId}");

        if (string.IsNullOrWhiteSpace(e.Product))
        {
            await bus.PublishAsync(new OrderFailed(e.OrderId, "Product is required"), ct);
            return;
        }

        if (e.Amount <= 0)
        {
            await bus.PublishAsync(new OrderFailed(e.OrderId, "Amount must be positive"), ct);
            return;
        }

        if (!e.CustomerEmail.Contains('@'))
        {
            await bus.PublishAsync(new OrderFailed(e.OrderId, "Invalid email"), ct);
            return;
        }

        await bus.PublishAsync(
            new OrderValidated(e.OrderId, e.CustomerEmail, e.Product, e.Amount), ct);
    }
}


// ── Route To Payment ──────────────────────────────────────────────────────────
// Receives: OrderValidated
// Emits:    PaymentRequested
public sealed class RouteToPaymentEffect : IEffect<OrderValidated>
{
    public async Task HandleAsync(OrderValidated e, IEventBus bus, CancellationToken ct = default)
    {
        Console.WriteLine($"[Effect:Route] Routing order {e.OrderId} to payment");

        await bus.PublishAsync(
            new PaymentRequested(e.OrderId, e.CustomerEmail, e.Amount), ct);
    }
}


// ── Confirm After Payment ─────────────────────────────────────────────────────
// Receives: PaymentProcessed
// Emits:    NotificationRequested (confirmation email)
public sealed class ConfirmAfterPaymentEffect : IEffect<PaymentProcessed>
{
    public async Task HandleAsync(PaymentProcessed e, IEventBus bus, CancellationToken ct = default)
    {
        Console.WriteLine($"[Effect:Confirm] Payment {e.TransactionId} confirmed for order {e.OrderId}");

        await bus.PublishAsync(new NotificationRequested(
            To:      "customer@example.com",
            Subject: $"Order {e.OrderId} confirmed",
            Body:    $"Your order was confirmed. Transaction: {e.TransactionId}"), ct);
    }
}


// ── Route AI Prompt Result ────────────────────────────────────────────────────
// Receives: PromptCompleted (result from AI Bridge)
// Emits:    NotificationRequested (sends result via email)
// Pure routing — the AI already ran, this Effect decides what to do with the result
public sealed class RoutePromptResultEffect : IEffect<PromptCompleted>
{
    public async Task HandleAsync(PromptCompleted e, IEventBus bus, CancellationToken ct = default)
    {
        Console.WriteLine($"[Effect:PromptResult] Routing AI result for correlation {e.CorrelationId}");

        // Business rule: if template is "fraud_check", send to review queue
        // (in a real system — here we just notify)
        await bus.PublishAsync(new NotificationRequested(
            To:      "ops@company.com",
            Subject: $"AI result ready — {e.TemplateName}",
            Body:    e.Result), ct);
    }
}
