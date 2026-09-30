namespace Triso.Infrastructure.Payments.InfinitePay;
internal sealed record InfinitePayItem(int Quantity, long Price, string Description);
internal sealed record InfinitePayCustomer(string Name, string Email);
internal sealed record InfinitePayAddress(string Cep, string Number, string? Complement);
internal sealed record CreateLinkRequest(string Handle, string Redirect_Url, string Webhook_Url, string Order_Nsu, IReadOnlyList<InfinitePayItem> Items, InfinitePayCustomer Customer, InfinitePayAddress Address);
internal sealed record CreateLinkResponse(string? Url);
internal sealed record PaymentCheckRequest(string Handle, string Order_Nsu, string Transaction_Nsu);
internal sealed record PaymentCheckResponse(bool Success, bool Paid, long Amount, string? Capture_Method);
internal sealed record WebhookDto(string? Order_Nsu, string? Transaction_Nsu);
