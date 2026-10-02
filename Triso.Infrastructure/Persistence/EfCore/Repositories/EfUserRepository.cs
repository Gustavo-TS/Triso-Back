using Microsoft.EntityFrameworkCore;
using Triso.Application.Ports.Persistence;
using Triso.Domain.Entities;
namespace Triso.Infrastructure.Persistence.EfCore.Repositories;
public sealed class EfUserRepository(TrisoDbContext db) : IUserRepository
{
    public Task AddAsync(User user, CancellationToken ct) => db.Users.AddAsync(user, ct).AsTask();
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) => db.Users.Include(x => x.Permission).SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) => db.Users.SingleOrDefaultAsync(x => x.Email == email, ct);
    public Task<Permission?> GetPermissionByNameAsync(string name, CancellationToken ct) => db.Permissions.SingleOrDefaultAsync(x => x.Name.ToLower() == name.ToLower(), ct);
}
