using Triso.Application.Marketplaces;

namespace Triso.Application.Validation;

public static class MarketplaceValidator
{
    public static Dictionary<string, string[]> Validate(MarketplaceRequest value)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateName(value.Name, errors);
        return errors;
    }

    public static Dictionary<string, string[]> Validate(MarketplaceUpdateRequest value)
    {
        var errors = new Dictionary<string, string[]>();
        if (value.Name is null && value.Active is null)
        {
            errors["request"] = ["Informe ao menos um campo para atualizar."];
            return errors;
        }

        if (value.Name is not null) ValidateName(value.Name, errors);
        return errors;
    }

    private static void ValidateName(string? name, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length is < 2 or > 100)
            errors["name"] = ["Informe um nome entre 2 e 100 caracteres."];
    }
}
