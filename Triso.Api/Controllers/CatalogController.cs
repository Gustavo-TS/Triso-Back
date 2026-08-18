using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Triso.Domain.Entities;
using Triso.Infrastructure.Persistence;

namespace Triso.Api.Controllers;

[ApiController, Route("api/v1/catalog"), EnableRateLimiting("public")]
public sealed class CatalogController(TrisoDbContext db) : ControllerBase
{
    [HttpGet("products")]
    public async Task<IActionResult> Products([FromQuery] string? q, [FromQuery] string? category, [FromQuery] int limit = 20, CancellationToken ct = default) { var query = db.Products.AsNoTracking().Where(x => x.Status == "published").Include(x => x.Category).Include(x => x.Images).Include(x => x.MarketplaceLinks).ThenInclude(x => x.Marketplace).AsQueryable(); if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => EF.Functions.ILike(x.Name, $"%{q.Trim()}%") || EF.Functions.ILike(x.Description ?? "", $"%{q.Trim()}%")); if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category.Slug == category); var items = await query.OrderByDescending(x => x.CreatedAt).Take(Math.Clamp(limit, 1, 50)).ToListAsync(ct); return Ok(new { data = items.Select(Map) }); }
    [HttpGet("products/{slug}")] public async Task<IActionResult> Product(string slug, CancellationToken ct) { var value = await db.Products.AsNoTracking().Where(x => x.Status == "published" && x.Slug == slug).Include(x => x.Category).Include(x => x.Images).Include(x => x.MarketplaceLinks).ThenInclude(x => x.Marketplace).SingleOrDefaultAsync(ct); return value is null ? NotFound() : Ok(new { data = Map(value) }); }
    [HttpGet("categories")] public async Task<IActionResult> Categories(CancellationToken ct) => Ok(new { data = await db.Categories.AsNoTracking().Where(x => x.Active).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Slug }).ToListAsync(ct) });
    [HttpGet("marketplaces")] public async Task<IActionResult> Marketplaces(CancellationToken ct) => Ok(new { data = await db.Marketplaces.AsNoTracking().Where(x => x.Active).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Slug }).ToListAsync(ct) });
    internal static object Map(Product x) => new { x.Id, x.Name, x.Slug, x.Description, x.PriceCents, x.Badge, x.Status, category = new { x.CategoryId, name = x.Category?.Name, slug = x.Category?.Slug }, images = x.Images.OrderBy(i => i.DisplayOrder).Select(i => new { i.Id, i.Url, i.AltText, i.IsCover }), marketplaceLinks = x.MarketplaceLinks.Where(l => l.Active).Select(l => new { l.Id, l.Url, l.ExternalProductId, marketplace = new { l.MarketplaceId, name = l.Marketplace?.Name, slug = l.Marketplace?.Slug } }) };
}
