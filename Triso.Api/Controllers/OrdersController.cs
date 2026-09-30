using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Triso.Api.Filters;
using Triso.Application.Orders;
using Triso.Domain.Enums;

namespace Triso.Api.Controllers;
[ApiController, Route("api/v1")]
public sealed class OrdersController(CreateOrderUseCase createOrder, CreateCheckoutUseCase checkout, UpdateOrderStatusUseCase updateStatus) : ControllerBase
{
    [Authorize, HttpPost("orders")]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken ct)
    {
        try { var result = await createOrder.ExecuteAsync(UserId(), request, ct); return StatusCode(result.ReusedExistingOrder ? StatusCodes.Status200OK : StatusCodes.Status201Created, new { data = new { id = result.Id, reusedExistingOrder = result.ReusedExistingOrder } }); }
        catch (KeyNotFoundException) { return NotFound(new { error = "Produto inexistente ou inativo." }); }
        catch (ArgumentException) { return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["items"] = ["Informe itens e quantidades v\u00e1lidas."] })); }
        catch (InvalidOperationException) { return Conflict(new { error = "Cotação de frete inválida ou expirada." }); }
    }
    [Authorize, HttpPost("orders/{id:guid}/checkout")]
    public async Task<IActionResult> Checkout(Guid id, CancellationToken ct)
    {
        try { return (await checkout.ExecuteAsync(id, UserId(), ct)) is { } value ? Ok(new { data = value }) : NotFound(); }
        catch (HttpRequestException) { return Problem(statusCode: StatusCodes.Status502BadGateway, title: "N\u00e3o foi poss\u00edvel criar o checkout."); }
        catch (InvalidOperationException) { return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Gateway de pagamento n\u00e3o configurado."); }
    }
    [HttpPatch("admin/orders/{id:guid}/status"), AdminOnly]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateStatusRequest request, CancellationToken ct)
    {
        try { return await updateStatus.ExecuteAsync(id, request.Status, ct) ? NoContent() : NotFound(); }
        catch (InvalidOperationException) { return Conflict(new { error = "Transi\u00e7\u00e3o de status inv\u00e1lida." }); }
    }
    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
public sealed record UpdateStatusRequest(OrderStatus Status);
