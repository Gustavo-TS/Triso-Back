using System.Net.Mail;
using Triso.Application.Users;

namespace Triso.Application.Validation;

public static class UserValidator
{
    public static Dictionary<string, string[]> Validate(UserCreateRequest value)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateName(value.Name, errors);
        ValidateEmail(value.Email, errors);
        ValidatePassword(value.Password, errors);
        ValidatePermission(value.IdPermission, errors);
        return errors;
    }

    public static Dictionary<string, string[]> Validate(UserUpdateRequest value)
    {
        var errors = new Dictionary<string, string[]>();
        if (value.Name is null && value.Email is null && value.Password is null && value.IdPermission is null && value.Active is null)
        {
            errors["request"] = ["Informe ao menos um campo para atualizar."];
            return errors;
        }
        if (value.Name is not null) ValidateName(value.Name, errors);
        if (value.Email is not null) ValidateEmail(value.Email, errors);
        if (value.Password is not null) ValidatePassword(value.Password, errors);
        if (value.IdPermission is not null) ValidatePermission(value.IdPermission.Value, errors);
        return errors;
    }

    private static void ValidateName(string? value, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length is < 2 or > 120)
            errors["name"] = ["Informe um nome entre 2 e 120 caracteres."];
    }

    private static void ValidateEmail(string? value, Dictionary<string, string[]> errors)
    {
        var email = value?.Trim();
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254 || !MailAddress.TryCreate(email, out var parsed) || !parsed.Address.Equals(email, StringComparison.OrdinalIgnoreCase))
            errors["email"] = ["Informe um e-mail válido."];
    }

    private static void ValidatePassword(string? value, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length is < 12 or > 128)
            errors["password"] = ["Informe uma senha entre 12 e 128 caracteres."];
    }

    private static void ValidatePermission(int value, Dictionary<string, string[]> errors)
    {
        if (value <= 0) errors["idPermission"] = ["Informe uma permissão válida."];
    }

}
