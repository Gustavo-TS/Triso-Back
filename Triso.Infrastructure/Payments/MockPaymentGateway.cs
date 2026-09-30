using System.Text.Json;
using Triso.Application.Ports.Payments;
using Triso.Domain.Enums;

namespace Triso.Infrastructure.Payments;

public sealed class MockPaymentGateway(string frontendUrl) : IPaymentGateway
{
    public Task<CreateCheckoutResult> CreateCheckoutAsync(CreateCheckoutCommand command, CancellationToken ct) =>
        Task.FromResult(new CreateCheckoutResult($"{frontendUrl.TrimEnd('/')}/payment/success?order_nsu={Uri.EscapeDataString(command.OrderNsu)}&mock=true"));

    public Task<PaymentVerificationResult> VerifyPaymentAsync(PaymentVerificationCommand command, CancellationToken ct) =>
        Task.FromResult(new PaymentVerificationResult(true, command.TransactionNsu, command.ExpectedAmountCents ?? 0, command.ExpectedAmountCents, 1, PaymentStatus.Paid, PaymentMethod.Pix));

    public Task<PaymentNotification> ParseWebhookAsync(string rawPayload, IReadOnlyDictionary<string, string> headers, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<MockWebhookPayload>(rawPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new JsonException("Payload mock inválido.");
        return Task.FromResult(new PaymentNotification(payload.OrderNsu ?? string.Empty, payload.TransactionNsu ?? string.Empty, payload.InvoiceSlug ?? "mock", null, JsonSerializer.Serialize(new { order_nsu = payload.OrderNsu, transaction_nsu = payload.TransactionNsu })));
    }

    private sealed record MockWebhookPayload(string? OrderNsu, string? TransactionNsu, string? InvoiceSlug);
}
