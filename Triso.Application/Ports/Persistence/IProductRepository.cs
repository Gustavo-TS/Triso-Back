using Triso.Domain.Entities;
namespace Triso.Application.Ports.Persistence;
public interface IProductRepository { Task<IReadOnlyList<Product>> GetActiveByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct); }
