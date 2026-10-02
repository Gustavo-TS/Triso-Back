using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Triso.Api.Filters;
using Triso.Domain.Enums;
using Triso.Infrastructure.Persistence;

namespace Triso.Api.Controllers;

[ApiController, Route("api/v1/admin"), ManagerAccess]
public sealed class AdminOrdersController(TrisoDbContext db) : ControllerBase
{
    [HttpGet("orders")]
    public async Task<IActionResult> List([FromQuery] OrderStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? sort = "shippingDeadline", CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Orders.AsNoTracking().Include(x => x.Items).Include(x => x.Address).Include(x => x.Payment).AsQueryable();
        if (status is not null) query = query.Where(x => x.Status == status);
        var rows = await query.ToListAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var mapped = rows.Select(x => Summary(x, now)).ToList();
        if (string.Equals(sort, "shippingDeadline", StringComparison.OrdinalIgnoreCase))
            mapped = mapped.OrderBy(x => ShippingRank(x)).ThenBy(x => x.ShippingDeadlineAt ?? DateTimeOffset.MaxValue).ThenByDescending(x => x.CreatedAt).ToList();
        else mapped = mapped.OrderByDescending(x => x.CreatedAt).ToList();
        return Ok(new { data = mapped.Skip((page - 1) * pageSize).Take(pageSize), meta = new { page, pageSize, total = mapped.Count } });
    }

