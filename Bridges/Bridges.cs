namespace CEB.Bridges;

using CEB.Core;
using CEB.Domain;

// =============================================================================
// BRIDGES — the only place with I/O
// Each Bridge speaks to one external system.
// On success: emits a result event back onto the bus.
// No business logic. No retries. No knowledge of other Bridges.
// =============================================================================


// ── Payment Bridge (REST) ─────────────────────────────────────────────────────
// Calls: Stripe / PayPal / any payment REST API
// Receives: PaymentRequested
// Emits:    PaymentProcessed
public sealed class PaymentBridge : IBridge<PaymentRequested>
{
    public async Task ExecuteAsync(PaymentRequested e, IEventBus bus, CancellationToken ct = default)
    {
        Console.WriteLine($"[Bridge:Payment/REST] POST https://api.stripe.com/v1/charges");
        Console.WriteLine($"  → amount={e.Amount}, customer={e.CustomerEmail}");

        // In production: HttpClient.PostAsJsonAsync(...)
        await Task.Delay(50, ct); // simulate network

        var txId = $"TXN-{Guid.NewGuid():N}"[..10].ToUpper();
        Console.WriteLine($"  ← 200 OK txId={txId}");

        await bus.PublishAsync(new PaymentProcessed(e.OrderId, txId), ct);
    }
}


// ── Email Bridge (SMTP / SendGrid) ───────────────────────────────────────────
// Calls: SendGrid API or SMTP
// Receives: NotificationRequested
// Emits:    nothing (fire and forget)
public sealed class EmailBridge : IBridge<NotificationRequested>
{
    public async Task ExecuteAsync(NotificationRequested e, IEventBus bus, CancellationToken ct = default)
    {
        Console.WriteLine($"[Bridge:Email/SMTP] Sending email to {e.To}");
        Console.WriteLine($"  → subject: {e.Subject}");

        // In production: SmtpClient.SendAsync(...) or SendGrid API call
        await Task.Delay(30, ct);

        Console.WriteLine($"  ← Email delivered");
    }
}


// ── ERP Bridge (SOAP) ─────────────────────────────────────────────────────────
// Calls: legacy ERP system via SOAP/XML
// Receives: OrderValidated
// Emits:    nothing (sync call, result not needed downstream)
public sealed class ErpBridge : IBridge<OrderValidated>
{
    public async Task ExecuteAsync(OrderValidated e, IEventBus bus, CancellationToken ct = default)
    {
        Console.WriteLine($"[Bridge:ERP/SOAP] POST https://erp.company.com/OrderService");
        Console.WriteLine($"  → <CreateOrder><Id>{e.OrderId}</Id><Product>{e.Product}</Product></CreateOrder>");

        // In production: HttpClient with SOAPAction header + XML body
        await Task.Delay(80, ct);

        Console.WriteLine($"  ← <CreateOrderResponse><Status>OK</Status></CreateOrderResponse>");
    }
}


// ── Inventory Bridge (gRPC) ───────────────────────────────────────────────────
// Calls: warehouse microservice via gRPC
// Receives: OrderValidated
// Emits:    nothing (reservation confirmed synchronously)
public sealed class InventoryBridge : IBridge<OrderValidated>
{
    public async Task ExecuteAsync(OrderValidated e, IEventBus bus, CancellationToken ct = default)
    {
        Console.WriteLine($"[Bridge:Inventory/gRPC] inventory.ReserveStock(orderId={e.OrderId})");

        // In production: generated gRPC client stub call
        await Task.Delay(40, ct);

        Console.WriteLine($"  ← Stock reserved");
    }
}


// ── Analytics Bridge (GraphQL) ────────────────────────────────────────────────
// Calls: analytics platform via GraphQL mutation
// Receives: PaymentProcessed
// Emits:    nothing
public sealed class AnalyticsBridge : IBridge<PaymentProcessed>
{
    public async Task ExecuteAsync(PaymentProcessed e, IEventBus bus, CancellationToken ct = default)
    {
        Console.WriteLine($"[Bridge:Analytics/GraphQL] mutation trackConversion");
        Console.WriteLine($"  → orderId={e.OrderId}, txId={e.TransactionId}");

        // In production: HttpClient POST with GraphQL query body
        await Task.Delay(20, ct);

        Console.WriteLine($"  ← Event tracked");
    }
}


// =============================================================================
// AI BRIDGES — two integration styles
// =============================================================================


