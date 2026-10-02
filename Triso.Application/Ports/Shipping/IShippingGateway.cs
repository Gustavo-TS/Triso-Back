namespace Triso.Application.Ports.Shipping;
public interface IShippingGateway { Task<IReadOnlyList<ShippingQuoteResult>> QuoteAsync(ShippingQuoteRequest request, CancellationToken ct); }
