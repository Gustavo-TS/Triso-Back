namespace Triso.Application.Ports.Payments;
public interface IPaymentGateway
{
    Task<CreateCheckoutResult> CreateCheckoutAsync(CreateCheckoutCommand command, CancellationToken ct);
    Task<PaymentVerificationResult> VerifyPaymentAsync(PaymentVerificationCommand command, CancellationToken ct);
    Task<PaymentNotification> ParseWebhookAsync(string rawPayload, IReadOnlyDictionary<string, string> headers, CancellationToken ct);
}
