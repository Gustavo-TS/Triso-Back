using Triso.Application.Ports.Persistence;
namespace Triso.Application.Orders;
public sealed class GetCustomerOrdersUseCase(IOrderRepository orders)
{
    public async Task<IReadOnlyList<OrderSummary>> ExecuteAsync(Guid userId, CancellationToken ct) =>
        (await orders.GetByUserAsync(userId, ct)).Select(x => new OrderSummary(x.Id, x.OrderNumber, x.Status, x.SubtotalCents, x.ShippingCents, x.TotalCents, x.ShippingCarrier is null ? null : new ShippingSummary(x.ShippingCarrier, x.ShippingService!, x.ShippingCents, x.ShippingDeliveryDays ?? 0, x.TrackingCode), x.CreatedAt)).ToList();
}
