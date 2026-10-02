namespace Triso.Infrastructure.Payments.InfinitePay;
internal sealed record InfinitePayItem(int Quantity, long Price, string Description);
internal sealed record InfinitePayCustomer(string Name, string Email);
internal sealed record InfinitePayAddress(string Cep, string Number, string? Complement);
internal sealed record CreateLinkRequest(string Handle, string Redirect_Url, string Webhook_Url, string Order_Nsu, IReadOnlyList<InfinitePayItem> Items, InfinitePayCustomer Customer, InfinitePayAddress Address);
internal sealed record CreateLinkResponse(string? Url);
internal sealed record PaymentCheckRequest(string Handle, string Order_Nsu, string Transaction_Nsu, string Slug);
internal sealed record PaymentCheckResponse(bool Success, bool Paid, long Amount, long? Paid_Amount, int? Installments, string? Capture_Method);
internal sealed record WebhookDto(string? Invoice_Slug, long? Amount, long? Paid_Amount, int? Installments, string? Capture_Method, string? Transaction_Nsu, string? Order_Nsu, string? Receipt_Url);
