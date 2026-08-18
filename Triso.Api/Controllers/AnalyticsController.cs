using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Triso.Api.Filters;
using Triso.Infrastructure.Persistence;

namespace Triso.Api.Controllers;

[ApiController, Route("api/v1/admin/analytics"), AdminOnly]
public sealed class AnalyticsController(TrisoDbContext db) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(DateOnly? from, DateOnly? to, CancellationToken ct) { var start = (from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-29))).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc); var end = (to ?? DateOnly.FromDateTime(DateTime.UtcNow)).AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc); if ((end - start).TotalDays > 367) return BadRequest(new { error = "O período máximo é de 366 dias." }); var clicks = db.MarketplaceClicks.AsNoTracking().Where(x => x.ClickedAt >= start && x.ClickedAt < end); var total = await clicks.CountAsync(ct); var products = await clicks.GroupBy(x => new { x.ProductMarketplaceLink.ProductId, x.ProductMarketplaceLink.Product.Name }).Select(x => new { id = x.Key.ProductId, name = x.Key.Name, clicks = x.Count() }).OrderByDescending(x => x.clicks).Take(10).ToListAsync(ct); var marketplaces = await clicks.GroupBy(x => new { x.ProductMarketplaceLink.MarketplaceId, x.ProductMarketplaceLink.Marketplace.Name }).Select(x => new { id = x.Key.MarketplaceId, name = x.Key.Name, clicks = x.Count() }).OrderByDescending(x => x.clicks).ToListAsync(ct); var daily = await clicks.GroupBy(x => x.ClickedAt.Date).Select(x => new { date = x.Key, clicks = x.Count() }).OrderBy(x => x.date).ToListAsync(ct); return Ok(new { data = new { summary = new { totalClicks = total, topProduct = products.FirstOrDefault(), topMarketplace = marketplaces.FirstOrDefault() }, timeseries = daily, products, marketplaces } }); }
}
