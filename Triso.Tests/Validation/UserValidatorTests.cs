using Triso.Application.Users;
using Triso.Application.Validation;
using System.Text.Json;

namespace Triso.Tests.Validation;

public sealed class UserValidatorTests
{
    [Fact]
    public void Accepts_valid_admin() =>
        Assert.Empty(UserValidator.Validate(new UserCreateRequest("Novo Admin", "novo@triso.com", "SenhaSegura@123", 1)));

    [Fact]
    public void Rejects_invalid_create_fields()
    {
        var errors = UserValidator.Validate(new UserCreateRequest("A", "invalido", "curta", 0));
        Assert.Contains("name", errors);
        Assert.Contains("email", errors);
        Assert.Contains("password", errors);
        Assert.Contains("idPermission", errors);
    }

    [Fact]
    public void Rejects_empty_update() =>
        Assert.Contains("request", UserValidator.Validate(new UserUpdateRequest(null, null, null, null, null)));

    [Fact]
    public void Accepts_password_reset_only() =>
        Assert.Empty(UserValidator.Validate(new UserUpdateRequest(null, null, "NovaSenha@123", null, null)));

    [Fact]
    public void Create_request_defaults_active_to_true_when_json_omits_field()
    {
        const string json = """
            {"name":"Novo usuário","email":"novo@triso.com","password":"SenhaSegura@123","idPermission":1}
            """;

        var request = JsonSerializer.Deserialize<UserCreateRequest>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(request);
        Assert.True(request.Active);
    }
}
