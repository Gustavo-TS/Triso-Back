using Triso.Api;
using Triso.Api.Configuration;
using Triso.Api.Middleware;
using Triso.Infrastructure.Persistence;

EnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? throw new InvalidOperationException("DATABASE_URL não configurada.");
var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");

builder.Services.AddPersistence(databaseUrl, builder.Configuration["DatabaseName"]);
builder.Services.AddApplicationUseCases();
builder.Services.AddPaymentGateway(
    builder.Environment,
    Environment.GetEnvironmentVariable("INFINITEPAY_HANDLE"),
    Environment.GetEnvironmentVariable("INFINITEPAY_BASE_URL"),
    frontendUrl,
    Environment.GetEnvironmentVariable("API_PUBLIC_URL"));
builder.Services.AddApiPlatformServices(builder.Environment);
builder.Services.AddApiAuthenticationAndAuthorization(builder.Environment);
builder.Services.AddApiRateLimiting(builder.Environment);
builder.Services.AddFrontendCors(builder.Environment, frontendUrl);

var app = builder.Build();

if (args.Contains("--seed-admin", StringComparer.OrdinalIgnoreCase))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("O seed de administrador só pode ser executado em Development.");

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
app.UseResponseCompression();
app.UseCors("frontend");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