// ── AI Bridge Style 1: Prompt Engineering with Template ──────────────────────
// Like Semantic Kernel PromptFunction or a simple template engine.
// Receives a PromptRequested event with a template name and variables.
// Fills the template, calls the LLM, emits PromptCompleted with the result.
//
// Example template "fraud_check":
//   "Analyze this order for fraud risk.
//    Product: {{product}}, Amount: {{amount}}, Country: {{country}}
//    Respond with: risk_score (0-100), recommendation (approve/review/block)"
public sealed class AiPromptBridge : IBridge<PromptRequested>
{
    // Template registry — in production load from files or Semantic Kernel
    private static readonly Dictionary<string, string> Templates = new()
    {
        ["fraud_check"] =
            "Analyze this order for fraud risk.\n" +
            "Product: {{product}}, Amount: {{amount}}, Country: {{country}}\n" +
            "Respond with risk_score (0-100) and recommendation (approve/review/block).",

        ["order_summary"] =
            "Write a short confirmation message for this order.\n" +
            "Customer: {{customer}}, Product: {{product}}, Amount: {{amount}}.",

        ["support_reply"] =
            "You are a helpful support agent.\n" +
            "Customer message: {{message}}\n" +
            "Write a short, professional reply (max 3 sentences).",
    };

    public async Task ExecuteAsync(PromptRequested e, IEventBus bus, CancellationToken ct = default)
    {
        Console.WriteLine($"[Bridge:AI/Prompt] Template={e.TemplateName}");

        // Step 1: fill the template with variables
        var template = Templates.GetValueOrDefault(e.TemplateName, "Answer: {{message}}");
        var prompt   = e.Variables.Aggregate(template,
            (current, kv) => current.Replace($"{{{{{kv.Key}}}}}", kv.Value));

        Console.WriteLine($"  → Prompt: {prompt[..Math.Min(80, prompt.Length)]}…");

        // Step 2: call LLM (simulated — replace with real Anthropic/OpenAI SDK call)
        // In production:
        //   var response = await anthropicClient.Messages.CreateAsync(new() {
        //       Model = "claude-sonnet-4-20250514",
        //       Messages = [new() { Role = "user", Content = prompt }]
        //   });
        await Task.Delay(200, ct);
        var result = SimulateLlmResponse(e.TemplateName, e.Variables);

        Console.WriteLine($"  ← Result: {result[..Math.Min(80, result.Length)]}…");

        // Step 3: emit result back onto the bus — same as any other Bridge
        await bus.PublishAsync(new PromptCompleted(e.CorrelationId, e.TemplateName, result), ct);
    }

    private static string SimulateLlmResponse(string template, Dictionary<string, string> vars) =>
        template switch
        {
            "fraud_check"    => $"risk_score: 15, recommendation: approve. No significant risk indicators found for {vars.GetValueOrDefault("product", "item")}.",
            "order_summary"  => $"Dear {vars.GetValueOrDefault("customer", "Customer")}, your order for {vars.GetValueOrDefault("product")} has been confirmed.",
            "support_reply"  => "Thank you for reaching out. We have received your message and will respond within 24 hours.",
            _                => "Request processed successfully.",
        };
}


// ── AI Bridge Style 2: Streaming Chat ────────────────────────────────────────
// Receives a ChatMessageReceived event.
// Calls LLM with streaming enabled.
// Emits ChatStreamChunk events as tokens arrive — one per chunk, last has IsFinal=true.
// The consumer (WebSocket handler, SignalR hub) listens for ChatStreamChunk
// and pushes each chunk to the client in real time.
public sealed class AiStreamingChatBridge : IBridge<ChatMessageReceived>
{
    public async Task ExecuteAsync(ChatMessageReceived e, IEventBus bus, CancellationToken ct = default)
    {
        Console.WriteLine($"[Bridge:AI/Stream] Starting stream for session {e.SessionId}");
        Console.WriteLine($"  → Message: {e.Message}");

        await bus.PublishAsync(new ChatStreamStarted(e.SessionId), ct);

        // In production with Anthropic SDK:
        //   await foreach (var chunk in anthropicClient.Messages.StreamAsync(...))
        //   {
        //       await bus.PublishAsync(new ChatStreamChunk(e.SessionId, chunk.Delta, false), ct);
        //   }
        //   await bus.PublishAsync(new ChatStreamChunk(e.SessionId, "", true), ct);

        // Simulated streaming — emits chunks with small delays
        var response = SimulateStreamedResponse(e.Message);
        var words    = response.Split(' ');

        for (var i = 0; i < words.Length; i++)
        {
            var chunk   = words[i] + (i < words.Length - 1 ? " " : "");
            var isFinal = i == words.Length - 1;

            await bus.PublishAsync(new ChatStreamChunk(e.SessionId, chunk, isFinal), ct);

            Console.Write(chunk);
            await Task.Delay(40, ct); // simulate token generation latency
        }

        Console.WriteLine(); // newline after stream
    }

    private static string SimulateStreamedResponse(string message)
    {
        var lower = message.ToLower();
        return lower.Contains("order") || lower.Contains("track")
            ? "I can help you track your order. Please provide your order ID and I will look it up right away."
            : lower.Contains("refund") || lower.Contains("return")
            ? "I understand you would like a refund. Our return policy allows returns within 30 days of purchase."
            : "Thank you for reaching out to our support team. I am here to help you with any questions or concerns you may have.";
    }
}
