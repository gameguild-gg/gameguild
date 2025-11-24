using FluentAssertions;
using GameGuild.API.Authorization;
using GameGuild.Permissions.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using Xunit;

namespace GameGuild.Tests.API.Unit.Authorization;

public class PermissionAuthorizationFilterTests
{
    private readonly Mock<IPermissionsContext> _mockPermissions;
    private readonly Mock<ILogger<PermissionAuthorizationFilter>> _mockLogger;
    private readonly PermissionAuthorizationFilter _filter;
    private readonly DefaultHttpContext _httpContext;

    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testTenantId = Guid.NewGuid();

    public PermissionAuthorizationFilterTests()
    {
        _mockPermissions = new Mock<IPermissionsContext>();
        _mockLogger = new Mock<ILogger<PermissionAuthorizationFilter>>();
        _filter = new PermissionAuthorizationFilter(_mockPermissions.Object, _mockLogger.Object);
        _httpContext = new DefaultHttpContext();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Permissions_Is_Null()
    {
        var act = () => new PermissionAuthorizationFilter(null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("permissions");
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        var act = () => new PermissionAuthorizationFilter(_mockPermissions.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region AllowAnonymous Tests

    [Fact]
    public async Task OnAuthorizationAsync_Should_Allow_Access_When_Endpoint_Has_AllowAnonymous_Metadata()
    {
        var endpoint = new Endpoint(
            requestDelegate: context => Task.CompletedTask,
            metadata: new EndpointMetadataCollection(new AllowAnonymousAttribute()),
            displayName: "Test"
        );
        _httpContext.SetEndpoint(endpoint);
        
        var context = CreateAuthorizationContext(_httpContext);
        
        await _filter.OnAuthorizationAsync(context);
        
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task OnAuthorizationAsync_Should_Allow_Access_When_Action_Has_AllowAnonymous()
    {
        var methodInfo = GetType().GetMethod(nameof(ActionWithAllowAnonymous))!;
        var context = CreateAuthorizationContext(_httpContext, methodInfo);
        
        await _filter.OnAuthorizationAsync(context);
        
        context.Result.Should().BeNull();
    }

    [AllowAnonymous]
    public void ActionWithAllowAnonymous() { }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task OnAuthorizationAsync_Should_Return_Challenge_When_User_Not_Authenticated()
    {
        _mockPermissions.Setup(x => x.IsAuthenticated).Returns(false);
        _mockPermissions.Setup(x => x.UserId).Returns(_testUserId);
        
        var context = CreateAuthorizationContext(_httpContext);
        
        await _filter.OnAuthorizationAsync(context);
        
        context.Result.Should().BeOfType<ChallengeResult>();
    }

    [Fact]
    public async Task OnAuthorizationAsync_Should_Allow_When_Authenticated_And_No_Permissions_Required()
    {
        _mockPermissions.Setup(x => x.IsAuthenticated).Returns(true);
        
        var context = CreateAuthorizationContext(_httpContext);
        
        await _filter.OnAuthorizationAsync(context);
        
        context.Result.Should().BeNull();
    }

    #endregion

    #region SystemAdmin Tests

    [Fact]
    public async Task OnAuthorizationAsync_Should_Allow_SystemAdmin_Without_Permission_Check()
    {
        _mockPermissions.Setup(x => x.IsAuthenticated).Returns(true);
        _mockPermissions.Setup(x => x.IsSystemAdmin).Returns(true);
        _mockPermissions.Setup(x => x.UserId).Returns(_testUserId);
        
        int callCount = 0;
        _mockPermissions.Setup(x => x.HasTenantPermissionAsync(It.IsAny<string>()))
            .Callback(() => callCount++)
            .ReturnsAsync(true);
        
        var methodInfo = GetType().GetMethod(nameof(ActionWithPermission))!;
        var context = CreateAuthorizationContext(_httpContext, methodInfo);
        
        await _filter.OnAuthorizationAsync(context);
        
        context.Result.Should().BeNull();
        callCount.Should().Be(0);
    }

    #endregion

    #region Permission Validation Tests

    [RequiresPermission("users.read")]
    public void ActionWithPermission() { }

    [Fact]
    public async Task OnAuthorizationAsync_Should_Check_Required_Permission()
    {
        _mockPermissions.Setup(x => x.IsAuthenticated).Returns(true);
        _mockPermissions.Setup(x => x.IsSystemAdmin).Returns(false);
        _mockPermissions.Setup(x => x.UserId).Returns(_testUserId);
        _mockPermissions.Setup(x => x.TenantId).Returns(_testTenantId);
        
        string? capturedPermission = null;
        _mockPermissions.Setup(x => x.HasTenantPermissionAsync(It.IsAny<string>()))
            .Callback<string>(p => capturedPermission = p)
            .ReturnsAsync(true);
        
        var methodInfo = GetType().GetMethod(nameof(ActionWithPermission))!;
        var context = CreateAuthorizationContext(_httpContext, methodInfo);
        
        await _filter.OnAuthorizationAsync(context);
        
        context.Result.Should().BeNull();
        capturedPermission.Should().Be("users.read");
    }

    [Fact]
    public async Task OnAuthorizationAsync_Should_Return_Forbid_When_Permission_Missing()
    {
        _mockPermissions.Setup(x => x.IsAuthenticated).Returns(true);
        _mockPermissions.Setup(x => x.IsSystemAdmin).Returns(false);
        _mockPermissions.Setup(x => x.UserId).Returns(_testUserId);
        _mockPermissions.Setup(x => x.TenantId).Returns(_testTenantId);
        _mockPermissions
            .Setup(x => x.HasTenantPermissionAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        
        var methodInfo = GetType().GetMethod(nameof(ActionWithPermission))!;
        var context = CreateAuthorizationContext(_httpContext, methodInfo);
        
        await _filter.OnAuthorizationAsync(context);
        
        context.Result.Should().BeOfType<ForbidResult>();
    }

    [RequiresPermission("users.read")]
    [RequiresPermission("users.write")]
    public void ActionWithMultiplePermissions() { }

    [Fact]
    public async Task OnAuthorizationAsync_Should_Check_All_Required_Permissions()
    {
        _mockPermissions.Setup(x => x.IsAuthenticated).Returns(true);
        _mockPermissions.Setup(x => x.IsSystemAdmin).Returns(false);
        _mockPermissions.Setup(x => x.UserId).Returns(_testUserId);
        _mockPermissions.Setup(x => x.TenantId).Returns(_testTenantId);
        
        var capturedPermissions = new List<string>();
        _mockPermissions.Setup(x => x.HasTenantPermissionAsync(It.IsAny<string>()))
            .Callback<string>(p => capturedPermissions.Add(p))
            .ReturnsAsync(true);
        
        var methodInfo = GetType().GetMethod(nameof(ActionWithMultiplePermissions))!;
        var context = CreateAuthorizationContext(_httpContext, methodInfo);
        
        await _filter.OnAuthorizationAsync(context);
        
        context.Result.Should().BeNull();
        capturedPermissions.Should().Contain("users.read");
        capturedPermissions.Should().Contain("users.write");
    }

    [Fact]
    public async Task OnAuthorizationAsync_Should_Return_Forbid_When_Any_Permission_Missing()
    {
        _mockPermissions.Setup(x => x.IsAuthenticated).Returns(true);
        _mockPermissions.Setup(x => x.IsSystemAdmin).Returns(false);
        _mockPermissions.Setup(x => x.UserId).Returns(_testUserId);
        _mockPermissions.Setup(x => x.TenantId).Returns(_testTenantId);
        
        var callCount = 0;
        _mockPermissions.Setup(x => x.HasTenantPermissionAsync(It.IsAny<string>()))
            .Callback(() => callCount++)
            .ReturnsAsync((string p) => p == "users.read");
        
        var methodInfo = GetType().GetMethod(nameof(ActionWithMultiplePermissions))!;
        var context = CreateAuthorizationContext(_httpContext, methodInfo);
        
        await _filter.OnAuthorizationAsync(context);
        
        context.Result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region Controller-Level Permissions Tests

    [RequiresPermission("controller.access")]
    public class TestControllerWithPermission
    {
        [RequiresPermission("action.access")]
        public void ActionMethod() { }
    }

    [Fact]
    public async Task OnAuthorizationAsync_Should_Check_Both_Controller_And_Action_Permissions()
    {
        _mockPermissions.Setup(x => x.IsAuthenticated).Returns(true);
        _mockPermissions.Setup(x => x.IsSystemAdmin).Returns(false);
        _mockPermissions.Setup(x => x.UserId).Returns(_testUserId);
        _mockPermissions.Setup(x => x.TenantId).Returns(_testTenantId);
        
        var capturedPermissions = new List<string>();
        _mockPermissions.Setup(x => x.HasTenantPermissionAsync(It.IsAny<string>()))
            .Callback<string>(p => capturedPermissions.Add(p))
            .ReturnsAsync(true);
        
        var methodInfo = typeof(TestControllerWithPermission).GetMethod(nameof(TestControllerWithPermission.ActionMethod))!;
        var controllerTypeInfo = typeof(TestControllerWithPermission).GetTypeInfo();
        var actionDescriptor = new ControllerActionDescriptor
        {
            MethodInfo = methodInfo,
            ControllerTypeInfo = controllerTypeInfo,
            ControllerName = "TestControllerWithPermission",
            ActionName = "ActionMethod"
        };
        
        var actionContext = new ActionContext(_httpContext, new RouteData(), actionDescriptor);
        var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        
        await _filter.OnAuthorizationAsync(context);
        
        context.Result.Should().BeNull();
        capturedPermissions.Should().Contain("controller.access");
        capturedPermissions.Should().Contain("action.access");
    }

    #endregion

    #region Endpoint Metadata Tests

    [Fact]
    public async Task OnAuthorizationAsync_Should_Read_Permissions_From_Endpoint_Metadata()
    {
        _mockPermissions.Setup(x => x.IsAuthenticated).Returns(true);
        _mockPermissions.Setup(x => x.IsSystemAdmin).Returns(false);
        _mockPermissions.Setup(x => x.UserId).Returns(_testUserId);
        _mockPermissions.Setup(x => x.TenantId).Returns(_testTenantId);
        
        string? capturedPermission = null;
        _mockPermissions.Setup(x => x.HasTenantPermissionAsync(It.IsAny<string>()))
            .Callback<string>(p => capturedPermission = p)
            .ReturnsAsync(true);
        
        var attribute = new RequiresPermissionAttribute("endpoint.access");
        var endpoint = new Endpoint(
            requestDelegate: context => Task.CompletedTask,
            metadata: new EndpointMetadataCollection(attribute),
            displayName: "Test"
        );
        _httpContext.SetEndpoint(endpoint);
        
        var context = CreateAuthorizationContext(_httpContext);
        
        await _filter.OnAuthorizationAsync(context);
        
        context.Result.Should().BeNull();
        capturedPermission.Should().Be("endpoint.access");
    }

    #endregion

    #region Helper Methods

    private AuthorizationFilterContext CreateAuthorizationContext(
        HttpContext httpContext,
        MethodInfo? methodInfo = null)
    {
        ActionDescriptor actionDescriptor;
        
        if (methodInfo != null)
        {
            actionDescriptor = new ControllerActionDescriptor
            {
                MethodInfo = methodInfo,
                ControllerTypeInfo = GetType().GetTypeInfo(),
                ControllerName = GetType().Name,
                ActionName = methodInfo.Name
            };
        }
        else
        {
            actionDescriptor = new ActionDescriptor();
        }
        
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    #endregion
}
