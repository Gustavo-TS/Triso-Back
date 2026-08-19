using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Triso.Api.Controllers;
using Triso.Api.Filters;

namespace Triso.Tests.Authorization;

public sealed class PermissionPolicyTests
{
    [Fact]
    public void Product_read_allows_dashboard_but_writes_require_manager()
    {
        Assert.Equal(PermissionPolicies.Dashboard, PolicyOn(typeof(ProductsController).GetMethod(nameof(ProductsController.List))!));
        Assert.Equal(PermissionPolicies.Manager, PolicyOn(typeof(ProductsController).GetMethod(nameof(ProductsController.Create))!));
        Assert.Equal(PermissionPolicies.Manager, PolicyOn(typeof(ProductsController).GetMethod(nameof(ProductsController.Update))!));
        Assert.Equal(PermissionPolicies.Manager, PolicyOn(typeof(ProductsController).GetMethod(nameof(ProductsController.Delete))!));
    }

    [Fact]
    public void Dashboard_allows_all_three_admin_profiles() =>
        Assert.Equal(PermissionPolicies.Dashboard, PolicyOn(typeof(AnalyticsController)));

    [Fact]
    public void User_reads_allow_manager_but_writes_require_admin()
    {
        Assert.Equal(PermissionPolicies.Manager, PolicyOn(typeof(UsersController).GetMethod(nameof(UsersController.List))!));
        Assert.Equal(PermissionPolicies.Manager, PolicyOn(typeof(UsersController).GetMethod(nameof(UsersController.Get))!));
        Assert.Equal(PermissionPolicies.Admin, PolicyOn(typeof(UsersController).GetMethod(nameof(UsersController.Create))!));
        Assert.Equal(PermissionPolicies.Admin, PolicyOn(typeof(UsersController).GetMethod(nameof(UsersController.Update))!));
        Assert.Equal(PermissionPolicies.Admin, PolicyOn(typeof(UsersController).GetMethod(nameof(UsersController.Block))!));
    }

    private static string? PolicyOn(MemberInfo member) => member.GetCustomAttribute<AuthorizeAttribute>()?.Policy;
}
