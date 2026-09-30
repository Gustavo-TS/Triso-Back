using System.Text.Json;
using Triso.Application.Ports.Payments;
using Triso.Domain.Enums;
namespace Triso.Infrastructure.Payments.InfinitePay;
internal sealed class InfinitePayGateway(InfinitePayClient client, InfinitePayOptions options) : IPaymentGateway
{
    public async Task<CreateCheckoutResult> CreateCheckoutAsync(CreateCheckoutCommand command, CancellationToken ct)
    {
        var request = new CreateLinkRequest(options.Handle,
            $"{options.FrontendUrl.TrimEnd('/')}/payment/success?order_nsu={Uri.EscapeDataString(command.OrderNsu)}",
            $"{options.ApiPublicUrl.TrimEnd('/')}/api/v1/webhooks/infinitepay", command.OrderNsu,
            command.Items.Select(x => new InfinitePayItem(x.Quantity, x.UnitPriceCents, x.Name)).ToList(),
            new InfinitePayCustomer(command.Customer.Name, command.Customer.Email),
            new InfinitePayAddress(command.Address.PostalCode, command.Address.Number, command.Address.Complement));
        var response = await client.CreateLinkAsync(request, ct);
        if (!Uri.TryCreate(response.Url, UriKind.Absolute, out _)) throw new InvalidOperationException("A InfinitePay n\u00e3o retornou uma URL de checkout v\u00e1lida.");
        return new CreateCheckoutResult(response.Url!);
    }
    public async Task<PaymentVerificationResult> VerifyPaymentAsync(PaymentVerificationCommand command, CancellationToken ct)
    {
        var response = await client.CheckPaymentAsync(new PaymentCheckRequest(options.Handle, command.OrderNsu, command.TransactionNsu), ct);
        var method = response.Capture_Method?.Equals("pix", StringComparison.OrdinalIgnoreCase) == true ? PaymentMethod.Pix :
            response.Capture_Method?.Equals("credit_card", StringComparison.OrdinalIgnoreCase) == true ? PaymentMethod.CreditCard : PaymentMethod.Unknown;
        return new PaymentVerificationResult(response.Success && response.Paid, command.TransactionNsu, response.Amount, response.Paid ? PaymentStatus.Paid : PaymentStatus.Failed, method);
    }
    public Task<PaymentNotification> ParseWebhookAsync(string rawPayload, IReadOnlyDictionary<string, string> headers, CancellationToken ct)
    {
        var webhook = JsonSerializer.Deserialize<WebhookDto>(rawPayload) ?? throw new InvalidOperationException("Webhook inv\u00e1lido.");
        return Task.FromResult(new PaymentNotification(webhook.Order_Nsu ?? string.Empty, webhook.Transaction_Nsu));
    }
}
