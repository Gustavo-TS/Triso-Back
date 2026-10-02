using Triso.Domain.Entities;

namespace Triso.Application.Ports.Persistence;

public interface IUserAddressRepository
{
    Task<IReadOnlyList<UserAddress>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<UserAddress?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken ct);
    Task AddAsync(UserAddress address, CancellationToken ct);
    void Remove(UserAddress address);
}
