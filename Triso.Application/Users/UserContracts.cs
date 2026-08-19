namespace Triso.Application.Users;

public sealed record UserCreateRequest(string Name, string Email, string Password, int IdPermission, bool Active = true);
public sealed record UserUpdateRequest(string? Name, string? Email, string? Password, int? IdPermission, bool? Active);
