using System.Text.Json.Serialization;

namespace Triso.Application.Analytics;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CampaignDownloadRequest(string? Source);
