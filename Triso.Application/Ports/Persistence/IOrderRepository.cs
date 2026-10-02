using Triso.Domain.Entities;

namespace Triso.Application.Ports.Persistence;
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct);
    Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<Order>> GetOpenByUserAsync(Guid userId, CancellationToken ct);
    Task AddAsync(Order order, CancellationToken ct);
}
