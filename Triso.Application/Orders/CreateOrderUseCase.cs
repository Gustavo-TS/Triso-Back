using Triso.Application.Ports.Persistence;
using Triso.Domain.Entities;
using Triso.Domain.Enums;
namespace Triso.Application.Orders;
public sealed class CreateOrderUseCase(IProductRepository products, IUserRepository users, IOrderRepository orders, IPaymentRepository payments, IShippingQuoteRepository quotes, IUnitOfWork unitOfWork)
{
    public async Task<CreateOrderResult> ExecuteAsync(Guid userId, CreateOrderRequest request, CancellationToken ct)
    {
        if (request.Items.Count == 0 || request.Items.Any(x => x.Quantity <= 0)) throw new ArgumentException("Itens inv\u00e1lidos.");
        var ids = request.Items.Select(x => x.ProductId).Distinct().ToArray();
        var found = await products.GetActiveByIdsAsync(ids, ct);
        if (found.Count != ids.Length) throw new KeyNotFoundException("Produto inexistente ou inativo.");
        var customer = await users.GetByIdAsync(userId, ct) ?? throw new InvalidOperationException("Usuário autenticado não encontrado.");
        var byId = found.ToDictionary(x => x.Id);
        var quote = request.ShippingQuoteId == Guid.Empty ? null : await quotes.GetByIdAsync(request.ShippingQuoteId, ct);
        var cep = new string(request.Address.PostalCode.Where(char.IsDigit).ToArray());
        var hash = Triso.Application.Shipping.QuoteShippingUseCase.Hash(request.Items.Select(x => new Triso.Application.Shipping.ShippingItemRequest(x.ProductId, x.Quantity)));
        if (quote is null || quote.UserId != userId || quote.ExpiresAt <= DateTimeOffset.UtcNow || quote.DestinationPostalCode != cep || quote.ItemsHash != hash) throw new InvalidOperationException("Cotação de frete inválida ou expirada.");
        var existing = (await orders.GetOpenByUserAsync(userId, ct)).FirstOrDefault(x => IsSameOrder(x, request, quote));
        if (existing is not null) return new CreateOrderResult(existing.Id, true);
        var order = new Order { OrderNumber = $"TRI-{Guid.NewGuid():N}"[..16].ToUpperInvariant(), CustomerName = request.Address.RecipientName.Trim(), CustomerEmail = customer.Email, UserId = userId,
            ShippingQuoteId = quote.Id, ShippingCarrier = quote.Carrier, ShippingService = quote.Service, ShippingCents = quote.PriceCents, ShippingDeliveryDays = quote.DeliveryDays,
            Address = new OrderAddress { RecipientName = request.Address.RecipientName.Trim(), Street = request.Address.Street.Trim(), Number = request.Address.Number.Trim(), Complement = request.Address.Complement?.Trim(), Neighborhood = request.Address.Neighborhood.Trim(), City = request.Address.City.Trim(), State = request.Address.State.Trim(), PostalCode = request.Address.PostalCode.Trim() } };
        foreach (var requested in request.Items)
        {
            var product = byId[requested.ProductId];
            var total = checked(product.PriceCents * requested.Quantity);
            order.Items.Add(new OrderItem { ProductId = product.Id, ProductName = product.Name, UnitPriceCents = product.PriceCents, Quantity = requested.Quantity, TotalCents = total });
            order.SubtotalCents = checked(order.SubtotalCents + total);
        }
        order.TotalCents = checked(order.SubtotalCents + order.ShippingCents);
        order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.PendingPayment, Note = "Pedido criado." });
        await orders.AddAsync(order, ct);
        await payments.AddAsync(new Payment { Order = order, Provider = "infinitepay", OrderNsu = order.Id.ToString(), AmountCents = order.TotalCents }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return new CreateOrderResult(order.Id, false);
    }

    private static bool IsSameOrder(Order order, CreateOrderRequest request, ShippingQuote quote)
    {
        if (order.ShippingCents != quote.PriceCents || order.ShippingDeliveryDays != quote.DeliveryDays || !SameText(order.ShippingCarrier, quote.Carrier) || !SameText(order.ShippingService, quote.Service)) return false;
        var address = order.Address;
        if (!SameText(order.CustomerName, request.Address.RecipientName) || !SameText(address.Street, request.Address.Street) || !SameText(address.Number, request.Address.Number) || !SameText(address.Complement, request.Address.Complement) || !SameText(address.Neighborhood, request.Address.Neighborhood) || !SameText(address.City, request.Address.City) || !SameText(address.State, request.Address.State) || Digits(address.PostalCode) != Digits(request.Address.PostalCode)) return false;
        var requestedItems = request.Items.GroupBy(x => x.ProductId).OrderBy(x => x.Key).Select(x => (x.Key, Quantity: x.Sum(y => y.Quantity))).ToArray();
        var orderItems = order.Items.GroupBy(x => x.ProductId).OrderBy(x => x.Key).Select(x => (x.Key, Quantity: x.Sum(y => y.Quantity))).ToArray();
        return requestedItems.SequenceEqual(orderItems);
    }

    private static bool SameText(string? left, string? right) => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    private static string Digits(string? value) => new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
}
