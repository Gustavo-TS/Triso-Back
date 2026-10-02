using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Triso.Api.Filters;
using Triso.Application.Shipping;
using Triso.Domain.Entities;
using Triso.Infrastructure.Persistence;
namespace Triso.Api.Controllers;
[ApiController, Route("api/v1/admin/shipping/settings"), AdminOnly]
public sealed class AdminShippingSettingsController(TrisoDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) { var x = await db.StoreShippingSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct); return Ok(new { data = x is null ? null : Map(x) }); }
    [HttpPut] public async Task<IActionResult> Put(UpdateStoreShippingSettingsRequest request, CancellationToken ct)
    { var cep = new string(request.OriginPostalCode.Where(char.IsDigit).ToArray()); var state = request.OriginState?.Trim().ToUpperInvariant(); if (cep.Length != 8 || state is { Length: > 0 } && (state.Length != 2 || !state.All(char.IsLetter))) return BadRequest(new { error = "CEP ou UF inválidos." }); var x = await db.StoreShippingSettings.SingleOrDefaultAsync(x => x.Id == 1, ct) ?? new StoreShippingSettings { OriginPostalCode = cep }; x.OriginPostalCode = cep; x.OriginStreet = request.OriginStreet?.Trim(); x.OriginNumber = request.OriginNumber?.Trim(); x.OriginComplement = request.OriginComplement?.Trim(); x.OriginNeighborhood = request.OriginNeighborhood?.Trim(); x.OriginCity = request.OriginCity?.Trim(); x.OriginState = string.IsNullOrEmpty(state) ? null : state; x.UpdatedAt = DateTimeOffset.UtcNow; x.UpdatedByUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); if (db.Entry(x).State == EntityState.Detached) db.StoreShippingSettings.Add(x); await db.SaveChangesAsync(ct); return Ok(new { data = Map(x) }); }
    private static StoreShippingSettingsResponse Map(StoreShippingSettings x) => new(x.OriginPostalCode, x.OriginStreet, x.OriginNumber, x.OriginComplement, x.OriginNeighborhood, x.OriginCity, x.OriginState, x.UpdatedAt);
}
