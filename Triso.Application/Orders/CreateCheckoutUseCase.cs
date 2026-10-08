using Triso.Application.Ports.Payments;
using Triso.Application.Ports.Persistence;
using Triso.Domain.Entities;
using Triso.Domain.Enums;
namespace Triso.Application.Orders;
public sealed class CreateCheckoutUseCase(IOrderRepository orders, IPaymentRepository payments, IUserRepository users, IPaymentGateway gateway, IUnitOfWork unitOfWork)
{
    public async Task<CheckoutResponse?> ExecuteAsync(Guid orderId, Guid userId, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(orderId, ct);
        if (order is null || order.UserId != userId || order.Status != OrderStatus.PendingPayment) return null;
        var payment = await payments.GetByOrderIdAsync(orderId, ct);
        if (payment is null) return null;
        if (!string.IsNullOrWhiteSpace(payment.CheckoutUrl)) return new CheckoutResponse(order.Id, payment.CheckoutUrl);
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null || !user.Active) return null;
        var checkoutItems = order.Items.Select(x => new CreateCheckoutItem(x.ProductName, x.UnitPriceCents, x.Quantity)).ToList();
        if (order.ShippingCents > 0)
            checkoutItems.Add(new CreateCheckoutItem($"Frete - {order.ShippingService}", order.ShippingCents, 1));
        var result = await gateway.CreateCheckoutAsync(new(payment.OrderNsu, order.TotalCents,
            checkoutItems,
            new CreateCheckoutCustomer(user.Name, user.Email), new CreateCheckoutAddress(order.Address.Street, order.Address.Number, order.Address.Complement, order.Address.Neighborhood, order.Address.City, order.Address.State, order.Address.PostalCode)), ct);
        payment.CheckoutUrl = result.CheckoutUrl;
        payment.UpdatedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
        return new CheckoutResponse(order.Id, result.CheckoutUrl);
    }
}
