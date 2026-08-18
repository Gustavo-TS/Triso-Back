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
        else if (value.MarketplaceLinks.GroupBy(x => x.MarketplaceId).Any(x => x.Count() > 1))
            errors["marketplaceLinks"] = ["Informe apenas um link por marketplace."];
        else if (value.MarketplaceLinks.Any(x => x.MarketplaceId == Guid.Empty))
            errors["marketplaceLinks"] = ["Marketplace inválido."];
        else if (value.MarketplaceLinks.Any(x => !IsHttpsUrl(x.Url)))
            errors["marketplaceLinks"] = ["Todos os links devem possuir uma URL HTTPS válida com até 2.048 caracteres."];
        else if (value.MarketplaceLinks.Any(x => x.ExternalProductId?.Trim().Length > 120))
            errors["marketplaceLinks"] = ["O identificador externo deve possuir no máximo 120 caracteres."];
        return errors;
    }

    private static bool IsHttpsUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 2048 &&
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}