    [HttpGet("orders/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(x => x.Items).Include(x => x.Address).Include(x => x.Payment).Include(x => x.StatusHistory).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is null) return NotFound();
        return Ok(new { data = new { summary = Summary(order, DateTimeOffset.UtcNow), items = order.Items.Select(x => new { x.ProductId, x.ProductName, x.UnitPriceCents, x.Quantity, x.TotalCents }), address = new { order.Address.RecipientName, order.Address.Street, order.Address.Number, order.Address.Complement, order.Address.Neighborhood, order.Address.City, order.Address.State, order.Address.PostalCode }, payment = order.Payment is null ? null : new { order.Payment.Status, order.Payment.Method, order.Payment.PaidAt, order.Payment.PaidAmountCents, order.Payment.Installments, order.Payment.ReceiptUrl }, history = order.StatusHistory.OrderBy(x => x.CreatedAt).Select(x => new { x.Status, x.Note, x.CreatedAt }) } });
    }

    [HttpPatch("orders/{id:guid}/shipping")]
    public async Task<IActionResult> UpdateShipping(Guid id, UpdateShippingRequest request, CancellationToken ct)
    {
        var order = await db.Orders.Include(x => x.Address).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is null) return NotFound();
        if (order.Status is not (OrderStatus.ReadyToShip or OrderStatus.Shipped)) return Conflict(new { error = "Rastreio só pode ser alterado em pedidos prontos para envio ou enviados." });
        var code = new string((request.TrackingCode ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code) || code.Length > 120) return BadRequest(new { error = "Código de rastreio inválido." });
        if (order.ShippingCarrier?.Contains("Correios", StringComparison.OrdinalIgnoreCase) == true && !System.Text.RegularExpressions.Regex.IsMatch(code, "^[A-Z]{2}[0-9]{9}BR$")) return BadRequest(new { error = "Código dos Correios inválido." });
        var previous = order.TrackingCode; order.TrackingCode = code; order.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new() { UserId = UserId(), Action = "order.shipping_tracking_updated", EntityType = "order", EntityId = order.Id, OldData = JsonSerializer.Serialize(new { trackingCode = previous }), NewData = JsonSerializer.Serialize(new { trackingCode = code }) });
        await db.SaveChangesAsync(ct);
        return Ok(new { data = new { order.Id, order.TrackingCode, order.UpdatedAt } });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? region, CancellationToken ct)
    {
        if (from is null || to is null) return BadRequest(new { error = "Informe from e to para a análise histórica." });
        var startDate = from.Value;
        var endDate = to.Value;
        if (endDate < startDate) return BadRequest(new { error = "O período informado é inválido." });
        var selectedRegion = string.IsNullOrWhiteSpace(region) ? null : RegionNames.SingleOrDefault(x => string.Equals(x, region.Trim(), StringComparison.OrdinalIgnoreCase));
        if (region is not null && selectedRegion is null) return BadRequest(new { error = "Região inválida. Use Norte, Nordeste, Centro-Oeste, Sudeste ou Sul." });
        var start = StartOfDayInSaoPaulo(startDate);
        var endExclusive = StartOfDayInSaoPaulo(endDate.AddDays(1));
        var operationRows = await db.Orders.AsNoTracking().Include(x => x.Payment).Where(x => x.Status <= OrderStatus.Shipped).ToListAsync(ct);
        var operationNow = DateTimeOffset.UtcNow;
        var priority = operationRows.Where(x => x.Status is OrderStatus.Paid or OrderStatus.InProduction or OrderStatus.ReadyToShip).Select(x => new { x.Id, x.OrderNumber, x.Status, x.CustomerEmail, x.TotalCents, shippingDeadlineAt = x.Payment?.PaidAt?.AddDays(2) }).OrderBy(x => OperationPriority(x.shippingDeadlineAt, operationNow)).ThenBy(x => x.shippingDeadlineAt ?? DateTimeOffset.MaxValue).Take(5).Select(x => new { x.Id, x.OrderNumber, x.Status, x.CustomerEmail, x.TotalCents, x.shippingDeadlineAt, shippingDeadlineStatus = DeadlineStatus(x.shippingDeadlineAt, operationNow) });
        var createdOrders = await db.Orders.AsNoTracking().Include(x => x.Address).Where(x => x.CreatedAt >= start && x.CreatedAt < endExclusive).ToListAsync(ct);
        var paidStatuses = new[] { OrderStatus.Paid, OrderStatus.InProduction, OrderStatus.ReadyToShip, OrderStatus.Shipped, OrderStatus.Delivered };
        var revenueOrders = await db.Orders.AsNoTracking().Include(x => x.Address).Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x.Category).Include(x => x.Payment)
            .Where(x => x.Payment != null && x.Payment.PaidAt >= start && x.Payment.PaidAt < endExclusive && paidStatuses.Contains(x.Status)).ToListAsync(ct);
        var scopedCreatedOrders = selectedRegion is null ? createdOrders : createdOrders.Where(x => RegionForState(x.Address?.State) == selectedRegion).ToList();
        var scopedRevenueOrders = selectedRegion is null ? revenueOrders : revenueOrders.Where(x => RegionForState(x.Address?.State) == selectedRegion).ToList();
        var grossRevenue = scopedRevenueOrders.Sum(x => x.SubtotalCents);
        var statuses = Enum.GetValues<OrderStatus>().Select(value => new { status = (int)value, count = scopedCreatedOrders.Count(x => x.Status == value) });
        var categories = scopedRevenueOrders.SelectMany(x => x.Items).Where(x => x.Product?.Category is not null).GroupBy(x => new { x.Product!.CategoryId, x.Product.Category.Name }).OrderByDescending(x => x.Sum(y => y.Quantity)).ThenByDescending(x => x.Sum(y => y.TotalCents)).Take(10).Select(x => new { categoryId = x.Key.CategoryId, name = x.Key.Name, quantity = x.Sum(y => y.Quantity), revenueCents = x.Sum(y => y.TotalCents) });
        var regions = RegionNames.Select(region => new { region, rows = revenueOrders.Where(x => RegionForState(x.Address?.State) == region).ToList() }).Select(x => new { region = x.region, ordersCount = x.rows.Count, revenueCents = x.rows.Sum(y => y.SubtotalCents) });
        return Ok(new { data = new { operations = new { updatedAt = operationNow, inProgressCount = operationRows.Count(x => x.Status <= OrderStatus.Shipped), pendingPaymentCount = operationRows.Count(x => x.Status == OrderStatus.PendingPayment), awaitingShipmentCount = operationRows.Count(x => x.Status is OrderStatus.Paid or OrderStatus.InProduction or OrderStatus.ReadyToShip), shippedCount = operationRows.Count(x => x.Status == OrderStatus.Shipped), priorityOrders = priority }, summary = new { grossRevenueCents = grossRevenue, totalOrdersCount = scopedCreatedOrders.Count, cancelledOrdersCount = scopedCreatedOrders.Count(x => x.Status == OrderStatus.Cancelled), paidOrdersCount = scopedRevenueOrders.Count, averageTicketCents = scopedRevenueOrders.Count == 0 ? 0 : grossRevenue / scopedRevenueOrders.Count }, ordersByStatus = statuses, revenueByDay = RevenueByDay(revenueOrders), revenueByRegionDay = RevenueByRegionDay(revenueOrders, startDate, endDate), topProducts = scopedRevenueOrders.SelectMany(x => x.Items).GroupBy(x => new { x.ProductId, x.ProductName }).OrderByDescending(x => x.Sum(y => y.Quantity)).ThenByDescending(x => x.Sum(y => y.TotalCents)).Take(10).Select(x => new { productId = x.Key.ProductId, name = x.Key.ProductName, quantity = x.Sum(y => y.Quantity), revenueCents = x.Sum(y => y.TotalCents) }), topCategories = categories, salesByRegion = regions } });
    }

    private static AdminOrderSummary Summary(Triso.Domain.Entities.Order x, DateTimeOffset now)
    {
        var deadline = x.Payment?.PaidAt?.AddDays(2); var deadlineStatus = deadline is null ? null : deadline.Value.Date < now.Date ? "overdue" : deadline.Value.Date == now.Date ? "dueToday" : "onTime";
        return new(x.Id, x.OrderNumber, x.Status, x.CreatedAt, x.Payment?.PaidAt, x.SubtotalCents, x.ShippingCents, x.TotalCents, x.CustomerName, x.CustomerEmail, x.ShippingCarrier, x.ShippingService, x.ShippingDeliveryDays, x.TrackingCode, deadline, deadlineStatus, x.Items.Sum(i => i.Quantity), x.Items.Select(i => i.ProductName).Distinct().ToArray(), x.Items.Select(i => new AdminOrderItem(i.ProductId, i.ProductName, i.Quantity, i.UnitPriceCents, i.TotalCents)).ToArray(), x.Address is null ? null : $"{x.Address.Street}, {x.Address.Number} - {x.Address.City}/{x.Address.State}");
    }
    private static int ShippingRank(AdminOrderSummary x) => x.Status is OrderStatus.Shipped or OrderStatus.Delivered ? 4 : x.ShippingDeadlineStatus == "overdue" ? 0 : x.ShippingDeadlineStatus == "dueToday" ? 1 : x.ShippingDeadlineAt is null ? 3 : 2;
    private static readonly TimeZoneInfo SaoPauloTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    private static readonly string[] RegionNames = ["Norte", "Nordeste", "Centro-Oeste", "Sudeste", "Sul"];
    private static string? RegionForState(string? state) => state?.Trim().ToUpperInvariant() switch
    {
        "AC" or "AP" or "AM" or "PA" or "RO" or "RR" or "TO" => "Norte",
        "AL" or "BA" or "CE" or "MA" or "PB" or "PE" or "PI" or "RN" or "SE" => "Nordeste",
        "DF" or "GO" or "MT" or "MS" => "Centro-Oeste",
        "ES" or "MG" or "RJ" or "SP" => "Sudeste",
        "PR" or "RS" or "SC" => "Sul",
        _ => null
    };
    private static object[] RevenueByDay(IEnumerable<Triso.Domain.Entities.Order> orders) => orders.GroupBy(x => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(x.Payment!.PaidAt!.Value, SaoPauloTimeZone).DateTime)).OrderBy(x => x.Key).Select(x => (object)new { date = x.Key.ToString("yyyy-MM-dd"), ordersCount = x.Count(), revenueCents = x.Sum(y => y.SubtotalCents) }).ToArray();
    private static object[] RevenueByRegionDay(IEnumerable<Triso.Domain.Entities.Order> orders, DateOnly from, DateOnly to)
    {
        var totals = orders.Where(x => RegionForState(x.Address?.State) is not null)
            .GroupBy(x => new { Region = RegionForState(x.Address!.State)!, Date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(x.Payment!.PaidAt!.Value, SaoPauloTimeZone).DateTime) })
            .ToDictionary(x => (x.Key.Region, x.Key.Date), x => (OrdersCount: x.Count(), RevenueCents: x.Sum(y => y.SubtotalCents)));
        var rows = new List<object>();
        for (var date = from; date <= to; date = date.AddDays(1))
            foreach (var region in RegionNames)
            {
                var value = totals.GetValueOrDefault((region, date));
                rows.Add(new { region, date = date.ToString("yyyy-MM-dd"), ordersCount = value.OrdersCount, revenueCents = value.RevenueCents });
            }
        return rows.ToArray();
    }
    private static string? DeadlineStatus(DateTimeOffset? deadline, DateTimeOffset now) => deadline is null ? null : deadline.Value.Date < now.Date ? "overdue" : deadline.Value.Date == now.Date ? "dueToday" : "onTime";
    private static int OperationPriority(DateTimeOffset? deadline, DateTimeOffset now) => DeadlineStatus(deadline, now) switch { "overdue" => 0, "dueToday" => 1, "onTime" => 2, _ => 3 };
    private static DateTimeOffset StartOfDayInSaoPaulo(DateOnly day) => new(TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), SaoPauloTimeZone));
    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public sealed record UpdateShippingRequest(string? TrackingCode);
public sealed record AdminOrderItem(Guid ProductId, string Name, int Quantity, long UnitPriceCents, long TotalCents);
public sealed record AdminOrderSummary(Guid Id, string OrderNumber, OrderStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? PaidAt, long SubtotalCents, long ShippingCents, long TotalCents, string CustomerName, string CustomerEmail, string? Carrier, string? Service, int? DeliveryDays, string? TrackingCode, DateTimeOffset? ShippingDeadlineAt, string? ShippingDeadlineStatus, int ItemsQuantity, IReadOnlyList<string> ItemNames, IReadOnlyList<AdminOrderItem> Items, string? DeliveryAddress);
