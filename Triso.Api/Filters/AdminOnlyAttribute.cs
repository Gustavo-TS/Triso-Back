using Microsoft.AspNetCore.Authorization;

namespace Triso.Api.Filters;

public sealed class AdminOnlyAttribute : AuthorizeAttribute
{
    public AdminOnlyAttribute() => Roles = "admin";
}
