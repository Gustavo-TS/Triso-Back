using Microsoft.EntityFrameworkCore;
using Triso.Domain.Entities;
using Triso.Domain.Enums;

namespace Triso.Infrastructure.Persistence;

public sealed class TrisoDbContext(DbContextOptions<TrisoDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Marketplace> Marketplaces => Set<Marketplace>();
    public DbSet<ProductMarketplaceLink> ProductMarketplaceLinks => Set<ProductMarketplaceLink>();
    public DbSet<MarketplaceClick> MarketplaceClicks => Set<MarketplaceClick>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderAddress> OrderAddresses => Set<OrderAddress>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentWebhookEvent> PaymentWebhookEvents => Set<PaymentWebhookEvent>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ShippingQuote> ShippingQuotes => Set<ShippingQuote>();
    public DbSet<StoreShippingSettings> StoreShippingSettings => Set<StoreShippingSettings>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasDefaultSchema("public");

        model.Entity<User>(entity =>
        {
            entity.ToTable("users");
            ConfigureId(entity);
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.Active).HasDefaultValue(true);
            ConfigureCreatedAt(entity.Property(x => x.CreatedAt));
            ConfigureUpdatedAt(entity.Property(x => x.UpdatedAt));
            entity.HasIndex(x => x.IdPermission);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasOne(x => x.Permission).WithMany(x => x.Users).HasForeignKey(x => x.IdPermission).OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<UserAddress>(entity =>
        {
            entity.ToTable("user_addresses", table =>
            {
                table.HasCheckConstraint("chk_user_addresses_postal_code", "postal_code ~ '^[0-9]{8}$'");
                table.HasCheckConstraint("chk_user_addresses_state", "state ~ '^[A-Z]{2}$'");
            });
            ConfigureId(entity);
            entity.Property(x => x.Label).HasMaxLength(60);
            entity.Property(x => x.RecipientName).HasMaxLength(120);
            entity.Property(x => x.PostalCode).HasMaxLength(8);
            entity.Property(x => x.Street).HasMaxLength(160);
            entity.Property(x => x.Number).HasMaxLength(30);
            entity.Property(x => x.Complement).HasMaxLength(100);
            entity.Property(x => x.Neighborhood).HasMaxLength(100);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.State).HasMaxLength(2);
            entity.Property(x => x.IsDefault).HasDefaultValue(false);
            ConfigureCreatedAt(entity.Property(x => x.CreatedAt));
            ConfigureUpdatedAt(entity.Property(x => x.UpdatedAt));
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.UserId).IsUnique().HasFilter("is_default").HasDatabaseName("ux_user_addresses_one_default_per_user");
            entity.HasOne(x => x.User).WithMany(x => x.Addresses).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<Permission>(entity =>
        {
            entity.ToTable("permission");
            entity.HasKey(x => x.IdPermission);
            entity.Property(x => x.IdPermission).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).HasColumnName("permission").HasMaxLength(100);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        model.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            ConfigureId(entity);
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.Slug).HasMaxLength(140);
            entity.Property(x => x.Active).HasDefaultValue(true);
            ConfigureCreatedAt(entity.Property(x => x.CreatedAt));
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        model.Entity<Product>(entity =>
        {
            entity.ToTable("products", table =>
            {
                table.HasCheckConstraint("chk_products_price", "price_cents >= 0");
                table.HasCheckConstraint("chk_products_status", "status IN ('draft', 'published', 'archived')");
            });
            ConfigureId(entity);
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.Slug).HasMaxLength(160);
            entity.Property(x => x.Description).HasColumnType("text");
            entity.Property(x => x.Badge).HasMaxLength(40);
            entity.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("draft");
            entity.Property(x => x.WidthCm).HasPrecision(8, 2); entity.Property(x => x.HeightCm).HasPrecision(8, 2); entity.Property(x => x.LengthCm).HasPrecision(8, 2); entity.Property(x => x.RequiresShipping).HasDefaultValue(true);
            ConfigureCreatedAt(entity.Property(x => x.CreatedAt));
            ConfigureUpdatedAt(entity.Property(x => x.UpdatedAt));
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => x.CategoryId);
            entity.HasIndex(x => x.Status);
            entity.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => x.DeletedAt == null);
        });

        model.Entity<ProductImage>(entity =>
        {
            entity.ToTable("product_images", table => table.HasCheckConstraint("chk_product_images_order", "display_order >= 0"));
            ConfigureId(entity);
            entity.Property(x => x.AltText).HasMaxLength(200);
            entity.Property(x => x.DisplayOrder).HasDefaultValue(0);
            entity.Property(x => x.IsCover).HasDefaultValue(false);
            ConfigureCreatedAt(entity.Property(x => x.CreatedAt));
            entity.HasIndex(x => x.ProductId);
            entity.HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(x => x.Product.DeletedAt == null);
        });

        model.Entity<Marketplace>(entity =>
        {
            entity.ToTable("marketplaces");
            ConfigureId(entity);
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.Slug).HasMaxLength(100);
            entity.Property(x => x.LegacyAllowedDomain).HasColumnName("allowed_domain").HasMaxLength(255);
            entity.Property(x => x.Active).HasDefaultValue(true);
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        model.Entity<ProductMarketplaceLink>(entity =>
        {
            entity.ToTable("product_marketplace_links");
            ConfigureId(entity);
            entity.Property(x => x.ExternalProductId).HasMaxLength(120);
            entity.Property(x => x.Active).HasDefaultValue(true);
            ConfigureCreatedAt(entity.Property(x => x.CreatedAt));
            ConfigureUpdatedAt(entity.Property(x => x.UpdatedAt));
            entity.HasIndex(x => new { x.ProductId, x.MarketplaceId }).IsUnique();
            entity.HasOne(x => x.Product).WithMany(x => x.MarketplaceLinks).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Marketplace).WithMany().HasForeignKey(x => x.MarketplaceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => x.Product.DeletedAt == null);
        });

        model.Entity<MarketplaceClick>(entity =>
        {
            entity.ToTable("marketplace_clicks");
            ConfigureId(entity);
            entity.Property(x => x.EventId).HasDefaultValueSql("gen_random_uuid()");
            ConfigureCreatedAt(entity.Property(x => x.ClickedAt));
            entity.Property(x => x.Source).HasMaxLength(100);
            entity.HasIndex(x => x.EventId).IsUnique();
            entity.HasIndex(x => x.ClickedAt);
            entity.HasIndex(x => new { x.ProductMarketplaceLinkId, x.ClickedAt });
            entity.HasOne(x => x.ProductMarketplaceLink).WithMany().HasForeignKey(x => x.ProductMarketplaceLinkId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => x.ProductMarketplaceLink.Product.DeletedAt == null);
        });

        model.Entity<Session>(entity =>
        {
            entity.ToTable("sessions");
            ConfigureId(entity);
            ConfigureCreatedAt(entity.Property(x => x.CreatedAt));
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.ExpiresAt);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            ConfigureId(entity);
            entity.Property(x => x.Action).HasMaxLength(100);
            entity.Property(x => x.EntityType).HasMaxLength(100);
            entity.Property(x => x.OldData).HasColumnType("jsonb");
            entity.Property(x => x.NewData).HasColumnType("jsonb");
            ConfigureCreatedAt(entity.Property(x => x.CreatedAt));
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => new { x.EntityType, x.EntityId });
            entity.HasIndex(x => x.CreatedAt).IsDescending();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        model.Entity<Order>(entity =>
        {
            entity.ToTable("orders"); ConfigureId(entity);
            entity.Property(x => x.OrderNumber).HasMaxLength(32);
            entity.Property(x => x.CustomerName).HasColumnName("customer_name").HasMaxLength(120);
            entity.Property(x => x.CustomerEmail).HasColumnName("customer_email").HasMaxLength(254);
            entity.Property(x => x.ShippingCents).HasColumnName("shipping_price_cents");
            entity.Property(x => x.Status).HasConversion(value => OrderStatusToDatabase(value), value => OrderStatusFromDatabase(value)).HasMaxLength(30);
            ConfigureCreatedAt(entity.Property(x => x.CreatedAt)); ConfigureUpdatedAt(entity.Property(x => x.UpdatedAt));
            entity.HasIndex(x => x.OrderNumber).IsUnique(); entity.HasIndex(x => x.UserId); entity.HasIndex(x => x.Status);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });
        model.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items"); ConfigureId(entity); entity.Property(x => x.ProductName).HasMaxLength(120);
            entity.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<OrderAddress>(entity =>
        {
            entity.ToTable("order_addresses"); ConfigureId(entity); entity.Ignore(x => x.RecipientName);
            entity.Property(x => x.Street).HasMaxLength(160); entity.Property(x => x.Number).HasMaxLength(30); entity.Property(x => x.Complement).HasMaxLength(100);
            entity.Property(x => x.Neighborhood).HasMaxLength(100); entity.Property(x => x.City).HasMaxLength(100); entity.Property(x => x.State).HasMaxLength(40); entity.Property(x => x.PostalCode).HasColumnName("cep").HasMaxLength(20);
            entity.HasIndex(x => x.OrderId).IsUnique(); entity.HasOne(x => x.Order).WithOne(x => x.Address).HasForeignKey<OrderAddress>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<Payment>(entity =>
        {
            entity.ToTable("payments"); ConfigureId(entity); entity.Property(x => x.Provider).HasMaxLength(50); entity.Property(x => x.OrderNsu).HasMaxLength(100); entity.Property(x => x.TransactionNsu).HasMaxLength(100);
            entity.Property(x => x.Status).HasConversion(value => PaymentStatusToDatabase(value), value => PaymentStatusFromDatabase(value)).HasMaxLength(20); entity.Property(x => x.Method).HasColumnName("capture_method").HasConversion(value => PaymentMethodToDatabase(value), value => PaymentMethodFromDatabase(value)).HasMaxLength(20); entity.Property(x => x.CheckoutUrl).HasMaxLength(2048); entity.Property(x => x.InvoiceSlug).HasColumnName("invoice_slug").HasMaxLength(200); entity.Property(x => x.ReceiptUrl).HasColumnName("receipt_url").HasMaxLength(2048); entity.Property(x => x.PaidAmountCents).HasColumnName("paid_amount_cents"); entity.Property(x => x.PaidAt).HasColumnName("paid_at");
            ConfigureCreatedAt(entity.Property(x => x.CreatedAt)); ConfigureUpdatedAt(entity.Property(x => x.UpdatedAt));
            entity.HasIndex(x => x.OrderId).IsUnique(); entity.HasIndex(x => x.OrderNsu).IsUnique(); entity.HasIndex(x => x.TransactionNsu).IsUnique().HasFilter("transaction_nsu IS NOT NULL");
            entity.HasOne(x => x.Order).WithOne(x => x.Payment).HasForeignKey<Payment>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<PaymentWebhookEvent>(entity => { entity.ToTable("payment_webhook_events"); ConfigureId(entity); entity.Property(x => x.Provider).HasMaxLength(50); entity.Property(x => x.OrderNsu).HasMaxLength(100); entity.Property(x => x.ExternalTransactionId).HasMaxLength(100); entity.Property(x => x.ExternalInvoiceSlug).HasMaxLength(200); entity.Property(x => x.Status).HasMaxLength(30); entity.Property(x => x.FailureReason).HasMaxLength(500); entity.Property(x => x.SanitizedPayload).HasColumnType("jsonb"); ConfigureCreatedAt(entity.Property(x => x.ReceivedAt)); entity.HasIndex(x => x.OrderId); entity.HasIndex(x => new { x.Provider, x.ExternalTransactionId }).IsUnique().HasFilter("external_transaction_id IS NOT NULL"); entity.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.SetNull); });
        model.Entity<OrderStatusHistory>(entity => { entity.ToTable("order_status_history"); ConfigureId(entity); entity.Property(x => x.Status).HasColumnName("new_status").HasConversion(value => OrderStatusToDatabase(value), value => OrderStatusFromDatabase(value)).HasMaxLength(30); entity.Property(x => x.Note).HasMaxLength(500); ConfigureCreatedAt(entity.Property(x => x.CreatedAt)); entity.HasIndex(x => x.OrderId); entity.HasOne(x => x.Order).WithMany(x => x.StatusHistory).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade); });
        model.Entity<Notification>(entity => { entity.ToTable("notifications"); ConfigureId(entity); entity.Property(x => x.Type).HasMaxLength(100); entity.Property(x => x.Content).HasColumnType("text"); ConfigureCreatedAt(entity.Property(x => x.CreatedAt)); entity.HasIndex(x => x.UserId); });
        model.Entity<OutboxMessage>(entity => { entity.ToTable("outbox_messages"); ConfigureId(entity); entity.Property(x => x.Type).HasMaxLength(100); entity.Property(x => x.Destination).HasMaxLength(254); entity.Property(x => x.Payload).HasColumnType("jsonb"); entity.Property(x => x.Status).HasMaxLength(20); entity.Property(x => x.LastError).HasColumnType("text"); ConfigureCreatedAt(entity.Property(x => x.CreatedAt)); entity.HasIndex(x => new { x.Status, x.NextAttemptAt }); });
        model.Entity<ShippingQuote>(entity => { entity.ToTable("shipping_quotes"); ConfigureId(entity); entity.Property(x => x.OriginPostalCode).HasColumnName("origin_cep").HasMaxLength(8); entity.Property(x => x.DestinationPostalCode).HasColumnName("destination_cep").HasMaxLength(8); entity.Property(x => x.ItemsHash).HasColumnName("request_hash").HasMaxLength(64); entity.Property(x => x.ItemsSnapshot).HasColumnName("items_snapshot").HasColumnType("jsonb"); entity.Property(x => x.Provider).HasMaxLength(30); entity.Property(x => x.Carrier).HasMaxLength(120); entity.Property(x => x.Service).HasMaxLength(120); entity.Property(x => x.DeliveryDays).HasColumnName("estimated_days"); ConfigureCreatedAt(entity.Property(x => x.CreatedAt)); entity.HasIndex(x => x.UserId); entity.HasIndex(x => x.ExpiresAt); });
        model.Entity<StoreShippingSettings>(entity => { entity.ToTable("store_shipping_settings"); entity.HasKey(x => x.Id); entity.Property(x => x.Id).ValueGeneratedNever(); entity.HasCheckConstraint("chk_store_shipping_settings_singleton", "id = 1"); entity.Property(x => x.OriginPostalCode).HasMaxLength(8); entity.Property(x => x.OriginStreet).HasMaxLength(160); entity.Property(x => x.OriginNumber).HasMaxLength(30); entity.Property(x => x.OriginComplement).HasMaxLength(100); entity.Property(x => x.OriginNeighborhood).HasMaxLength(100); entity.Property(x => x.OriginCity).HasMaxLength(100); entity.Property(x => x.OriginState).HasMaxLength(2); ConfigureUpdatedAt(entity.Property(x => x.UpdatedAt)); entity.HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull); });
    }

    private static void ConfigureId<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : class => entity.Property<Guid>("Id").HasDefaultValueSql("gen_random_uuid()");

    private static void ConfigureCreatedAt(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<DateTimeOffset> property) =>
        property.HasDefaultValueSql("NOW()");

    private static void ConfigureUpdatedAt(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<DateTimeOffset> property) =>
        property.HasDefaultValueSql("NOW()");

    private static string OrderStatusToDatabase(OrderStatus value) => value switch
    {
        OrderStatus.PendingPayment => "pending_payment",
        OrderStatus.Paid => "paid",
        OrderStatus.InProduction => "in_production",
        OrderStatus.ReadyToShip => "ready_to_ship",
        OrderStatus.Shipped => "shipped",
        OrderStatus.Delivered => "delivered",
        OrderStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static OrderStatus OrderStatusFromDatabase(string value) => value switch
    {
        "pending_payment" => OrderStatus.PendingPayment,
        "paid" => OrderStatus.Paid,
        "in_production" => OrderStatus.InProduction,
        "ready_to_ship" => OrderStatus.ReadyToShip,
        "shipped" => OrderStatus.Shipped,
        "delivered" => OrderStatus.Delivered,
        "cancelled" => OrderStatus.Cancelled,
        _ => throw new InvalidOperationException($"Status de pedido desconhecido no banco: {value}.")
    };

    private static string PaymentStatusToDatabase(PaymentStatus value) => value.ToString().ToLowerInvariant();
    private static PaymentStatus PaymentStatusFromDatabase(string value) => value.ToLowerInvariant() switch
    {
        "pending" => PaymentStatus.Pending,
        "paid" => PaymentStatus.Paid,
        "failed" => PaymentStatus.Failed,
        "expired" => PaymentStatus.Expired,
        "refunded" => PaymentStatus.Refunded,
        _ => throw new InvalidOperationException($"Status de pagamento desconhecido no banco: {value}.")
    };

    private static string PaymentMethodToDatabase(PaymentMethod value) => value switch
    {
        PaymentMethod.Pix => "pix",
        PaymentMethod.CreditCard => "credit_card",
        _ => "unknown"
    };

    private static PaymentMethod PaymentMethodFromDatabase(string value) => value.ToLowerInvariant() switch
    {
        "pix" => PaymentMethod.Pix,
        "credit_card" => PaymentMethod.CreditCard,
        _ => PaymentMethod.Unknown
    };
}
