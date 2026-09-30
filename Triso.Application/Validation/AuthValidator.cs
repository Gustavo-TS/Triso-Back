using System.Net.Mail;
using Triso.Application.Auth;
using Triso.Application.Customers;

namespace Triso.Application.Validation;

public static class AuthValidator
{
    public static Dictionary<string, string[]> Validate(RegisterCustomerRequest value) => Validate(value.Name, value.Email, value.Password);
    public static Dictionary<string, string[]> Validate(BootstrapAdminRequest value)
        => Validate(value.Name, value.Email, value.Password);

    private static Dictionary<string, string[]> Validate(string? rawName, string? rawEmail, string? password)
    {
        var errors = new Dictionary<string, string[]>();
        var name = rawName?.Trim();
        var email = rawEmail?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length is < 2 or > 120)
            errors["name"] = ["Informe um nome entre 2 e 120 caracteres."];

        if (string.IsNullOrWhiteSpace(email) || email.Length > 254 ||
            !MailAddress.TryCreate(email, out var parsedEmail) ||
            !parsedEmail.Address.Equals(email, StringComparison.OrdinalIgnoreCase))
            errors["email"] = ["Informe um e-mail válido."];

        if (string.IsNullOrWhiteSpace(password) || password.Length is < 12 or > 128)
            errors["password"] = ["Informe uma senha entre 12 e 128 caracteres."];

        return errors;
    }
}
