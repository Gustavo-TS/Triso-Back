using Microsoft.EntityFrameworkCore;
using Triso.Application.Ports.Persistence;
using Triso.Domain.Entities;
namespace Triso.Infrastructure.Persistence.EfCore.Repositories;
public sealed class EfOrderRepository(TrisoDbContext db) : IOrderRepository
{
    public Task AddAsync(Order order, CancellationToken ct) => db.Orders.AddAsync(order, ct).AsTask();
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct) => db.Orders.Include(x => x.Items).Include(x => x.Address).Include(x => x.Payment).Include(x => x.StatusHistory).SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct) => db.Orders.Include(x => x.Items).Include(x => x.Address).Include(x => x.Payment).SingleOrDefaultAsync(x => x.OrderNumber == orderNumber, ct);
    public async Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId, CancellationToken ct) => await db.Orders.AsNoTracking().Include(x => x.Payment).Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    public async Task<IReadOnlyList<Order>> GetOpenByUserAsync(Guid userId, CancellationToken ct) => await db.Orders.AsNoTracking().Include(x => x.Items).Include(x => x.Address).Where(x => x.UserId == userId && x.Status != Domain.Enums.OrderStatus.Delivered && x.Status != Domain.Enums.OrderStatus.Cancelled).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
}
