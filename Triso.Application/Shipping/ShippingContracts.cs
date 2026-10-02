namespace Triso.Application.Shipping;
public sealed record ShippingItemRequest(Guid ProductId, int Quantity);
public sealed record QuoteShippingRequest(string PostalCode, IReadOnlyList<ShippingItemRequest> Items);
public sealed record ShippingQuoteResponse(Guid Id, string Carrier, string Service, long PriceCents, int DeliveryDays, DateTimeOffset ExpiresAt);
