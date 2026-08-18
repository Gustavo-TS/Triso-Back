using Triso.Application.Products;

namespace Triso.Application.Validation;

public static class ProductValidator
{
    public static Dictionary<string, string[]> Validate(ProductRequest value)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(value.Name) || value.Name.Trim().Length is < 2 or > 120) errors["name"] = ["Informe um nome entre 2 e 120 caracteres."];
        if (value.Description.Length > 2000) errors["description"] = ["Máximo de 2.000 caracteres."];
        if (value.PriceCents < 0) errors["priceCents"] = ["O preço não pode ser negativo."];
        if (value.CategoryId == Guid.Empty) errors["categoryId"] = ["Categoria inválida."];
        if (value.Status is not ("draft" or "published" or "archived")) errors["status"] = ["Status inválido."];
        if (value.Images.Count > 10) errors["images"] = ["Máximo de 10 imagens."];
        if (value.MarketplaceLinks.Count > 10) errors["marketplaceLinks"] = ["Máximo de 10 links."];
        return errors;
    }
}
