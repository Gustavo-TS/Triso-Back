using System.Security.Cryptography;
using System.Text;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Triso.Application.Analytics;
using Triso.Infrastructure.Persistence;

namespace Triso.Api.Controllers;

[ApiController]
[Route("api/v1/campaigns/{campaign}/downloads")]
[EnableRateLimiting("public")]
public sealed class CampaignDownloadsController(TrisoDbContext db) : ControllerBase
{
    private const int CookieMaxAgeSeconds = 31_536_000;

    [HttpGet]
    public async Task<IActionResult> Get(string campaign, CancellationToken ct)
    {
        var normalizedCampaign = NormalizeCampaign(campaign);
        if (normalizedCampaign is null) return BadRequest(new { error = "Campanha invÃ¡lida." });

        var downloadsCount = await db.CampaignDownloads.LongCountAsync(x => x.Campaign == normalizedCampaign, ct);
        return Ok(new { data = new { campaign = normalizedCampaign, downloadsCount } });
    }

    [HttpPost]
    public async Task<IActionResult> Create(string campaign, [FromBody] CampaignDownloadRequest request, CancellationToken ct)
    {
        var normalizedCampaign = NormalizeCampaign(campaign);
        if (normalizedCampaign is null) return BadRequest(new { error = "Campanha invÃ¡lida." });

        var cookieName = $"{normalizedCampaign}_download_id";
        var anonymousToken = Request.Cookies[cookieName];
        if (!Guid.TryParse(anonymousToken, out _))
        {
            anonymousToken = Guid.NewGuid().ToString();
            Response.Cookies.Append(cookieName, anonymousToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromSeconds(CookieMaxAgeSeconds),
                Path = "/"
            });
        }

        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(anonymousToken)));
        var source = NormalizeSource(request.Source);
        var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO campaign_downloads (id, campaign, anonymous_token_hash, source, created_at)
            VALUES ({Guid.NewGuid()}, {normalizedCampaign}, {tokenHash}, {source}, NOW())
            ON CONFLICT (campaign, anonymous_token_hash) DO NOTHING
            """, ct);
        var downloadsCount = await db.CampaignDownloads.LongCountAsync(x => x.Campaign == normalizedCampaign, ct);

        return StatusCode(inserted == 1 ? StatusCodes.Status201Created : StatusCodes.Status200OK,
            new { data = new { campaign = normalizedCampaign, downloadsCount, counted = inserted == 1 } });
    }

    [HttpGet("admin/count"), AllowAnonymous]
    public async Task<IActionResult> SetCount(string campaign, [FromQuery(Name = "CONTADOR")] long contador, CancellationToken ct)
    {
        var normalizedCampaign = NormalizeCampaign(campaign);
        if (normalizedCampaign is null) return BadRequest(new { error = "Campanha invÃ¡lida." });
        if (contador < 0) return BadRequest(new { error = "CONTADOR deve ser maior ou igual a zero." });

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE campaign_downloads IN EXCLUSIVE MODE", ct);

            var currentCount = await db.CampaignDownloads.LongCountAsync(x => x.Campaign == normalizedCampaign, ct);
            if (currentCount > contador)
            {
                var excess = currentCount - contador;
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    DELETE FROM campaign_downloads
                    WHERE id IN (
                        SELECT id FROM campaign_downloads
                        WHERE campaign = {normalizedCampaign}
                        ORDER BY created_at DESC, id DESC
                        LIMIT {excess}
                    )
                    """, ct);
            }
            else if (currentCount < contador)
            {
                var missing = contador - currentCount;
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO campaign_downloads (id, campaign, anonymous_token_hash, source, created_at)
                    SELECT gen_random_uuid(), {normalizedCampaign}, md5(gen_random_uuid()::text), 'manual-adjustment', NOW()
                    FROM generate_series(1, {missing})
                    """, ct);
            }

            await transaction.CommitAsync(ct);
        });
        return Ok(new { data = new { campaign = normalizedCampaign, downloadsCount = contador } });
    }

    private static string? NormalizeCampaign(string campaign)
    {
        var normalized = campaign.Trim().ToLowerInvariant();
        if (normalized.Length is < 1 or > 80) return null;

        foreach (var character in normalized)
        {
            if (!(char.IsAsciiLetterOrDigit(character) || character is '-' or '_')) return null;
        }

        return normalized;
    }

    private static string? NormalizeSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source)) return null;
        var normalized = source.Trim();
        return normalized.Length <= 40 ? normalized : normalized[..40];
    }
}
