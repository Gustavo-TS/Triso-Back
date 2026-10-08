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
        catch (HttpRequestException exception) { return Problem(statusCode: StatusCodes.Status502BadGateway, title: "Não foi possível criar o checkout.", detail: exception.Message); }
        catch (InvalidOperationException) { return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Gateway de pagamento n\u00e3o configurado."); }
    }
    [HttpPatch("admin/orders/{id:guid}/status"), ManagerAccess]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateStatusRequest request, CancellationToken ct)
    {
        try { var isAdmin = User.HasClaim(PermissionPolicies.ClaimType, "1"); return await updateStatus.ExecuteAsync(id, request.Status, ct, isAdmin) ? NoContent() : NotFound(); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
    }
    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
public sealed record UpdateStatusRequest(OrderStatus Status);
