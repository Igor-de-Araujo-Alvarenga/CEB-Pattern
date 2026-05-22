namespace CEB.Channels;

using CEB.Core;
using CEB.Domain;

// =============================================================================
// CHANNELS
// Each channel speaks a different protocol but emits the same domain events.
// The rest of the system never knows which protocol the input came from.
// =============================================================================


// ── REST / JSON Channel ───────────────────────────────────────────────────────
// Receives: HTTP POST /orders { product, amount, email }
// Emits:    OrderReceived
public sealed class RestOrderChannel : IChannel<RestOrderInput, OrderReceived>
{
    public Task<OrderReceived> TranslateAsync(RestOrderInput input, CancellationToken ct = default)
    {
        Console.WriteLine("[Channel:REST] Translating HTTP/JSON → OrderReceived");

        return Task.FromResult(new OrderReceived(
            OrderId:       $"ORD-{Guid.NewGuid():N}"[..8].ToUpper(),
            CustomerEmail: input.Email,
            Product:       input.Product,
            Amount:        input.Amount));
    }
}

public record RestOrderInput(string Email, string Product, decimal Amount);


// ── SOAP Channel ──────────────────────────────────────────────────────────────
// Receives: SOAP envelope <PlaceOrder><product>…</product></PlaceOrder>
// Emits:    OrderReceived (same event — downstream doesn't know it was SOAP)
public sealed class SoapOrderChannel : IChannel<SoapOrderEnvelope, OrderReceived>
{
    public Task<OrderReceived> TranslateAsync(SoapOrderEnvelope input, CancellationToken ct = default)
    {
        Console.WriteLine("[Channel:SOAP] Translating SOAP envelope → OrderReceived");

        // In production: deserialize XML, extract fields
        return Task.FromResult(new OrderReceived(
            OrderId:       $"ORD-{Guid.NewGuid():N}"[..8].ToUpper(),
            CustomerEmail: input.CustomerEmail,
            Product:       input.ProductCode,
            Amount:        decimal.Parse(input.TotalAmount)));
    }
}

public record SoapOrderEnvelope(string CustomerEmail, string ProductCode, string TotalAmount);


// ── gRPC Channel ──────────────────────────────────────────────────────────────
// Receives: gRPC PlaceOrderRequest (generated protobuf)
// Emits:    OrderReceived
public sealed class GrpcOrderChannel : IChannel<GrpcOrderRequest, OrderReceived>
{
    public Task<OrderReceived> TranslateAsync(GrpcOrderRequest input, CancellationToken ct = default)
    {
        Console.WriteLine("[Channel:gRPC] Translating gRPC request → OrderReceived");

        return Task.FromResult(new OrderReceived(
            OrderId:       $"ORD-{Guid.NewGuid():N}"[..8].ToUpper(),
            CustomerEmail: input.Email,
            Product:       input.ProductId,
            Amount:        (decimal)input.AmountCents / 100));
    }
}

public record GrpcOrderRequest(string Email, string ProductId, long AmountCents);


// ── GraphQL Channel ───────────────────────────────────────────────────────────
// Receives: GraphQL mutation placeOrder(input: PlaceOrderInput)
// Emits:    OrderReceived
public sealed class GraphQlOrderChannel : IChannel<GraphQlOrderInput, OrderReceived>
{
    public Task<OrderReceived> TranslateAsync(GraphQlOrderInput input, CancellationToken ct = default)
    {
        Console.WriteLine("[Channel:GraphQL] Translating GraphQL mutation → OrderReceived");

        return Task.FromResult(new OrderReceived(
            OrderId:       $"ORD-{Guid.NewGuid():N}"[..8].ToUpper(),
            CustomerEmail: input.CustomerEmail,
            Product:       input.ProductSlug,
            Amount:        input.Price));
    }
}

public record GraphQlOrderInput(string CustomerEmail, string ProductSlug, decimal Price);


// ── Message Queue Channel ─────────────────────────────────────────────────────
// Receives: message from SQS / RabbitMQ / Azure Service Bus (JSON payload)
// Emits:    OrderReceived
public sealed class QueueOrderChannel : IChannel<QueueMessage, OrderReceived>
{
    public Task<OrderReceived> TranslateAsync(QueueMessage input, CancellationToken ct = default)
    {
        Console.WriteLine($"[Channel:Queue] Translating queue message (source: {input.Source}) → OrderReceived");

        return Task.FromResult(new OrderReceived(
            OrderId:       input.MessageId,
            CustomerEmail: input.Payload["email"],
            Product:       input.Payload["product"],
            Amount:        decimal.Parse(input.Payload["amount"])));
    }
}

public record QueueMessage(string MessageId, string Source, Dictionary<string, string> Payload);


// ── Chat Channel ──────────────────────────────────────────────────────────────
// Receives: WebSocket / SignalR message from customer
// Emits:    ChatMessageReceived
public sealed class ChatChannel : IChannel<ChatInput, ChatMessageReceived>
{
    public Task<ChatMessageReceived> TranslateAsync(ChatInput input, CancellationToken ct = default)
    {
        Console.WriteLine("[Channel:Chat] Translating WebSocket message → ChatMessageReceived");

        return Task.FromResult(new ChatMessageReceived(
            SessionId:     input.SessionId,
            CustomerEmail: input.Email,
            Message:       input.Text));
    }
}

public record ChatInput(string SessionId, string Email, string Text);
