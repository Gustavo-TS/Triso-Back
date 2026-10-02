using System.Text.Json;
using Triso.Application.Ports.Persistence;
using Triso.Domain.Entities;
using Triso.Domain.Enums;
namespace Triso.Application.Orders;
public sealed class UpdateOrderStatusUseCase(IOrderRepository orders, IOutboxRepository outbox, IUnitOfWork unitOfWork)
{
    public async Task<bool> ExecuteAsync(Guid orderId, OrderStatus status, CancellationToken ct, bool forceByAdministrator = false)
    {
        var order = await orders.GetByIdAsync(orderId, ct);
        if (order is null) return false;
        if (!forceByAdministrator && status == OrderStatus.Shipped && order.ShippingCarrier?.Contains("Correios", StringComparison.OrdinalIgnoreCase) == true && string.IsNullOrWhiteSpace(order.TrackingCode))
            throw new InvalidOperationException("Informe o código de rastreio dos Correios antes de enviar o pedido.");
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (forceByAdministrator) order.SetStatusByAdministrator(status); else order.ChangeStatus(status);
            await outbox.AddAsync(new OutboxMessage { Type = "order.status_changed", Destination = order.UserId?.ToString() ?? string.Empty, Payload = JsonSerializer.Serialize(new { order.Id, order.OrderNumber, status }) }, token);
            await unitOfWork.SaveChangesAsync(token);
        }, ct);
        return true;
    }
}
