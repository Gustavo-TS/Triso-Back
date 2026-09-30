namespace Triso.Application.Shipping;
public sealed record StoreShippingSettingsResponse(string OriginPostalCode, string? OriginStreet, string? OriginNumber, string? OriginComplement, string? OriginNeighborhood, string? OriginCity, string? OriginState, DateTimeOffset UpdatedAt);
public sealed record UpdateStoreShippingSettingsRequest(string OriginPostalCode, string? OriginStreet, string? OriginNumber, string? OriginComplement, string? OriginNeighborhood, string? OriginCity, string? OriginState);
