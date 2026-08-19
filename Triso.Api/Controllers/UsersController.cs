using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Triso.Api.Filters;
using Triso.Application.Users;
using Triso.Application.Validation;
using Triso.Domain.Entities;
using Triso.Infrastructure.Persistence;

namespace Triso.Api.Controllers;

[ApiController, Route("api/v1/admin/users")]
public sealed class UsersController(TrisoDbContext db) : ControllerBase
{
    [HttpGet, ManagerAccess]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(new
    {
        data = await db.Users.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Email, x.IdPermission, permission = x.Permission.Name, x.Active, x.CreatedAt, x.UpdatedAt })
            .ToListAsync(ct)
    });

    [HttpGet("{id:guid}"), ManagerAccess]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Name, x.Email, x.IdPermission, permission = x.Permission.Name, x.Active, x.CreatedAt, x.UpdatedAt })
            .SingleOrDefaultAsync(ct);
        return user is null ? NotFound() : Ok(new { data = user });
    }

    [HttpPost, AdminOnly]
    public async Task<IActionResult> Create(UserCreateRequest request, CancellationToken ct)
    {
        var errors = UserValidator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

        var permission = await db.Permissions.SingleOrDefaultAsync(x => x.IdPermission == request.IdPermission, ct);
        if (permission is null) return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["idPermission"] = ["Permissão não encontrada."] }));

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, ct))
            return Conflict(new { error = "Já existe um usuário com este e-mail." });

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = string.Empty,
            IdPermission = permission.IdPermission,
            Permission = permission,
            Active = request.Active
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, new
        {
            data = new { user.Id, user.Name, user.Email, user.IdPermission, permission = permission.Name, user.Active, user.CreatedAt, user.UpdatedAt }
        });
    }

    [HttpPatch("{id:guid}"), AdminOnly]
    public async Task<IActionResult> Update(Guid id, UserUpdateRequest request, CancellationToken ct)
    {
        var errors = UserValidator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

        var user = await db.Users.Include(x => x.Permission).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return NotFound();

        Permission? requestedPermission = null;
        if (request.IdPermission.HasValue)
        {
            requestedPermission = await db.Permissions.SingleOrDefaultAsync(x => x.IdPermission == request.IdPermission.Value, ct);
            if (requestedPermission is null) return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["idPermission"] = ["Permissão não encontrada."] }));
        }

        var currentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var removesAdminAccess = requestedPermission is not null && requestedPermission.IdPermission != 1;
        if (id.ToString() == currentId && (request.Active == false || removesAdminAccess))
            return Conflict(new { error = "Você não pode bloquear ou remover o seu próprio acesso administrativo." });

        if (user.IdPermission == 1 && user.Active &&
            (removesAdminAccess || request.Active == false) &&
            !await HasAnotherActiveAdmin(id, ct))
            return Conflict(new { error = "Não é possível remover o último administrador ativo." });

        if (request.Email is not null)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(x => x.Id != id && x.Email == email, ct))
                return Conflict(new { error = "Já existe um usuário com este e-mail." });
            user.Email = email;
        }
        if (request.Name is not null) user.Name = request.Name.Trim();
        if (requestedPermission is not null)
        {
            user.IdPermission = requestedPermission.IdPermission;
            user.Permission = requestedPermission;
        }
        if (request.Active.HasValue) user.Active = request.Active.Value;
        if (request.Password is not null) user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}"), AdminOnly]
    public async Task<IActionResult> Block(Guid id, CancellationToken ct)
    {
        if (id.ToString() == User.FindFirstValue(ClaimTypes.NameIdentifier))
            return Conflict(new { error = "Você não pode bloquear o próprio usuário." });

        var user = await db.Users.Include(x => x.Permission).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return NotFound();
        if (user.IdPermission == 1 && user.Active && !await HasAnotherActiveAdmin(id, ct))
            return Conflict(new { error = "Não é possível bloquear o último administrador ativo." });

        user.Active = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private Task<bool> HasAnotherActiveAdmin(Guid excludedId, CancellationToken ct) =>
        db.Users.AnyAsync(x => x.Id != excludedId && x.Active && x.IdPermission == 1, ct);
}
