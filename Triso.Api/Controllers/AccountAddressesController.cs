using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Triso.Application.Customers;

namespace Triso.Api.Controllers;

[ApiController, Authorize, Route("api/v1/account/addresses")]
public sealed class AccountAddressesController(CustomerAddressesUseCase addresses) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(new { data = await addresses.ListAsync(UserId(), ct) });

    [HttpPost]
    public async Task<IActionResult> Create(SaveCustomerAddressRequest request, CancellationToken ct)
    {
        try { return StatusCode(StatusCodes.Status201Created, new { data = await addresses.CreateAsync(UserId(), request, ct) }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveCustomerAddressRequest request, CancellationToken ct)
    {
        try
        {
            var value = await addresses.UpdateAsync(UserId(), id, request, ct);
            return value is null ? NotFound() : Ok(new { data = value });
        }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await addresses.DeleteAsync(UserId(), id, ct) ? NoContent() : NotFound();

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
