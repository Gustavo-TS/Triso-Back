using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Triso.Api.Filters;
using Triso.Application.Auth;
using Triso.Application.Validation;
using Triso.Domain.Entities;
using Triso.Infrastructure.Persistence;

namespace Triso.Api.Controllers;

[ApiController, Route("api/v1/auth")]
public sealed class AuthController(TrisoDbContext db) : ControllerBase
{
    [HttpPost("bootstrap"), EnableRateLimiting("login")]
    public async Task<IActionResult> Bootstrap(BootstrapAdminRequest request, CancellationToken ct)
    {
        var errors = AuthValidator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

        var strategy = db.Database.CreateExecutionStrategy();
        var user = await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            if (await db.Users.AnyAsync(ct)) return null;
            var adminPermission = await db.Permissions.SingleOrDefaultAsync(x => x.Name.ToLower() == "admin", ct)
                ?? throw new InvalidOperationException("A permissão admin não está cadastrada.");

            var createdUser = new User
            {
                Name = request.Name.Trim(),
                Email = request.Email.Trim().ToLowerInvariant(),
                PasswordHash = string.Empty,
                IdPermission = adminPermission.IdPermission,
                Permission = adminPermission
            };
            createdUser.PasswordHash = new PasswordHasher<User>().HashPassword(createdUser, request.Password);
            db.Users.Add(createdUser);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return createdUser;
        });

        if (user is null) return Conflict(new { error = "O administrador inicial já foi criado." });
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, CreatePrincipal(user));

        return StatusCode(StatusCodes.Status201Created, new { data = new { user.Id, user.Name, user.Email, user.IdPermission, permission = user.Permission.Name, user.Active } });
    }

    [HttpPost("login"), EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.Include(x => x.Permission).SingleOrDefaultAsync(x => x.Email == email && x.Active, ct);
        if (user is null || new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new { error = "Credenciais inválidas." });

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, CreatePrincipal(user));
        return Ok(new { data = new { user.Id, user.Name, user.Email, user.IdPermission, permission = user.Permission.Name, user.Active } });
    }

    [HttpGet("session")]
    public IActionResult Session()
    {
        if (User.Identity?.IsAuthenticated != true) return Unauthorized();
        _ = int.TryParse(User.FindFirstValue(PermissionPolicies.ClaimType), out var idPermission);
        return Ok(new { data = new { id = User.FindFirstValue(ClaimTypes.NameIdentifier), name = User.Identity.Name, email = User.FindFirstValue(ClaimTypes.Email), idPermission, permission = User.FindFirstValue(ClaimTypes.Role) } });
    }

    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return NoContent();
    }

    private static ClaimsPrincipal CreatePrincipal(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Permission.Name.ToLowerInvariant()),
            new Claim(PermissionPolicies.ClaimType, user.IdPermission.ToString())
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
