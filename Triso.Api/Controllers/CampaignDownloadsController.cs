using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Triso.Application.Analytics;
using Triso.Infrastructure.Persistence;

namespace Triso.Api.Controllers;

[ApiController]
[Route("api/v1/campaigns/dralfredo/downloads")]
[EnableRateLimiting("public")]
public sealed class CampaignDownloadsController(TrisoDbContext db) : ControllerBase
{
    private const string Campaign = "dralfredo";
    private const string CookieName = "dralfredo_download_id";
    private const int CookieMaxAgeSeconds = 31_536_000;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var downloadsCount = await db.CampaignDownloads.LongCountAsync(x => x.Campaign == Campaign, ct);
        return Ok(new { data = new { campaign = Campaign, downloadsCount } });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CampaignDownloadRequest request, CancellationToken ct)
    {
        var anonymousToken = Request.Cookies[CookieName];
        if (!Guid.TryParse(anonymousToken, out _))
        {
            anonymousToken = Guid.NewGuid().ToString();
            Response.Cookies.Append(CookieName, anonymousToken, new CookieOptions
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
            VALUES ({Guid.NewGuid()}, {Campaign}, {tokenHash}, {source}, NOW())
            ON CONFLICT (campaign, anonymous_token_hash) DO NOTHING
            """, ct);
        var downloadsCount = await db.CampaignDownloads.LongCountAsync(x => x.Campaign == Campaign, ct);

        return StatusCode(inserted == 1 ? StatusCodes.Status201Created : StatusCodes.Status200OK,
            new { data = new { campaign = Campaign, downloadsCount, counted = inserted == 1 } });
    }

    private static string? NormalizeSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source)) return null;
        var normalized = source.Trim();
        return normalized.Length <= 40 ? normalized : normalized[..40];
    }
}
