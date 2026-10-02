using Microsoft.EntityFrameworkCore;
using Triso.Application.Ports.Persistence;
using Triso.Domain.Entities;

namespace Triso.Infrastructure.Persistence.EfCore.Repositories;

public sealed class EfUserAddressRepository(TrisoDbContext db) : IUserAddressRepository
{
    public async Task<IReadOnlyList<UserAddress>> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        await db.UserAddresses.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.IsDefault).ThenByDescending(x => x.UpdatedAt).ToListAsync(ct);

    public Task<UserAddress?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken ct) =>
        db.UserAddresses.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);

    public Task AddAsync(UserAddress address, CancellationToken ct) => db.UserAddresses.AddAsync(address, ct).AsTask();
    public void Remove(UserAddress address) => db.UserAddresses.Remove(address);
}
