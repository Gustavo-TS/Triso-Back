namespace Triso.Application.Customers;
public sealed record RegisterCustomerRequest(string Name, string Email, string Password);
public sealed record UpdateCustomerProfileRequest(string? Name, string? Email);
public sealed record CustomerProfile(Guid Id, string Name, string Email, int IdPermission, string Permission);
