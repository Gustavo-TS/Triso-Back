using Triso.Application.Ports.Shipping;
namespace Triso.Infrastructure.Shipping.MelhorEnvio;
public sealed class MelhorEnvioShippingGateway : IShippingGateway
{
    public Task<IReadOnlyList<ShippingQuoteResult>> QuoteAsync(ShippingQuoteRequest request, CancellationToken ct) => Task.FromResult<IReadOnlyList<ShippingQuoteResult>>([new("melhorenvio", "Correios", "PAC", 2590, 6), new("melhorenvio", "Correios", "SEDEX", 3990, 3)]);
}
