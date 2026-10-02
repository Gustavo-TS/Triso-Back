using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Triso.Application.Shipping;
using Triso.Infrastructure.Persistence;
namespace Triso.Api.Controllers;
[ApiController, Authorize, Route("api/v1/shipping")]
public sealed class ShippingController(QuoteShippingUseCase quotes, TrisoDbContext db) : ControllerBase
{
    [HttpPost("quotes")]
    public async Task<IActionResult> Quote(QuoteShippingRequest request, CancellationToken ct)
    {
        try { var origin = await db.StoreShippingSettings.AsNoTracking().Where(x => x.Id == 1).Select(x => x.OriginPostalCode).SingleOrDefaultAsync(ct) ?? Environment.GetEnvironmentVariable("TRISO_ORIGIN_POSTAL_CODE"); if (string.IsNullOrWhiteSpace(origin)) return UnprocessableEntity(new { error = "Origem de envio não configurada." }); var realProvider = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MELHOR_ENVIO_TOKEN")); return Ok(new { data = new { destinationPostalCode = request.PostalCode, quotes = await quotes.ExecuteAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), request, origin, ct, realProvider) } }); }
        catch (ArgumentException) { return BadRequest(new { error = "CEP ou itens inválidos." }); }
        catch (KeyNotFoundException) { return NotFound(new { error = "Produto inexistente ou inativo." }); }
        catch (InvalidOperationException) { return UnprocessableEntity(new { error = "Produto sem dados logísticos ou origem não configurada." }); }
    }
}
