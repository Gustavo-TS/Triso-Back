using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Triso.Api.Filters;
using Triso.Infrastructure.Persistence;

namespace Triso.Api.Controllers;

[ApiController, Route("api/v1/admin/permissions"), ManagerAccess]
public sealed class PermissionsController(TrisoDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(new
    {
        data = await db.Permissions.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new { x.IdPermission, permission = x.Name })
            .ToListAsync(ct)
    });
}
