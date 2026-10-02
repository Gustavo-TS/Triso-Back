namespace Triso.Infrastructure.Payments.InfinitePay;
public sealed class InfinitePayOptions
{
    public required string Handle { get; init; }
    public required string BaseUrl { get; init; }
    public required string FrontendUrl { get; init; }
    public required string ApiPublicUrl { get; init; }
}
