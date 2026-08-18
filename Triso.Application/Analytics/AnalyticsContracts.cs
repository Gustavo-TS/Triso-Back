namespace Triso.Application.Analytics;

public sealed record ClickRequest(Guid EventId, Guid LinkId, string? Source);
