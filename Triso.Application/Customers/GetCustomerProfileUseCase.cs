using Triso.Application.Ports.Persistence;
namespace Triso.Application.Customers;
public sealed class GetCustomerProfileUseCase(IUserRepository users)
{
    public async Task<CustomerProfile?> ExecuteAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct);
        return user is null || !user.Active ? null : new CustomerProfile(user.Id, user.Name, user.Email, user.IdPermission, user.Permission.Name);
    }
}
