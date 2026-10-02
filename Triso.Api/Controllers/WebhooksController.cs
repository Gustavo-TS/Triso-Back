using Microsoft.AspNetCore.Mvc;
using Triso.Application.Orders;
namespace Triso.Api.Controllers;
[ApiController, Route("api/v1/webhooks")]
public sealed class WebhooksController(ProcessPaymentWebhookUseCase webhook) : ControllerBase
{
    [HttpPost("infinitepay")]
    public async Task<IActionResult> InfinitePay(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        var headers = Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        try { return await webhook.ExecuteAsync(body, headers, ct) ? Ok(new { success = true }) : BadRequest(new { success = false }); }
        catch (HttpRequestException) { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { success = false }); }
        catch (InvalidOperationException) { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { success = false }); }
        catch (System.Text.Json.JsonException) { return BadRequest(new { success = false }); }
    }
}
