namespace Triso.Application.Marketplaces;

public sealed record MarketplaceRequest(string Name, bool Active = true);
public sealed record MarketplaceUpdateRequest(string? Name, bool? Active);
