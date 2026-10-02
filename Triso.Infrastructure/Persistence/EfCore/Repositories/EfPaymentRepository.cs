using Microsoft.EntityFrameworkCore;
using Triso.Application.Ports.Persistence;
using Triso.Domain.Entities;
namespace Triso.Infrastructure.Persistence.EfCore.Repositories;
public sealed class EfPaymentRepository(TrisoDbContext db) : IPaymentRepository
{
    public Task AddAsync(Payment payment, CancellationToken ct) => db.Payments.AddAsync(payment, ct).AsTask();
    public Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct) => db.Payments.Include(x => x.Order).ThenInclude(x => x.Items).SingleOrDefaultAsync(x => x.OrderId == orderId, ct);
    public Task<Payment?> GetByOrderNsuAsync(string orderNsu, CancellationToken ct) => db.Payments.Include(x => x.Order).ThenInclude(x => x.Items).SingleOrDefaultAsync(x => x.OrderNsu == orderNsu, ct);
    public Task<bool> TransactionExistsAsync(string transactionNsu, CancellationToken ct) => db.Payments.AnyAsync(x => x.TransactionNsu == transactionNsu, ct);
}
