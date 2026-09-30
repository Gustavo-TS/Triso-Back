using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Triso.Application.Ports.Persistence;
using Triso.Application.Ports.Shipping;
using Triso.Domain.Entities;
namespace Triso.Application.Shipping;
public sealed class QuoteShippingUseCase(IProductRepository products, IShippingGateway gateway, IShippingQuoteRepository quotes, IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<ShippingQuoteResponse>> ExecuteAsync(Guid userId, QuoteShippingRequest request, string originPostalCode, CancellationToken ct, bool requireLogistics = true)
    {
        var cep = new string(request.PostalCode.Where(char.IsDigit).ToArray());
        if (cep.Length != 8 || request.Items.Count == 0 || request.Items.Any(x => x.Quantity <= 0)) throw new ArgumentException("CEP ou itens invalidos.");
        var ids = request.Items.Select(x => x.ProductId).Distinct().ToArray(); var found = await products.GetActiveByIdsAsync(ids, ct);
        if (found.Count != ids.Length) throw new KeyNotFoundException("Produto inexistente ou inativo.");
        if (requireLogistics && found.Any(x => x.RequiresShipping && (x.WeightGrams is null or <= 0 || x.WidthCm is null or <= 0 || x.HeightCm is null or <= 0 || x.LengthCm is null or <= 0))) throw new InvalidOperationException("Produto sem dados logisticos.");
        var byId = found.ToDictionary(x => x.Id); var hash = Hash(request.Items); var snapshot = JsonSerializer.Serialize(request.Items.OrderBy(x => x.ProductId).Select(x => new { productId = x.ProductId, quantity = x.Quantity }));
        var result = await gateway.QuoteAsync(new ShippingQuoteRequest(originPostalCode, cep, request.Items.Select(x => { var p = byId[x.ProductId]; return new ShippingQuoteItem(p.Id, p.Name, x.Quantity, p.PriceCents, p.WeightGrams is > 0 ? p.WeightGrams.Value : 100, p.WidthCm is > 0 ? p.WidthCm.Value : 10, p.HeightCm is > 0 ? p.HeightCm.Value : 10, p.LengthCm is > 0 ? p.LengthCm.Value : 10); }).ToList()), ct);
        var response = new List<ShippingQuoteResponse>();
        foreach (var item in result) { var quote = new ShippingQuote { UserId = userId, OriginPostalCode = originPostalCode, DestinationPostalCode = cep, ItemsHash = hash, ItemsSnapshot = snapshot, Provider = item.Provider, Carrier = item.Carrier, Service = item.Service, PriceCents = item.PriceCents, DeliveryDays = item.DeliveryDays, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30) }; await quotes.AddAsync(quote, ct); response.Add(new(quote.Id, quote.Carrier, quote.Service, quote.PriceCents, quote.DeliveryDays, quote.ExpiresAt)); }
        await unitOfWork.SaveChangesAsync(ct); return response;
    }
    public static string Hash(IEnumerable<ShippingItemRequest> items) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', items.OrderBy(x => x.ProductId).Select(x => $"{x.ProductId:N}:{x.Quantity}")))));
}
