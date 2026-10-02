using System.Text.Json;
using Triso.Application.Ports.Payments;
using Triso.Application.Ports.Persistence;
using Triso.Domain.Entities;
using Triso.Domain.Enums;
namespace Triso.Application.Orders;
public sealed class ProcessPaymentWebhookUseCase(IPaymentGateway gateway, IPaymentRepository payments, IOutboxRepository outbox, IUnitOfWork unitOfWork)
{
    public async Task<bool> ExecuteAsync(string rawPayload, IReadOnlyDictionary<string, string> headers, CancellationToken ct)
    {
        var notification = await gateway.ParseWebhookAsync(rawPayload, headers, ct);
        if (string.IsNullOrWhiteSpace(notification.OrderNsu) || string.IsNullOrWhiteSpace(notification.TransactionNsu)) return false;
        var payment = await payments.GetByOrderNsuAsync(notification.OrderNsu, ct);
        if (payment is null) return false;
        if (await payments.TransactionExistsAsync(notification.TransactionNsu, ct)) return true;
        var verified = await gateway.VerifyPaymentAsync(new PaymentVerificationCommand(notification.OrderNsu, notification.TransactionNsu, notification.InvoiceSlug, payment.Order.TotalCents), ct);
        if (!verified.Confirmed || verified.Status != PaymentStatus.Paid || verified.AmountCents != payment.Order.TotalCents) return false;
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (await payments.TransactionExistsAsync(notification.TransactionNsu, token)) return;
            payment.TransactionNsu = notification.TransactionNsu;
            payment.Method = verified.Method;
            payment.Status = PaymentStatus.Paid;
            payment.InvoiceSlug = notification.InvoiceSlug;
            payment.ReceiptUrl = notification.ReceiptUrl;
            payment.PaidAmountCents = verified.PaidAmountCents;
            payment.Installments = verified.Installments;
            payment.PaidAt = DateTimeOffset.UtcNow;
            payment.UpdatedAt = DateTimeOffset.UtcNow;
            payment.Order.ChangeStatus(OrderStatus.Paid);
            await outbox.AddAsync(new OutboxMessage { Type = "payment.confirmed", Destination = payment.Order.UserId?.ToString() ?? string.Empty, Payload = JsonSerializer.Serialize(new { payment.Order.Id, payment.Order.OrderNumber }) }, token);
            await unitOfWork.SaveChangesAsync(token);
        }, ct);
        return true;
    }
}
