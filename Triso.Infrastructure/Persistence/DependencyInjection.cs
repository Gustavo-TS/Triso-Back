using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triso.Application.Ports.Payments;
using Triso.Application.Ports.Persistence;
using Triso.Application.Ports.Security;
using Triso.Infrastructure.Authentication;
using Triso.Application.Ports.Notifications;
using Triso.Infrastructure.Notifications;
using Triso.Infrastructure.Payments.InfinitePay;
using Triso.Infrastructure.Persistence.EfCore;
using Triso.Infrastructure.Persistence.EfCore.Repositories;
using Triso.Application.Ports.Shipping;
using Triso.Infrastructure.Shipping.MelhorEnvio;

namespace Triso.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string databaseUrl, string? databaseName = null)
    {
        services.AddDbContext<TrisoDbContext>(options => options.UseNpgsql(DatabaseUrl.ToConnectionString(databaseUrl, databaseName), npgsql => npgsql.EnableRetryOnFailure()).UseSnakeCaseNamingConvention());
        services.AddScoped<IOrderRepository, EfOrderRepository>(); services.AddScoped<IUserRepository, EfUserRepository>(); services.AddScoped<IProductRepository, EfProductRepository>(); services.AddScoped<IPaymentRepository, EfPaymentRepository>(); services.AddScoped<IOutboxRepository, EfOutboxRepository>(); services.AddScoped<IShippingQuoteRepository, EfShippingQuoteRepository>(); services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IUserPasswordHasher, IdentityUserPasswordHasher>();
        services.AddScoped<INotificationSender, LoggingNotificationSender>();
        services.AddScoped<IShippingGateway, MelhorEnvioShippingGateway>();
        return services;
    }

    public static IServiceCollection AddInfinitePay(this IServiceCollection services, InfinitePayOptions options)
    {
        services.AddSingleton(options);
        services.AddHttpClient<InfinitePayClient>(client => { client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/"); client.Timeout = TimeSpan.FromSeconds(15); });
        services.AddScoped<IPaymentGateway, InfinitePayGateway>();
        return services;
    }
}
