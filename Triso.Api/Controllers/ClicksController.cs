using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Triso.Application.Analytics;
using Triso.Domain.Entities;
using Triso.Infrastructure.Persistence;

namespace Triso.Api.Controllers;

[ApiController, Route("api/v1/events"), EnableRateLimiting("click")]
public sealed class ClicksController(TrisoDbContext db) : ControllerBase
{
    [HttpPost("marketplace-clicks")]
    public async Task<IActionResult> Create(ClickRequest request, CancellationToken ct) { if (request.EventId == Guid.Empty || request.LinkId == Guid.Empty) return ValidationProblem(); if (!await db.ProductMarketplaceLinks.AnyAsync(x => x.Id == request.LinkId && x.Active, ct)) return NotFound(); if (await db.MarketplaceClicks.AnyAsync(x => x.EventId == request.EventId, ct)) return Accepted(); db.MarketplaceClicks.Add(new MarketplaceClick { EventId = request.EventId, ProductMarketplaceLinkId = request.LinkId, Source = string.IsNullOrWhiteSpace(request.Source) ? null : request.Source.Trim()[..Math.Min(request.Source.Trim().Length, 100)] }); await db.SaveChangesAsync(ct); return Accepted(); }
}
