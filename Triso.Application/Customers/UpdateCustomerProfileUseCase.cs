using Triso.Application.Ports.Persistence;
using Triso.Application.Ports.Security;
using Triso.Domain.Entities;
namespace Triso.Application.Customers;
public sealed class UpdateCustomerProfileUseCase(IUserRepository users, IUserPasswordHasher passwords, IUnitOfWork unitOfWork)
{
    public async Task<bool> ExecuteAsync(Guid userId, UpdateCustomerProfileRequest request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null || !user.Active) return false;
        if (request.Email is not null)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var existing = await users.GetByEmailAsync(email, ct);
            if (existing is not null && existing.Id != userId) throw new InvalidOperationException("E-mail j\u00e1 utilizado.");
            user.Email = email;
        }
        if (request.Name is not null) user.Name = request.Name.Trim();
        if (request.Password is not null) user.PasswordHash = passwords.Hash(user, request.Password);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
