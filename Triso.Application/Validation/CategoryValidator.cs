using Triso.Application.Categories;

namespace Triso.Application.Validation;

public static class CategoryValidator
{
    public static Dictionary<string, string[]> Validate(CategoryRequest value)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(value.Name) || value.Name.Trim().Length is < 2 or > 120)
            errors["name"] = ["Informe um nome entre 2 e 120 caracteres."];
        return errors;
    }

    public static Dictionary<string, string[]> Validate(CategoryUpdateRequest value)
    {
        var errors = new Dictionary<string, string[]>();
        if (value.Name is null && value.Active is null)
            errors["request"] = ["Informe ao menos um campo para atualizar."];
        else if (value.Name is not null && (string.IsNullOrWhiteSpace(value.Name) || value.Name.Trim().Length is < 2 or > 120))
            errors["name"] = ["Informe um nome entre 2 e 120 caracteres."];
        return errors;
    }
}
