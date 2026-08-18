using Triso.Application.Marketplaces;
using Triso.Application.Validation;

namespace Triso.Tests.Validation;

public sealed class MarketplaceValidatorTests
{
    [Theory]
    [InlineData("Mercado Livre")]
    [InlineData("Amazon")]
    public void Valid_marketplace_has_no_errors(string name) =>
        Assert.Empty(MarketplaceValidator.Validate(new MarketplaceRequest(name)));

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Invalid_name_returns_name_error(string name) =>
        Assert.Contains("name", MarketplaceValidator.Validate(new MarketplaceRequest(name)));

    [Fact]
    public void Empty_update_returns_request_error() =>
        Assert.Contains("request", MarketplaceValidator.Validate(new MarketplaceUpdateRequest(null, null)));

    [Fact]
    public void Update_accepts_individual_fields()
    {
        Assert.Empty(MarketplaceValidator.Validate(new MarketplaceUpdateRequest("Novo nome", null)));
        Assert.Empty(MarketplaceValidator.Validate(new MarketplaceUpdateRequest(null, false)));
    }
}
