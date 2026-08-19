using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Triso.Api.Filters;
using Triso.Application.Marketplaces;
using Triso.Application.Validation;
using Triso.Domain.Entities;
using Triso.Infrastructure.Persistence;

namespace Triso.Api.Controllers;

[ApiController, Route("api/v1/admin/marketplaces"), ManagerAccess]
public sealed class MarketplacesController(TrisoDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var marketplaces = await db.Marketplaces.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Slug, x.Active })
            .ToListAsync(ct);
        return Ok(new { data = marketplaces });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var marketplace = await db.Marketplaces.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Name, x.Slug, x.Active })
            .SingleOrDefaultAsync(ct);
        return marketplace is null ? NotFound() : Ok(new { data = marketplace });
    }

    [HttpPost]
    public async Task<IActionResult> Create(MarketplaceRequest request, CancellationToken ct)
    {
        var errors = MarketplaceValidator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

        var marketplace = new Marketplace
        {
            Name = request.Name.Trim(),
            Slug = await UniqueSlug(request.Name, null, ct),
            LegacyAllowedDomain = string.Empty,
            Active = request.Active
        };
        db.Marketplaces.Add(marketplace);
        await db.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, new
        {
            data = new { marketplace.Id, marketplace.Name, marketplace.Slug, marketplace.Active }
        });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, MarketplaceUpdateRequest request, CancellationToken ct)
    {
        var errors = MarketplaceValidator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

        var marketplace = await db.Marketplaces.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (marketplace is null) return NotFound();

        if (request.Name is not null)
        {
            marketplace.Name = request.Name.Trim();
            marketplace.Slug = await UniqueSlug(request.Name, id, ct);
        }
        if (request.Active.HasValue) marketplace.Active = request.Active.Value;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var marketplace = await db.Marketplaces.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (marketplace is null) return NotFound();

        marketplace.Active = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<string> UniqueSlug(string name, Guid? excludedId, CancellationToken ct)
    {
        var normalized = name.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var slug = Regex.Replace(
            new string(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()),
            "[^a-z0-9]+", "-").Trim('-');
        if (slug.Length == 0) slug = "marketplace";

        var candidate = slug;
        var suffix = 2;
        while (await db.Marketplaces.AnyAsync(x => x.Slug == candidate && x.Id != excludedId, ct))
            candidate = $"{slug}-{suffix++}";
        return candidate;
    }
}
