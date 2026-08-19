using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Triso.Api.Filters;
using Triso.Application.Categories;
using Triso.Application.Validation;
using Triso.Domain.Entities;
using Triso.Infrastructure.Persistence;

namespace Triso.Api.Controllers;

[ApiController, Route("api/v1/admin/categories"), ManagerAccess]
public sealed class CategoriesController(TrisoDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Slug, x.Active, x.CreatedAt })
            .ToListAsync(ct);
        return Ok(new { data = categories });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CategoryRequest request, CancellationToken ct)
    {
        var errors = CategoryValidator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

        var category = new Category
        {
            Name = request.Name.Trim(),
            Slug = await UniqueSlug(request.Name, null, ct),
            Active = request.Active
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, new
        {
            data = new { category.Id, category.Name, category.Slug, category.Active, category.CreatedAt }
        });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CategoryUpdateRequest request, CancellationToken ct)
    {
        var errors = CategoryValidator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

        var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (category is null) return NotFound();

        if (request.Name is not null)
        {
            category.Name = request.Name.Trim();
            category.Slug = await UniqueSlug(request.Name, id, ct);
        }
        if (request.Active.HasValue) category.Active = request.Active.Value;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (category is null) return NotFound();

        category.Active = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<string> UniqueSlug(string name, Guid? excludedId, CancellationToken ct)
    {
        var normalized = name.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var slug = Regex.Replace(
            new string(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()),
            "[^a-z0-9]+", "-").Trim('-');
        if (slug.Length == 0) slug = "categoria";

        var candidate = slug;
        var suffix = 2;
        while (await db.Categories.AnyAsync(x => x.Slug == candidate && x.Id != excludedId, ct)) candidate = $"{slug}-{suffix++}";
        return candidate;
    }
}
