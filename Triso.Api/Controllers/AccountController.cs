using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Triso.Application.Customers;
using Triso.Application.Orders;

namespace Triso.Api.Controllers;
[ApiController, Authorize, Route("api/v1/account")]
public sealed class AccountController(GetCustomerProfileUseCase profile, UpdateCustomerProfileUseCase update, GetCustomerOrdersUseCase orders, GetOrderDetailsUseCase details) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken ct) => (await profile.ExecuteAsync(UserId(), ct)) is { } value ? Ok(new { data = value }) : Unauthorized();
    [HttpPatch("profile")]
    public async Task<IActionResult> Update(UpdateCustomerProfileRequest request, CancellationToken ct)
    {
        try { return await update.ExecuteAsync(UserId(), request, ct) ? NoContent() : Unauthorized(); }
        catch (InvalidOperationException) { return Conflict(new { error = "E-mail j\u00e1 utilizado." }); }
    }
    [HttpGet("orders")]
    public async Task<IActionResult> ListOrders(CancellationToken ct) => Ok(new { data = await orders.ExecuteAsync(UserId(), ct) });
    [HttpGet("orders/{id:guid}")]
    public async Task<IActionResult> Order(Guid id, CancellationToken ct) => (await details.ExecuteAsync(id, UserId(), ct)) is { } value ? Ok(new { data = value }) : NotFound();
    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
