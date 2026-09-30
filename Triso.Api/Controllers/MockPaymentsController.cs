using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Triso.Application.Orders;

namespace Triso.Api.Controllers;

[ApiController, Route("api/v1/mock-payments")]
public sealed class MockPaymentsController(IHostEnvironment environment, ProcessPaymentWebhookUseCase webhook) : ControllerBase
{
    [HttpPost("{orderNsu}/approve")]
    public async Task<IActionResult> Approve(string orderNsu, CancellationToken ct)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var payload = JsonSerializer.Serialize(new { orderNsu, transactionNsu = $"mock-{Guid.NewGuid():N}" });
        return await webhook.ExecuteAsync(payload, new Dictionary<string, string>(), ct)
            ? Ok(new { success = true })
            : BadRequest(new { success = false });
    }
}
