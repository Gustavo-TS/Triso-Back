namespace Triso.Application.Customers;

public sealed record SaveCustomerAddressRequest(
    string Label,
    string RecipientName,
    string PostalCode,
    string Street,
    string Number,
    string? Complement,
    string Neighborhood,
    string City,
    string State,
    bool IsDefault = false);

public sealed record CustomerAddressResponse(
    Guid Id,
    string Label,
    string RecipientName,
    string PostalCode,
    string Street,
    string Number,
    string? Complement,
    string Neighborhood,
    string City,
    string State,
    bool IsDefault,
    DateTimeOffset UpdatedAt);
