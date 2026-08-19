using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Triso.Domain.Entities;
using Triso.Infrastructure.Persistence;

namespace Triso.Api;

public static class AdminSeeder
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var name = Environment.GetEnvironmentVariable("TRISO_ADMIN_NAME")?.Trim() ?? "Administrador";
        var email = Environment.GetEnvironmentVariable("TRISO_ADMIN_EMAIL")?.Trim().ToLowerInvariant()
            ?? throw new InvalidOperationException("TRISO_ADMIN_EMAIL não configurada.");
        var password = Environment.GetEnvironmentVariable("TRISO_ADMIN_PASSWORD")
            ?? throw new InvalidOperationException("TRISO_ADMIN_PASSWORD não configurada.");
        if (password.Length is < 12 or > 128)
            throw new InvalidOperationException("TRISO_ADMIN_PASSWORD deve ter entre 12 e 128 caracteres.");

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TrisoDbContext>();
        var adminPermission = await db.Permissions.SingleOrDefaultAsync(x => x.Name.ToLower() == "admin", ct)
            ?? throw new InvalidOperationException("A permissão admin não está cadastrada.");
        var user = await db.Users.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Email == email, ct);
        if (user is null)
        {
            user = new User { Name = name, Email = email, PasswordHash = string.Empty };
            db.Users.Add(user);
        }

        user.Name = name;
        user.IdPermission = adminPermission.IdPermission;
        user.Permission = adminPermission;
        user.Active = true;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
        await db.SaveChangesAsync(ct);

        Console.WriteLine($"Administrador preparado: {email}");
    }
}
