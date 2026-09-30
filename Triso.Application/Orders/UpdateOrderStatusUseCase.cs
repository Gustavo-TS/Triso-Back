using System.Text.Json;
using Triso.Application.Ports.Persistence;
using Triso.Domain.Entities;
using Triso.Domain.Enums;
namespace Triso.Application.Orders;
public sealed class UpdateOrderStatusUseCase(IOrderRepository orders, IOutboxRepository outbox, IUnitOfWork unitOfWork)
{
    public async Task<bool> ExecuteAsync(Guid orderId, OrderStatus status, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(orderId, ct);
        if (order is null) return false;
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            order.ChangeStatus(status);
            order.StatusHistory.Add(new OrderStatusHistory { Status = status, Note = "Atualizado pela administra\u00e7\u00e3o." });
            await outbox.AddAsync(new OutboxMessage { Type = "order.status_changed", Destination = order.UserId?.ToString() ?? string.Empty, Payload = JsonSerializer.Serialize(new { order.Id, order.OrderNumber, status }) }, token);
            await unitOfWork.SaveChangesAsync(token);
        }, ct);
        return true;
    }
}
