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
        await AlignLegacyUserSchemaAsync(db, ct);
        var user = await db.Users.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Email == email, ct);
        if (user is null)
        {
            user = new User { Name = name, Email = email, PasswordHash = string.Empty };
            db.Users.Add(user);
        }

        user.Name = name;
        user.Role = "admin";
        user.Status = "active";
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
        await db.SaveChangesAsync(ct);

        Console.WriteLine($"Administrador preparado: {email}");
    }

    private static async Task AlignLegacyUserSchemaAsync(TrisoDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE public.users ADD COLUMN IF NOT EXISTS status VARCHAR(20);
            ALTER TABLE public.users ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW();

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'users' AND column_name = 'active'
                ) THEN
                    UPDATE public.users
                    SET status = CASE WHEN active THEN 'active' ELSE 'blocked' END
                    WHERE status IS NULL;
                    ALTER TABLE public.users ALTER COLUMN active SET DEFAULT TRUE;
                END IF;
            END $$;

            UPDATE public.users SET status = 'active' WHERE status IS NULL;
            ALTER TABLE public.users ALTER COLUMN status SET DEFAULT 'active';
            ALTER TABLE public.users ALTER COLUMN status SET NOT NULL;
            """, ct);
    }
}
