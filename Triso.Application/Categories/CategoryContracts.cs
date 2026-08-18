namespace Triso.Application.Categories;

public sealed record CategoryRequest(string Name, bool Active = true);
public sealed record CategoryUpdateRequest(string? Name, bool? Active);
