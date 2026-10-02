using Triso.Domain.Entities;
namespace Triso.Application.Ports.Persistence;
public interface IOutboxRepository { Task AddAsync(OutboxMessage message, CancellationToken ct); }
