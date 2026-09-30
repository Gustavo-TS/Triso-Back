using Triso.Application.Ports.Persistence;
using Triso.Domain.Entities;

namespace Triso.Application.Customers;

public sealed class CustomerAddressesUseCase(IUserAddressRepository addresses, IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<CustomerAddressResponse>> ListAsync(Guid userId, CancellationToken ct) =>
        (await addresses.GetByUserIdAsync(userId, ct)).Select(ToResponse).ToArray();

    public async Task<CustomerAddressResponse> CreateAsync(Guid userId, SaveCustomerAddressRequest request, CancellationToken ct)
    {
        var values = Normalize(request);
        CustomerAddressResponse? result = null;
        await unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            var existing = await addresses.GetByUserIdAsync(userId, transactionCt);
            var makeDefault = values.IsDefault || existing.Count == 0;
            if (makeDefault && existing.Any(x => x.IsDefault))
            {
                ClearDefault(existing);
                await unitOfWork.SaveChangesAsync(transactionCt);
            }

            var address = new UserAddress
            {
                UserId = userId,
                Label = values.Label,
                RecipientName = values.RecipientName,
                PostalCode = values.PostalCode,
                Street = values.Street,
                Number = values.Number,
                Complement = values.Complement,
                Neighborhood = values.Neighborhood,
                City = values.City,
                State = values.State,
                IsDefault = makeDefault
            };
            await addresses.AddAsync(address, transactionCt);
            await unitOfWork.SaveChangesAsync(transactionCt);
            result = ToResponse(address);
        }, ct);
        return result!;
    }

    public async Task<CustomerAddressResponse?> UpdateAsync(Guid userId, Guid addressId, SaveCustomerAddressRequest request, CancellationToken ct)
    {
        var values = Normalize(request);
        var address = await addresses.GetByIdAndUserIdAsync(addressId, userId, ct);
        if (address is null) return null;

        CustomerAddressResponse? result = null;
        await unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            if (values.IsDefault && !address.IsDefault)
            {
                ClearDefault(await addresses.GetByUserIdAsync(userId, transactionCt), addressId);
                await unitOfWork.SaveChangesAsync(transactionCt);
            }
            address.Label = values.Label;
            address.RecipientName = values.RecipientName;
            address.PostalCode = values.PostalCode;
            address.Street = values.Street;
            address.Number = values.Number;
            address.Complement = values.Complement;
            address.Neighborhood = values.Neighborhood;
            address.City = values.City;
            address.State = values.State;
            address.IsDefault = values.IsDefault;
            address.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync(transactionCt);
            result = ToResponse(address);
        }, ct);
        return result;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid addressId, CancellationToken ct)
    {
        var address = await addresses.GetByIdAndUserIdAsync(addressId, userId, ct);
        if (address is null) return false;

        await unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            var all = await addresses.GetByUserIdAsync(userId, transactionCt);
            var replacement = address.IsDefault
                ? all.Where(x => x.Id != addressId).OrderByDescending(x => x.UpdatedAt).FirstOrDefault()
                : null;
            addresses.Remove(address);
            await unitOfWork.SaveChangesAsync(transactionCt);
            if (replacement is not null)
            {
                replacement.IsDefault = true;
                replacement.UpdatedAt = DateTimeOffset.UtcNow;
                await unitOfWork.SaveChangesAsync(transactionCt);
            }
        }, ct);
        return true;
    }

    private static void ClearDefault(IEnumerable<UserAddress> values, Guid? exceptId = null)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var value in values.Where(x => x.IsDefault && x.Id != exceptId))
        {
            value.IsDefault = false;
            value.UpdatedAt = now;
        }
    }

    private static CustomerAddressResponse ToResponse(UserAddress value) => new(
        value.Id, value.Label, value.RecipientName, value.PostalCode, value.Street, value.Number,
        value.Complement, value.Neighborhood, value.City, value.State, value.IsDefault, value.UpdatedAt);

    private static NormalizedAddress Normalize(SaveCustomerAddressRequest request)
    {
        var postalCode = new string((request.PostalCode ?? string.Empty).Where(char.IsDigit).ToArray());
        if (postalCode.Length != 8) throw new ArgumentException("CEP deve conter 8 dígitos.");

        var state = (request.State ?? string.Empty).Trim().ToUpperInvariant();
        if (state.Length != 2 || !state.All(char.IsLetter)) throw new ArgumentException("UF deve conter exatamente 2 letras.");

        return new NormalizedAddress(
            Required(request.Label, "Apelido do endereço", 60), Required(request.RecipientName, "Nome do destinatário", 120), postalCode,
            Required(request.Street, "Rua", 160), Required(request.Number, "Número", 30),
            Optional(request.Complement, 100), Required(request.Neighborhood, "Bairro", 100),
            Required(request.City, "Cidade", 100), state, request.IsDefault);
    }

    private static string Required(string? value, string field, int maxLength)
    {
        var result = value?.Trim();
        if (string.IsNullOrWhiteSpace(result)) throw new ArgumentException($"{field} é obrigatório.");
        if (result.Length > maxLength) throw new ArgumentException($"{field} deve ter no máximo {maxLength} caracteres.");
        return result;
    }

    private static string? Optional(string? value, int maxLength)
    {
        var result = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (result?.Length > maxLength) throw new ArgumentException($"Complemento deve ter no máximo {maxLength} caracteres.");
        return result;
    }

    private sealed record NormalizedAddress(string Label, string RecipientName, string PostalCode, string Street, string Number, string? Complement, string Neighborhood, string City, string State, bool IsDefault);
}
