using Triso.Application.Products;
using Triso.Application.Validation;

namespace Triso.Tests.Validation;

public sealed class ProductValidatorTests
{
    [Fact] public void RejectsNegativePrice() { var request = new ProductRequest("Produto", "", -1, null, "draft", Guid.NewGuid(), [], []); Assert.Contains("priceCents", ProductValidator.Validate(request)); }
    [Fact] public void AcceptsValidProduct() { var request = new ProductRequest("Produto", "Descrição", 1000, null, "published", Guid.NewGuid(), [], []); Assert.Empty(ProductValidator.Validate(request)); }

    [Fact]
    public void RejectsInvalidMarketplaceUrl()
    {
        var links = new List<MarketplaceLinkRequest> { new(Guid.NewGuid(), "http://example.com/anuncio", null) };
        var request = new ProductRequest("Produto", "Descrição", 1000, null, "published", Guid.NewGuid(), [], links);
        Assert.Contains("marketplaceLinks", ProductValidator.Validate(request));
    }

    [Fact]
    public void RejectsDuplicateMarketplace()
    {
        var marketplaceId = Guid.NewGuid();
        var links = new List<MarketplaceLinkRequest>
        {
            new(marketplaceId, "https://example.com/um", null),
            new(marketplaceId, "https://example.com/dois", null)
        };
        var request = new ProductRequest("Produto", "Descrição", 1000, null, "published", Guid.NewGuid(), [], links);
        Assert.Contains("marketplaceLinks", ProductValidator.Validate(request));
    }
}
