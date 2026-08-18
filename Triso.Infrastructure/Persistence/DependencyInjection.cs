using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Triso.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string databaseUrl, string? databaseName = null) => services.AddDbContext<TrisoDbContext>(options => options.UseNpgsql(DatabaseUrl.ToConnectionString(databaseUrl, databaseName), npgsql => npgsql.EnableRetryOnFailure()).UseSnakeCaseNamingConvention());
}
