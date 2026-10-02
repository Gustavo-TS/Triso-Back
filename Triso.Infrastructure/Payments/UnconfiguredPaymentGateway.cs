using Triso.Application.Ports.Payments;

namespace Triso.Infrastructure.Payments;

public sealed class UnconfiguredPaymentGateway : IPaymentGateway
{
    private static InvalidOperationException Error() => new("O gateway de pagamento não está configurado. Defina INFINITEPAY_HANDLE, INFINITEPAY_BASE_URL, FRONTEND_URL e API_PUBLIC_URL.");
    public Task<CreateCheckoutResult> CreateCheckoutAsync(CreateCheckoutCommand command, CancellationToken ct) => Task.FromException<CreateCheckoutResult>(Error());
    public Task<PaymentVerificationResult> VerifyPaymentAsync(PaymentVerificationCommand command, CancellationToken ct) => Task.FromException<PaymentVerificationResult>(Error());
    public Task<PaymentNotification> ParseWebhookAsync(string rawPayload, IReadOnlyDictionary<string, string> headers, CancellationToken ct) => Task.FromException<PaymentNotification>(Error());
}
