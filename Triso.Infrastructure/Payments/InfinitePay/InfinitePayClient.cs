using System.Net.Http.Json;
namespace Triso.Infrastructure.Payments.InfinitePay;
internal sealed class InfinitePayClient(HttpClient client)
{
    public async Task<CreateLinkResponse> CreateLinkAsync(CreateLinkRequest request, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync("links", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreateLinkResponse>(cancellationToken: ct) ?? throw new InvalidOperationException("Resposta vazia da InfinitePay.");
    }
    public async Task<PaymentCheckResponse> CheckPaymentAsync(PaymentCheckRequest request, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync("payment_check", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentCheckResponse>(cancellationToken: ct) ?? throw new InvalidOperationException("Resposta vazia da InfinitePay.");
    }
}
