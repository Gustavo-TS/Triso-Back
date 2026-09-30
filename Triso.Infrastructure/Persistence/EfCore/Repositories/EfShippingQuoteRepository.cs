using Microsoft.EntityFrameworkCore;
using Triso.Application.Ports.Persistence;
using Triso.Domain.Entities;
namespace Triso.Infrastructure.Persistence.EfCore.Repositories;
public sealed class EfShippingQuoteRepository(TrisoDbContext db) : IShippingQuoteRepository { public Task AddAsync(ShippingQuote quote, CancellationToken ct) => db.ShippingQuotes.AddAsync(quote, ct).AsTask(); public Task<ShippingQuote?> GetByIdAsync(Guid id, CancellationToken ct) => db.ShippingQuotes.SingleOrDefaultAsync(x => x.Id == id, ct); }
