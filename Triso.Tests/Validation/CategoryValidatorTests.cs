using Triso.Application.Categories;
using Triso.Application.Validation;

namespace Triso.Tests.Validation;

public sealed class CategoryValidatorTests
{
    [Theory]
    [InlineData("Eletrônicos")]
    [InlineData("Casa e cozinha")]
    public void Valid_name_has_no_errors(string name) =>
        Assert.Empty(CategoryValidator.Validate(new CategoryRequest(name)));

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Invalid_name_returns_name_error(string name) =>
        Assert.Contains("name", CategoryValidator.Validate(new CategoryRequest(name)));

    [Fact]
    public void Empty_update_returns_request_error() =>
        Assert.Contains("request", CategoryValidator.Validate(new CategoryUpdateRequest(null, null)));

    [Fact]
    public void Update_accepts_individual_fields()
    {
        Assert.Empty(CategoryValidator.Validate(new CategoryUpdateRequest("Nova categoria", null)));
        Assert.Empty(CategoryValidator.Validate(new CategoryUpdateRequest(null, false)));
    }
}
