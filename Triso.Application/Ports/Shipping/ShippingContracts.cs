namespace Triso.Application.Ports.Shipping;
public sealed record ShippingQuoteItem(Guid ProductId, string Name, int Quantity, long UnitPriceCents, int WeightGrams, decimal WidthCm, decimal HeightCm, decimal LengthCm);
public sealed record ShippingQuoteRequest(string OriginPostalCode, string DestinationPostalCode, IReadOnlyList<ShippingQuoteItem> Items);
public sealed record ShippingQuoteResult(string Provider, string Carrier, string Service, long PriceCents, int DeliveryDays);
