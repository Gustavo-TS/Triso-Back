using Triso.Application.Ports.Persistence;
using Triso.Application.Ports.Security;
using Triso.Domain.Entities;

namespace Triso.Application.Customers;
public sealed class RegisterCustomerUseCase(IUserRepository users, IUserPasswordHasher passwords, IUnitOfWork unitOfWork)
{
    public async Task<CustomerProfile?> ExecuteAsync(RegisterCustomerRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.GetByEmailAsync(email, ct) is not null) return null;
        var permission = await users.GetPermissionByNameAsync("cliente", ct) ?? throw new InvalidOperationException("Permiss\u00e3o cliente n\u00e3o configurada.");
        var user = new User { Name = request.Name.Trim(), Email = email, PasswordHash = string.Empty, IdPermission = permission.IdPermission, Permission = permission };
        user.PasswordHash = passwords.Hash(user, request.Password);
        await users.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return new CustomerProfile(user.Id, user.Name, user.Email, permission.IdPermission, permission.Name);
    }
}
