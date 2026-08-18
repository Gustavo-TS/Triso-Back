using Triso.Application.Products;
using Triso.Application.Validation;

namespace Triso.Tests.Validation;

public sealed class ProductValidatorTests
{
    [Fact] public void RejectsNegativePrice() { var request = new ProductRequest("Produto", "", -1, null, "draft", Guid.NewGuid(), [], []); Assert.Contains("priceCents", ProductValidator.Validate(request)); }
    [Fact] public void AcceptsValidProduct() { var request = new ProductRequest("Produto", "Descrição", 1000, null, "published", Guid.NewGuid(), [], []); Assert.Empty(ProductValidator.Validate(request)); }
}
