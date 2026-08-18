using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Triso.Api.Filters;
using Triso.Application.Products;
using Triso.Application.Validation;
using Triso.Domain.Entities;
using Triso.Infrastructure.Persistence;

namespace Triso.Api.Controllers;

[ApiController, Route("api/v1/admin/products"), AdminOnly]
public sealed class ProductsController(TrisoDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(new
    {
        data = (await db.Products.AsNoTracking().IgnoreQueryFilters()
            .Where(x => x.DeletedAt == null)
            .Include(x => x.Category).Include(x => x.Images)
            .Include(x => x.MarketplaceLinks).ThenInclude(x => x.Marketplace)
            .AsSplitQuery()
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct)).Select(CatalogController.Map)
    });

    [HttpPost]
    public async Task<IActionResult> Create(ProductRequest request, CancellationToken ct)
    {
        var errors = ProductValidator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        var product = new Product { Name = request.Name.Trim(), Slug = await UniqueSlug(request.Name, null, ct), Description = request.Description.Trim(), PriceCents = request.PriceCents, Badge = Clean(request.Badge, 40), Status = request.Status, CategoryId = request.CategoryId };
        await ApplyChildren(product, request, ct);
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { id = product.Id }, new { data = new { product.Id, product.Slug } });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ProductRequest request, CancellationToken ct)
    {
        var errors = ProductValidator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var product = await db.Products.Include(x => x.MarketplaceLinks).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (product is null) return (IActionResult)NotFound();
            product.Name = request.Name.Trim();
            product.Slug = await UniqueSlug(request.Name, id, ct);
            product.Description = request.Description.Trim();
            product.PriceCents = request.PriceCents;
            product.Badge = Clean(request.Badge, 40);
            product.Status = request.Status;
            product.CategoryId = request.CategoryId;
            product.UpdatedAt = DateTimeOffset.UtcNow;
            await db.ProductImages.IgnoreQueryFilters().Where(x => x.ProductId == id).ExecuteDeleteAsync(ct);
            var orderedImages = request.Images.OrderBy(x => x.DisplayOrder).ToList();
            for (var index = 0; index < orderedImages.Count; index++)
            {
                var image = orderedImages[index];
                db.ProductImages.Add(new ProductImage { ProductId = product.Id, Url = Https(image.Url), AltText = Clean(image.AltText, 200) ?? product.Name, DisplayOrder = index, IsCover = index == 0 });
            }

            var requestedMarketplaceIds = request.MarketplaceLinks.Select(x => x.MarketplaceId).ToHashSet();
            foreach (var removedLink in product.MarketplaceLinks.Where(x => !requestedMarketplaceIds.Contains(x.MarketplaceId)))
            {
                removedLink.Active = false;
                removedLink.UpdatedAt = DateTimeOffset.UtcNow;
            }
            foreach (var link in request.MarketplaceLinks)
            {
                var market = await db.Marketplaces.SingleOrDefaultAsync(x => x.Id == link.MarketplaceId && x.Active, ct);
                if (market is null) throw new BadHttpRequestException("Marketplace inválido.");

                var existingLink = product.MarketplaceLinks.SingleOrDefault(x => x.MarketplaceId == link.MarketplaceId);
                if (existingLink is null)
                {
                    product.MarketplaceLinks.Add(new ProductMarketplaceLink { Product = product, MarketplaceId = market.Id, Url = Https(link.Url), ExternalProductId = Clean(link.ExternalProductId, 120) });
                    continue;
                }

                existingLink.Url = Https(link.Url);
                existingLink.ExternalProductId = Clean(link.ExternalProductId, 120);
                existingLink.Active = true;
                existingLink.UpdatedAt = DateTimeOffset.UtcNow;
            }
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return (IActionResult)NoContent();
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (product is null) return NotFound();
        product.DeletedAt = DateTimeOffset.UtcNow;
        product.Status = "archived";
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task ApplyChildren(Product product, ProductRequest request, CancellationToken ct)
    {
        var orderedImages = request.Images.OrderBy(x => x.DisplayOrder).ToList();
        for (var index = 0; index < orderedImages.Count; index++)
        {
            var image = orderedImages[index];
            product.Images.Add(new ProductImage { Product = product, Url = Https(image.Url), AltText = Clean(image.AltText, 200) ?? product.Name, DisplayOrder = index, IsCover = index == 0 });
        }

        foreach (var link in request.MarketplaceLinks)
        {
            var market = await db.Marketplaces.SingleOrDefaultAsync(x => x.Id == link.MarketplaceId && x.Active, ct);
            if (market is null) throw new BadHttpRequestException("Marketplace inválido.");
            product.MarketplaceLinks.Add(new ProductMarketplaceLink { Product = product, MarketplaceId = market.Id, Url = Https(link.Url), ExternalProductId = Clean(link.ExternalProductId, 120) });
        }
    }

    private async Task<string> UniqueSlug(string name, Guid? id, CancellationToken ct)
    {
        var normalized = name.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var slug = Regex.Replace(new string(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()), "[^a-z0-9]+", "-").Trim('-');
        if (slug.Length == 0) slug = "produto";
        var candidate = slug;
        var suffix = 2;
        while (await db.Products.IgnoreQueryFilters().AnyAsync(x => x.Slug == candidate && x.Id != id, ct)) candidate = $"{slug}-{suffix++}";
        return candidate;
    }

    private static string Https(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || value.Length > 2048)
            throw new BadHttpRequestException("URL HTTPS inválida.");
        return uri.ToString();
    }

    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
}
