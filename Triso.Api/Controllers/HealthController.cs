using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Triso.Infrastructure.Persistence;

namespace Triso.Api.Controllers;

[ApiController, Route("health")]
public sealed class HealthController(TrisoDbContext db) : ControllerBase
{
    [HttpGet("live")] public IActionResult Live() => Ok(new { status = "ok" });
    [HttpGet("ready")] public async Task<IActionResult> Ready(CancellationToken ct) => await db.Database.CanConnectAsync(ct) ? Ok(new { status = "ready" }) : StatusCode(503);
}
