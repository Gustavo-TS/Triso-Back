namespace Triso.Application.Ports.Persistence;
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct);
}
