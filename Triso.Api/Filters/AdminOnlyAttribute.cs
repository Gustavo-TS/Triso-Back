using Microsoft.AspNetCore.Authorization;

namespace Triso.Api.Filters;

public static class PermissionPolicies
{
    public const string ClaimType = "id_permission";
    public const string Admin = "permission:admin";
    public const string Manager = "permission:manager";
    public const string Dashboard = "permission:dashboard";
}

public sealed class AdminOnlyAttribute : AuthorizeAttribute
{
    public AdminOnlyAttribute() => Policy = PermissionPolicies.Admin;
}

public sealed class ManagerAccessAttribute : AuthorizeAttribute
{
    public ManagerAccessAttribute() => Policy = PermissionPolicies.Manager;
}

public sealed class DashboardAccessAttribute : AuthorizeAttribute
{
    public DashboardAccessAttribute() => Policy = PermissionPolicies.Dashboard;
}
