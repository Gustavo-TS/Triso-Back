namespace Triso.Application.Auth;

public sealed record LoginRequest(string Email, string Password);
public sealed record BootstrapAdminRequest(string Name, string Email, string Password);
