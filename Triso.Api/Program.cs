using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Triso.Api;
using Triso.Api.Filters;
using Triso.Api.Middleware;
using Triso.Application.Customers;
using Triso.Application.Orders;
using Triso.Infrastructure.Persistence;
using Triso.Infrastructure.Payments.InfinitePay;
using Triso.Infrastructure.Payments;
using Triso.Application.Ports.Payments;

EnvLoader.Load();
var builder = WebApplication.CreateBuilder(args);
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") ?? throw new InvalidOperationException("DATABASE_URL não configurada.");
builder.Services.AddPersistence(databaseUrl, builder.Configuration["DatabaseName"]);
var infinitePayHandle = Environment.GetEnvironmentVariable("INFINITEPAY_HANDLE");
var infinitePayBaseUrl = Environment.GetEnvironmentVariable("INFINITEPAY_BASE_URL");
var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
var apiPublicUrl = Environment.GetEnvironmentVariable("API_PUBLIC_URL");
if (!string.IsNullOrWhiteSpace(infinitePayHandle) && !string.IsNullOrWhiteSpace(infinitePayBaseUrl) && !string.IsNullOrWhiteSpace(frontendUrl) && !string.IsNullOrWhiteSpace(apiPublicUrl))
{
    builder.Services.AddInfinitePay(new InfinitePayOptions { Handle = infinitePayHandle, BaseUrl = infinitePayBaseUrl, FrontendUrl = frontendUrl, ApiPublicUrl = apiPublicUrl });
}
else if (builder.Environment.IsDevelopment())
{
    var mockFrontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:5173";
    builder.Services.AddScoped<IPaymentGateway>(_ => new MockPaymentGateway(mockFrontendUrl));
}
else builder.Services.AddScoped<IPaymentGateway, UnconfiguredPaymentGateway>();
builder.Services.AddScoped<RegisterCustomerUseCase>(); builder.Services.AddScoped<GetCustomerProfileUseCase>(); builder.Services.AddScoped<UpdateCustomerProfileUseCase>();
builder.Services.AddScoped<CreateOrderUseCase>(); builder.Services.AddScoped<CreateCheckoutUseCase>(); builder.Services.AddScoped<GetCustomerOrdersUseCase>(); builder.Services.AddScoped<GetOrderDetailsUseCase>(); builder.Services.AddScoped<UpdateOrderStatusUseCase>(); builder.Services.AddScoped<ProcessPaymentWebhookUseCase>();
builder.Services.AddScoped<Triso.Application.Shipping.QuoteShippingUseCase>();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen();
}
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = builder.Environment.IsDevelopment() ? "triso_session" : "__Host-triso_session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = builder.Environment.IsDevelopment() ? SameSiteMode.Strict : SameSiteMode.None;
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
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Permission.Name.ToLowerInvariant()),
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
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PermissionPolicies.Admin, policy =>
        policy.RequireClaim(PermissionPolicies.ClaimType, "1"));
    options.AddPolicy(PermissionPolicies.Manager, policy =>
        policy.RequireClaim(PermissionPolicies.ClaimType, "1", "2"));
    options.AddPolicy(PermissionPolicies.Dashboard, policy =>
        policy.RequireClaim(PermissionPolicies.ClaimType, "1", "2", "3"));
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Muitas requisições. Aguarde e tente novamente."
        }, ct);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
            return RateLimitPartition.GetNoLimiter("non-api");

        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var key = userId is not null
            ? $"user:{userId}"
            : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    options.AddPolicy("public", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("login", context => builder.Environment.IsDevelopment()
        ? RateLimitPartition.GetNoLimiter("development-login")
        : RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(15), QueueLimit = 0 }));
    options.AddPolicy("click", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var origins = (frontendUrl ?? "http://localhost:5173").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
{
    if (builder.Environment.IsDevelopment())
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
            if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
            if (!IPAddress.TryParse(uri.Host, out var address)) return false;
            if (IPAddress.IsLoopback(address)) return true;

            var bytes = address.GetAddressBytes();
            return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                   (bytes[0] == 10 ||
                    bytes[0] == 192 && bytes[1] == 168 ||
                    bytes[0] == 172 && bytes[1] is >= 16 and <= 31);
        });
    }
    else
    {
        policy.WithOrigins(origins);
    }

    policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));
var app = builder.Build();
if (args.Contains("--seed-admin", StringComparer.OrdinalIgnoreCase))
{
    if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("O seed de administrador só pode ser executado em Development.");
    await AdminSeeder.RunAsync(app.Services);
    return;
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseExceptionHandler();
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors("frontend");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
