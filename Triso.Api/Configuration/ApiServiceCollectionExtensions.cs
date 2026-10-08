using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Triso.Api.Filters;
using Triso.Application.Customers;
using Triso.Application.Orders;
using Triso.Application.Ports.Payments;
using Triso.Infrastructure.Payments;
using Triso.Infrastructure.Payments.InfinitePay;
using Triso.Infrastructure.Persistence;

namespace Triso.Api.Configuration;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationUseCases(this IServiceCollection services)
    {
        services.AddScoped<RegisterCustomerUseCase>();
        services.AddScoped<GetCustomerProfileUseCase>();
        services.AddScoped<UpdateCustomerProfileUseCase>();
        services.AddScoped<CustomerAddressesUseCase>();
        services.AddScoped<CreateOrderUseCase>();
        services.AddScoped<CreateCheckoutUseCase>();
        services.AddScoped<GetCustomerOrdersUseCase>();
        services.AddScoped<GetOrderDetailsUseCase>();
        services.AddScoped<UpdateOrderStatusUseCase>();
        services.AddScoped<ProcessPaymentWebhookUseCase>();
        services.AddScoped<Triso.Application.Shipping.QuoteShippingUseCase>();
        return services;
    }

    public static IServiceCollection AddPaymentGateway(this IServiceCollection services, IWebHostEnvironment environment, string? handle, string? baseUrl, string? frontendUrl, string? apiPublicUrl)
    {
        if (!string.IsNullOrWhiteSpace(handle) && !string.IsNullOrWhiteSpace(baseUrl) && !string.IsNullOrWhiteSpace(frontendUrl) && !string.IsNullOrWhiteSpace(apiPublicUrl))
        {
            services.AddInfinitePay(new InfinitePayOptions { Handle = handle, BaseUrl = baseUrl, FrontendUrl = frontendUrl, ApiPublicUrl = apiPublicUrl });
        }
        else if (environment.IsDevelopment())
        {
            services.AddScoped<IPaymentGateway>(_ => new MockPaymentGateway(frontendUrl ?? "http://localhost:5173"));
        }
        else
        {
            services.AddScoped<IPaymentGateway, UnconfiguredPaymentGateway>();
        }

        return services;
    }

    public static IServiceCollection AddApiAuthenticationAndAuthorization(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
            options.Cookie.Name = environment.IsDevelopment() ? "triso_session" : "__Host-triso_session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.Cookie.SameSite = environment.IsDevelopment() ? SameSiteMode.Strict : SameSiteMode.None;
            options.Cookie.Path = "/";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
            options.Events.OnValidatePrincipal = async context =>
            {
                var idValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(idValue, out var userId))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<TrisoDbContext>();
                var user = await db.Users.AsNoTracking().Include(x => x.Permission)
                    .SingleOrDefaultAsync(x => x.Id == userId && x.Active, context.HttpContext.RequestAborted);
                if (user is null)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email), new Claim(ClaimTypes.Role, user.Permission.Name.ToLowerInvariant()),
                    new Claim(PermissionPolicies.ClaimType, user.IdPermission.ToString())
                };
                var shouldRenew = context.Principal?.FindFirstValue(PermissionPolicies.ClaimType) != user.IdPermission.ToString() ||
                                  context.Principal?.FindFirstValue(ClaimTypes.Role) != user.Permission.Name.ToLowerInvariant() ||
                                  context.Principal?.FindFirstValue(ClaimTypes.Name) != user.Name ||
                                  context.Principal?.FindFirstValue(ClaimTypes.Email) != user.Email;
                context.ReplacePrincipal(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
                context.ShouldRenew = shouldRenew;
            };
        });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PermissionPolicies.Admin, policy => policy.RequireClaim(PermissionPolicies.ClaimType, "1"));
            options.AddPolicy(PermissionPolicies.Manager, policy => policy.RequireClaim(PermissionPolicies.ClaimType, "1", "2"));
            options.AddPolicy(PermissionPolicies.Dashboard, policy => policy.RequireClaim(PermissionPolicies.ClaimType, "1", "2", "3"));
        });
        return services;
    }

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                await context.HttpContext.Response.WriteAsJsonAsync(new { error = "Muitas requisições. Aguarde e tente novamente." }, ct);
            };
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                if (!context.Request.Path.StartsWithSegments("/api")) return RateLimitPartition.GetNoLimiter("non-api");
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var key = userId is not null ? $"user:{userId}" : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions { PermitLimit = 100, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true });
            });
            options.AddPolicy("public", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.AddPolicy("login", context => environment.IsDevelopment() ? RateLimitPartition.GetNoLimiter("development-login") : RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(15), QueueLimit = 0 }));
            options.AddPolicy("click", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        return services;
    }

    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IWebHostEnvironment environment, string? frontendUrl)
    {
        var origins = (frontendUrl ?? "http://localhost:5173").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        services.AddCors(options => options.AddPolicy("frontend", policy =>
        {
            if (environment.IsDevelopment())
            {
                policy.SetIsOriginAllowed(origin =>
                {
                    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
                    if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
                    if (!IPAddress.TryParse(uri.Host, out var address)) return false;
                    if (IPAddress.IsLoopback(address)) return true;
                    var bytes = address.GetAddressBytes();
                    return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                           (bytes[0] == 10 || bytes[0] == 192 && bytes[1] == 168 || bytes[0] == 172 && bytes[1] is >= 16 and <= 31);
                });
            }
            else policy.WithOrigins(origins);
            policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }));
        return services;
    }

    public static IServiceCollection AddApiPlatformServices(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.AddResponseCompression();
        if (environment.IsDevelopment()) services.AddSwaggerGen();
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
        return services;
    }
}
