namespace Triso.Application.Products;

public sealed record ImageRequest(string Url, string? AltText, int DisplayOrder, bool IsCover);
public sealed record MarketplaceLinkRequest(Guid MarketplaceId, string Url, string? ExternalProductId);
public sealed record ProductRequest(string Name, string Description, long PriceCents, string? Badge, string Status, Guid CategoryId, List<ImageRequest> Images, List<MarketplaceLinkRequest> MarketplaceLinks);
