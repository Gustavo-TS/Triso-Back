using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Triso.Domain.Entities;
using Triso.Infrastructure.Persistence;

namespace Triso.Tests.Persistence;

public sealed class SchemaContractTests
{
    private static readonly IModel Model = CreateModel();

    [Fact]
    public void Maps_all_tables_from_the_infrastructure_contract()
    {
        var tables = Model.GetEntityTypes().Select(x => x.GetTableName()).OfType<string>().ToHashSet();
        var requiredTables = new HashSet<string>
        {
            "users", "permission", "categories", "products", "product_images", "marketplaces",
            "product_marketplace_links", "marketplace_clicks", "sessions", "audit_logs"
        };

        Assert.Subset(requiredTables, tables);
    }

    [Fact]
    public void User_uses_active_and_permission_without_legacy_role_or_status()
    {
        var user = Model.FindEntityType(typeof(User))!;
        var columns = user.GetProperties().Select(x => x.GetColumnName()).ToHashSet();

        Assert.Contains("active", columns);
        Assert.Contains("updated_at", columns);
        Assert.Contains("id_permission", columns);
        Assert.DoesNotContain("status", columns);
        Assert.DoesNotContain("role", columns);
    }

    [Fact]
    public void Restrictive_relationships_match_the_database_contract()
    {
        Assert.Equal(DeleteBehavior.Restrict, ForeignKey<Product, Category>().DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, ForeignKey<ProductMarketplaceLink, Marketplace>().DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, ForeignKey<MarketplaceClick, ProductMarketplaceLink>().DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, ForeignKey<User, Permission>().DeleteBehavior);
    }

    [Fact]
    public void Product_marketplace_pair_is_unique()
    {
        var link = Model.FindEntityType(typeof(ProductMarketplaceLink))!;
        var index = link.GetIndexes().Single(x => x.Properties.Select(p => p.Name).SequenceEqual(["ProductId", "MarketplaceId"]));

        Assert.True(index.IsUnique);
    }

    private static IForeignKey ForeignKey<TDependent, TPrincipal>() where TDependent : class where TPrincipal : class =>
        Model.FindEntityType(typeof(TDependent))!.GetForeignKeys().Single(x => x.PrincipalEntityType.ClrType == typeof(TPrincipal));

    private static IModel CreateModel()
    {
        var options = new DbContextOptionsBuilder<TrisoDbContext>()
            .UseNpgsql("Host=localhost;Database=triso_contract;Username=contract;Password=contract")
            .UseSnakeCaseNamingConvention()
            .Options;
        using var context = new TrisoDbContext(options);
        return context.Model;
    }
}
