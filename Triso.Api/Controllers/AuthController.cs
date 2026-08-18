using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
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

            var createdUser = new User
            {
                Name = request.Name.Trim(),
                Email = request.Email.Trim().ToLowerInvariant(),
                PasswordHash = string.Empty,
                Role = "admin"
            };
            createdUser.PasswordHash = new PasswordHasher<User>().HashPassword(createdUser, request.Password);
            db.Users.Add(createdUser);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return createdUser;
        });

        if (user is null) return Conflict(new { error = "O administrador inicial já foi criado." });
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, CreatePrincipal(user));

        return StatusCode(StatusCodes.Status201Created, new { data = new { user.Id, user.Name, user.Email, user.Role } });
    }

    [HttpPost("login"), EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email && x.Status == "active", ct);
        if (user is null || new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new { error = "Credenciais inválidas." });

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, CreatePrincipal(user));
        return Ok(new { data = new { user.Id, user.Name, user.Email, user.Role } });
    }

    [HttpGet("session")]
    public IActionResult Session() => User.Identity?.IsAuthenticated == true
        ? Ok(new { data = new { id = User.FindFirstValue(ClaimTypes.NameIdentifier), name = User.Identity.Name, email = User.FindFirstValue(ClaimTypes.Email), role = User.FindFirstValue(ClaimTypes.Role) } })
        : Unauthorized();

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
            new Claim(ClaimTypes.Role, user.Role)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
