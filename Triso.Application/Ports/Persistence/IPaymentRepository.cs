using Triso.Domain.Entities;
namespace Triso.Application.Ports.Persistence;
public interface IPaymentRepository
{
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct);
    Task<Payment?> GetByOrderNsuAsync(string orderNsu, CancellationToken ct);
    Task<bool> TransactionExistsAsync(string transactionNsu, CancellationToken ct);
    Task AddAsync(Payment payment, CancellationToken ct);
}
