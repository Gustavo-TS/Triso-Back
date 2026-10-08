using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Triso.Api.Filters;
using Triso.Domain.Entities;
using Triso.Domain.Enums;
using Triso.Infrastructure.Persistence;

namespace Triso.Api.Controllers;

[ApiController, Route("api/v1/admin/tasks"), ManagerAccess]
public sealed class AdminTasksController(TrisoDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] Guid? assignedToUserId, CancellationToken ct)
    {
        if (from is null || to is null || to < from) return BadRequest(new { error = "Informe um período válido com from e to." });
        var query = db.AdminTasks.AsNoTracking().Include(x => x.Orders).Include(x => x.AssignedToUser).Where(x => x.DueDate >= from && x.DueDate <= to);
        if (assignedToUserId is not null) query = query.Where(x => x.AssignedToUserId == assignedToUserId);
        var tasks = await query.OrderBy(x => x.IsCompleted).ThenBy(x => x.DueDate).ThenByDescending(x => x.CreatedAt).ToListAsync(ct);
        return Ok(new { data = tasks.Select(Summary) });
    }

    [HttpGet("eligible-orders")]
    public async Task<IActionResult> EligibleOrders(CancellationToken ct)
    {
        var orders = await db.Orders.AsNoTracking().Include(x => x.Items).Include(x => x.Address).Include(x => x.Payment).Where(x => (x.Status == OrderStatus.Paid || x.Status == OrderStatus.InProduction || x.Status == OrderStatus.ReadyToShip) && x.ShippingCarrier != "Retirada na loja").OrderBy(x => x.Payment!.PaidAt).ThenBy(x => x.CreatedAt).ToListAsync(ct);
        return Ok(new { data = orders.Select(OrderSummary) });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var task = await TaskQuery().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return task is null ? NotFound() : Ok(new { data = DetailResponse(task) });
    }

    [HttpPost] public Task<IActionResult> Create(TaskRequest request, CancellationToken ct) => Save(null, request, ct, true);
    [HttpPut("{id:guid}")] public Task<IActionResult> Update(Guid id, TaskRequest request, CancellationToken ct) => Save(id, request, ct, false);

    [HttpPatch("{id:guid}/completion")]
    public async Task<IActionResult> Complete(Guid id, CompletionRequest request, CancellationToken ct)
    {
        var task = await db.AdminTasks.Include(x => x.Orders).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (task is null) return NotFound();
        SetTaskCompletion(task, request.IsCompleted, "manual");
        AddAudit("admin_task.completion_updated", task.Id, new { task.IsCompleted, task.CompletionSource });
        await db.SaveChangesAsync(ct);
        return Ok(new { data = Summary(task) });
    }

    [HttpPatch("{id:guid}/orders/{orderId:guid}/completion")]
    public async Task<IActionResult> CompleteOrder(Guid id, Guid orderId, CompletionRequest request, CancellationToken ct)
    {
        var task = await db.AdminTasks.Include(x => x.Orders).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (task is null) return NotFound();
        var link = task.Orders.SingleOrDefault(x => x.OrderId == orderId);
        if (link is null) return NotFound();
        if (request.IsCompleted) { link.CompletedAt = DateTimeOffset.UtcNow; link.CompletedByUserId = UserId(); } else { link.CompletedAt = null; link.CompletedByUserId = null; }
        if (task.CompletionSource == "automatic") SetTaskCompletion(task, task.Orders.Count > 0 && task.Orders.All(x => x.CompletedAt is not null), "automatic");
        else if (task.Orders.Count > 0 && task.Orders.All(x => x.CompletedAt is not null)) SetTaskCompletion(task, true, "automatic");
        else { task.UpdatedAt = DateTimeOffset.UtcNow; task.UpdatedByUserId = UserId(); }
        AddAudit("admin_task.order_completion_updated", task.Id, new { orderId, request.IsCompleted });
        await db.SaveChangesAsync(ct);
        return Ok(new { data = Summary(task) });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var task = await db.AdminTasks.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (task is null) return NotFound();
        AddAudit("admin_task.deleted", task.Id, new { task.Title }); db.Remove(task);
        await db.SaveChangesAsync(ct); return NoContent();
    }

    private async Task<IActionResult> Save(Guid? id, TaskRequest request, CancellationToken ct, bool create)
    {
        var title = request.Title?.Trim(); var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(); var ids = request.OrderIds?.ToArray() ?? [];
        if (string.IsNullOrWhiteSpace(title) || title.Length > 160 || notes?.Length > 10_000 || !TryParseType(request.Type, out var type)) return BadRequest(new { error = "Título, observações ou tipo inválido." });
        if (ids.Length != ids.Distinct().Count()) return BadRequest(new { error = "orderIds não pode conter pedidos duplicados." });
        if (type == AdminTaskType.ProductRegistration && ids.Length > 0) return BadRequest(new { error = "Cadastro de produto não aceita pedidos vinculados." });
        var orders = await db.Orders.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
        if (orders.Count != ids.Length) return NotFound(new { error = "Um ou mais pedidos não foram encontrados." });
        if (orders.Any(x => x.Status is not (OrderStatus.Paid or OrderStatus.InProduction or OrderStatus.ReadyToShip))) return Conflict(new { error = "Pedido não pode ser vinculado neste status." });
        var task = create ? new AdminTask { Title = title, Type = type, Notes = notes, DueDate = request.DueDate, CreatedByUserId = UserId(), AssignedToUserId = request.AssignedToUserId ?? UserId(), UpdatedByUserId = UserId() } : await db.AdminTasks.Include(x => x.Orders).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (task is null) return NotFound();
        var assignedToUserId = request.AssignedToUserId ?? task.AssignedToUserId;
        if (assignedToUserId is not null && !await db.Users.AnyAsync(x => x.Id == assignedToUserId && x.Active, ct)) return BadRequest(new { error = "Responsável inexistente ou inativo." });
        task.Title = title; task.Type = type; task.Notes = notes; task.DueDate = request.DueDate; task.UpdatedAt = DateTimeOffset.UtcNow; task.UpdatedByUserId = UserId();
        task.AssignedToUserId = assignedToUserId;
        if (!create) db.AdminTaskOrders.RemoveRange(task.Orders.Where(x => !ids.Contains(x.OrderId)));
        foreach (var orderId in ids.Where(orderId => task.Orders.All(x => x.OrderId != orderId))) task.Orders.Add(new AdminTaskOrder { OrderId = orderId });
        if (create) db.AdminTasks.Add(task);
        AddAudit(create ? "admin_task.created" : "admin_task.updated", task.Id, new { task.Title, task.Type, task.DueDate, task.AssignedToUserId, orderIds = ids });
        await db.SaveChangesAsync(ct);
        var response = await TaskQuery().AsNoTracking().SingleAsync(x => x.Id == task.Id, ct);
        return create ? StatusCode(StatusCodes.Status201Created, new { data = DetailResponse(response) }) : Ok(new { data = DetailResponse(response) });
    }

    private IQueryable<AdminTask> TaskQuery() => db.AdminTasks.Include(x => x.AssignedToUser).Include(x => x.Orders).ThenInclude(x => x.Order).ThenInclude(x => x.Items).Include(x => x.Orders).ThenInclude(x => x.Order).ThenInclude(x => x.Address);
    private object Summary(AdminTask x) => new { x.Id, x.Title, type = (short)x.Type, x.Notes, x.DueDate, x.IsCompleted, x.CompletedAt, x.CompletionSource, x.CreatedAt, x.UpdatedAt, assignedTo = x.AssignedToUser is null ? null : new { x.AssignedToUser.Id, x.AssignedToUser.Name, x.AssignedToUser.Email }, orderCount = x.Orders.Count, completedOrderCount = x.Orders.Count(o => o.CompletedAt is not null) };
    private object DetailResponse(AdminTask x) => new { x.Id, x.Title, type = (short)x.Type, x.Notes, x.DueDate, x.IsCompleted, x.CompletedAt, x.CompletionSource, x.CreatedAt, x.UpdatedAt, assignedTo = x.AssignedToUser is null ? null : new { x.AssignedToUser.Id, x.AssignedToUser.Name, x.AssignedToUser.Email }, orders = x.Orders.OrderBy(o => o.CreatedAt).Select(OrderLinkResponse) };
    private object OrderLinkResponse(AdminTaskOrder x) => new { x.Order.Id, x.Order.OrderNumber, status = (int)x.Order.Status, x.Order.CustomerEmail, x.Order.SubtotalCents, shippingCents = x.Order.ShippingCents, x.Order.TotalCents, carrier = x.Order.ShippingCarrier, service = x.Order.ShippingService, deliveryDays = x.Order.ShippingDeliveryDays, trackingCode = x.Order.TrackingCode, deliveryAddress = x.Order.Address is null ? null : $"{x.Order.Address.Street}, {x.Order.Address.Number} - {x.Order.Address.City}/{x.Order.Address.State}", isCompleted = x.CompletedAt is not null, x.CompletedAt, items = x.Order.Items.Select(i => new { i.ProductId, name = i.ProductName, i.Quantity, i.UnitPriceCents, i.TotalCents }) };
    private object OrderSummary(Order x) => new { x.Id, x.OrderNumber, status = (int)x.Status, x.CustomerEmail, x.SubtotalCents, shippingCents = x.ShippingCents, x.TotalCents, carrier = x.ShippingCarrier, service = x.ShippingService, deliveryDays = x.ShippingDeliveryDays, trackingCode = x.TrackingCode, deliveryAddress = x.Address is null ? null : $"{x.Address.Street}, {x.Address.Number} - {x.Address.City}/{x.Address.State}", items = x.Items.Select(i => new { i.ProductId, name = i.ProductName, i.Quantity, i.UnitPriceCents, i.TotalCents }) };
    private void SetTaskCompletion(AdminTask task, bool completed, string source) { task.IsCompleted = completed; task.CompletionSource = source; task.CompletedAt = completed ? DateTimeOffset.UtcNow : null; task.CompletedByUserId = completed ? UserId() : null; task.UpdatedAt = DateTimeOffset.UtcNow; task.UpdatedByUserId = UserId(); }
    private void AddAudit(string action, Guid taskId, object details) => db.AuditLogs.Add(new AuditLog { UserId = UserId(), Action = action, EntityType = "admin_task", EntityId = taskId, NewData = JsonSerializer.Serialize(details) });
    private static bool TryParseType(JsonElement value, out AdminTaskType type)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt16(out var number) && Enum.IsDefined((AdminTaskType)number)) { type = (AdminTaskType)number; return true; }
        var name = value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim().ToLowerInvariant() : null;
        type = name switch { "productregistration" or "product_registration" => AdminTaskType.ProductRegistration, "printorders" or "print_orders" => AdminTaskType.PrintOrders, "shiporders" or "ship_orders" => AdminTaskType.ShipOrders, "general" or "geral" => AdminTaskType.General, _ => default };
        return name is not null && (name is "productregistration" or "product_registration" or "printorders" or "print_orders" or "shiporders" or "ship_orders" or "general" or "geral");
    }
    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public sealed record TaskRequest(string? Title, JsonElement Type, string? Notes, DateOnly DueDate, IReadOnlyList<Guid>? OrderIds, Guid? AssignedToUserId);
public sealed record CompletionRequest(bool IsCompleted);
