using Triso.Domain.Entities;
namespace Triso.Application.Ports.Persistence;
public interface IShippingQuoteRepository { Task AddAsync(ShippingQuote quote, CancellationToken ct); Task<ShippingQuote?> GetByIdAsync(Guid id, CancellationToken ct); }
