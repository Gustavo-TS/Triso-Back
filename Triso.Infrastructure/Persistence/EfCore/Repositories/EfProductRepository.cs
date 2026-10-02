using Microsoft.EntityFrameworkCore;
using Triso.Application.Ports.Persistence;
using Triso.Domain.Entities;
namespace Triso.Infrastructure.Persistence.EfCore.Repositories;
public sealed class EfProductRepository(TrisoDbContext db) : IProductRepository
{
    public async Task<IReadOnlyList<Product>> GetActiveByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) => await db.Products.AsNoTracking().Where(x => ids.Contains(x.Id) && x.Status == "published").ToListAsync(ct);
}
