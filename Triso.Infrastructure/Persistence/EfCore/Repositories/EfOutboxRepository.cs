using Triso.Application.Ports.Persistence;
using Triso.Domain.Entities;
namespace Triso.Infrastructure.Persistence.EfCore.Repositories;
public sealed class EfOutboxRepository(TrisoDbContext db) : IOutboxRepository { public Task AddAsync(OutboxMessage message, CancellationToken ct) => db.OutboxMessages.AddAsync(message, ct).AsTask(); }
