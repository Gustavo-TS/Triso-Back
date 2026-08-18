using Microsoft.EntityFrameworkCore;
using Triso.Domain.Entities;

namespace Triso.Infrastructure.Persistence;

public sealed class TrisoDbContext(DbContextOptions<TrisoDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Marketplace> Marketplaces => Set<Marketplace>();
    public DbSet<ProductMarketplaceLink> ProductMarketplaceLinks => Set<ProductMarketplaceLink>();
    public DbSet<MarketplaceClick> MarketplaceClicks => Set<MarketplaceClick>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasDefaultSchema("public");

        model.Entity<User>(entity =>
        {
            entity.ToTable("users", table =>
            {
                table.HasCheckConstraint("chk_users_role", "role IN ('admin', 'customer')");
                table.HasCheckConstraint("chk_users_status", "status IN ('active', 'blocked')");
            });
            ConfigureId(entity);
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.Role).HasMaxLength(20).HasDefaultValue("customer");
            entity.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("active");
            ConfigureCreatedAt(entity.Property(x => x.CreatedAt));
            ConfigureUpdatedAt(entity.Property(x => x.UpdatedAt));
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
    }

    private static void ConfigureId<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : class => entity.Property<Guid>("Id").HasDefaultValueSql("gen_random_uuid()");

    private static void ConfigureCreatedAt(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<DateTimeOffset> property) =>
        property.HasDefaultValueSql("NOW()");

    private static void ConfigureUpdatedAt(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<DateTimeOffset> property) =>
        property.HasDefaultValueSql("NOW()");
}
