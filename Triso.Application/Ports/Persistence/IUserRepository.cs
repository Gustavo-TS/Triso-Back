using Triso.Domain.Entities;
namespace Triso.Application.Ports.Persistence;
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct);
    Task<Permission?> GetPermissionByNameAsync(string name, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
}
