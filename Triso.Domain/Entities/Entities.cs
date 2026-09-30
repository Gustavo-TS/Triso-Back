using Triso.Domain.Enums;

namespace Triso.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public int IdPermission { get; set; }
    public Permission Permission { get; set; } = null!;
    public bool Active { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Permission
{
    public int IdPermission { get; set; }
    public required string Name { get; set; }
    public ICollection<User> Users { get; set; } = [];
}

public sealed class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public bool Active { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Product> Products { get; set; } = [];
}

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public long PriceCents { get; set; }
    public string? Badge { get; set; }
    public string Status { get; set; } = "draft";
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<ProductMarketplaceLink> MarketplaceLinks { get; set; } = [];
    public int? WeightGrams { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? LengthCm { get; set; }
    public bool RequiresShipping { get; set; } = true;
}

public sealed class ProductImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public required string Url { get; set; }
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsCover { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Marketplace { public Guid Id { get; set; } = Guid.NewGuid(); public required string Name { get; set; } public required string Slug { get; set; } public string LegacyAllowedDomain { get; set; } = string.Empty; public bool Active { get; set; } = true; }
public sealed class ProductMarketplaceLink { public Guid Id { get; set; } = Guid.NewGuid(); public Guid ProductId { get; set; } public Product Product { get; set; } = null!; public Guid MarketplaceId { get; set; } public Marketplace Marketplace { get; set; } = null!; public required string Url { get; set; } public string? ExternalProductId { get; set; } public bool Active { get; set; } = true; public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow; public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow; }
public sealed class MarketplaceClick { public Guid Id { get; set; } = Guid.NewGuid(); public Guid EventId { get; set; } = Guid.NewGuid(); public Guid ProductMarketplaceLinkId { get; set; } public ProductMarketplaceLink ProductMarketplaceLink { get; set; } = null!; public DateTimeOffset ClickedAt { get; set; } = DateTimeOffset.UtcNow; public string? AnonymousSessionHash { get; set; } public string? Source { get; set; } public string? UserAgentHash { get; set; } }


public sealed class Session
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? OldData { get; set; }
    public string? NewData { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string OrderNumber { get; set; }
    public required string CustomerName { get; set; }
    public required string CustomerEmail { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public OrderStatus Status { get; private set; } = OrderStatus.PendingPayment;
    public long TotalCents { get; set; }
    public long SubtotalCents { get; set; }
    public long ShippingCents { get; set; }
    public Guid? ShippingQuoteId { get; set; }
    public string? ShippingCarrier { get; set; }
    public string? ShippingService { get; set; }
    public int? ShippingDeliveryDays { get; set; }
    public string? TrackingCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<OrderItem> Items { get; set; } = [];
    public OrderAddress Address { get; set; } = null!;
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = [];
    public Payment? Payment { get; set; }

    public void ChangeStatus(OrderStatus next)
    {
        var valid = (Status, next) switch
        {
            (OrderStatus.PendingPayment, OrderStatus.Paid or OrderStatus.Cancelled) => true,
            (OrderStatus.Paid, OrderStatus.InProduction or OrderStatus.Cancelled) => true,
            (OrderStatus.InProduction, OrderStatus.ReadyToShip) => true,
            (OrderStatus.ReadyToShip, OrderStatus.Shipped) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            _ => false
        };
        if (!valid) throw new InvalidOperationException($"Transi\u00e7\u00e3o inv\u00e1lida: {Status} para {next}.");
        Status = next;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid ProductId { get; set; }
    public required string ProductName { get; set; }
    public long UnitPriceCents { get; set; }
    public int Quantity { get; set; }
    public long TotalCents { get; set; }
}

public sealed class OrderAddress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public required string RecipientName { get; set; }
    public required string Street { get; set; }
    public required string Number { get; set; }
    public string? Complement { get; set; }
    public required string Neighborhood { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
    public required string PostalCode { get; set; }
}

public sealed class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public required string Provider { get; set; }
    public required string OrderNsu { get; set; }
    public string? TransactionNsu { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentMethod Method { get; set; } = PaymentMethod.Unknown;
    public long AmountCents { get; set; }
    public string? CheckoutUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class OrderStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public OrderStatus Status { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public required string Type { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Type { get; set; }
    public required string Destination { get; set; }
    public required string Payload { get; set; }
    public string Status { get; set; } = "pending";
    public int Attempts { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? LastError { get; set; }
}

public sealed class ShippingQuote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string OriginPostalCode { get; set; }
    public required string DestinationPostalCode { get; set; }
    public required string ItemsHash { get; set; }
    public required string ItemsSnapshot { get; set; }
    public required string Provider { get; set; }
    public required string Carrier { get; set; }
    public required string Service { get; set; }
    public long PriceCents { get; set; }
    public int DeliveryDays { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class StoreShippingSettings
{
    public short Id { get; set; } = 1;
    public required string OriginPostalCode { get; set; }
    public string? OriginStreet { get; set; }
    public string? OriginNumber { get; set; }
    public string? OriginComplement { get; set; }
    public string? OriginNeighborhood { get; set; }
    public string? OriginCity { get; set; }
    public string? OriginState { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }
}
