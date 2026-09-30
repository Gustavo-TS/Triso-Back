using Triso.Application.Ports.Persistence;
namespace Triso.Application.Orders;
public sealed class GetOrderDetailsUseCase(IOrderRepository orders)
{
    public async Task<OrderDetails?> ExecuteAsync(Guid orderId, Guid userId, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(orderId, ct);
        if (order is null || order.UserId != userId) return null;
        return new OrderDetails(order.Id, order.OrderNumber, order.Status, order.SubtotalCents, order.ShippingCents, order.TotalCents, order.ShippingCarrier is null ? null : new ShippingSummary(order.ShippingCarrier, order.ShippingService!, order.ShippingCents, order.ShippingDeliveryDays ?? 0, order.TrackingCode),
            order.Items.Select(x => new OrderLine(x.ProductId, x.ProductName, x.UnitPriceCents, x.Quantity, x.TotalCents)).ToList(),
            new OrderAddressRequest(order.CustomerName, order.Address.Street, order.Address.Number, order.Address.Complement, order.Address.Neighborhood, order.Address.City, order.Address.State, order.Address.PostalCode));
    }
}
