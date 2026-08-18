using Triso.Application.Auth;
using Triso.Application.Validation;

namespace Triso.Tests.Validation;

public sealed class AuthValidatorTests
{
    [Fact]
    public void Valid_bootstrap_request_has_no_errors()
    {
        var request = new BootstrapAdminRequest("Administrador", "admin@triso.com", "senha-segura-123");

        Assert.Empty(AuthValidator.Validate(request));
    }

    [Fact]
    public void Invalid_bootstrap_request_reports_each_field()
    {
        var request = new BootstrapAdminRequest("A", "email inválido", "curta");

        var errors = AuthValidator.Validate(request);

        Assert.Contains("name", errors);
        Assert.Contains("email", errors);
        Assert.Contains("password", errors);
    }
}
