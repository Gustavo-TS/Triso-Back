using Triso.Application.Ports.Persistence;
using Triso.Domain.Entities;
namespace Triso.Application.Customers;
public sealed class UpdateCustomerProfileUseCase(IUserRepository users, IUnitOfWork unitOfWork)
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
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
