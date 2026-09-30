using Microsoft.EntityFrameworkCore;
using Triso.Application.Ports.Persistence;
namespace Triso.Infrastructure.Persistence.EfCore;
public sealed class EfUnitOfWork(TrisoDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () => { await using var transaction = await db.Database.BeginTransactionAsync(ct); await operation(ct); await transaction.CommitAsync(ct); });
    }
}
