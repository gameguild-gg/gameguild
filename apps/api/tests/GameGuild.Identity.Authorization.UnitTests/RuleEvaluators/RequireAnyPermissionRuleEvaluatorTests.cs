using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.RuleEvaluators;

public class RequireAnyPermissionRuleEvaluatorTests
{
    private readonly Mock<IAuthorizationPermissionService> _mockPermissionService;
    private readonly Mock<IAuthorizationTenantContext> _mockTenantContext;
    private readonly RequireAnyPermissionRuleEvaluator _evaluator;

    public RequireAnyPermissionRuleEvaluatorTests()
    {
        _mockPermissionService = new Mock<IAuthorizationPermissionService>();
        _mockTenantContext = new Mock<IAuthorizationTenantContext>();
        _evaluator = new RequireAnyPermissionRuleEvaluator(_mockPermissionService.Object, _mockTenantContext.Object);
    }

    [Fact]
    public void RuleType_ReturnsRequireAnyPermission()
    {
        // Assert
        _evaluator.RuleType.Should().Be(RuleTypes.RequireAnyPermission);
    }

    [Fact]
    public async Task EvaluateAsync_WithNoPermissionsParameter_ReturnsFail()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity("TestAuth"));
        var context = new AuthorizationHandlerContext([], user, null);
        var parameters = RuleParameters.FromJson("{}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }
    
    [Fact]
    public async Task EvaluateAsync_WithEmptyPermissionsParameter_ReturnsFail()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity("TestAuth"));
        var context = new AuthorizationHandlerContext([], user, null);
        var parameters = RuleParameters.FromJson("{\"permissions\": []}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_UserNotAuthenticated_ReturnsFail()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated
        var context = new AuthorizationHandlerContext([], user, null);
        var parameters = RuleParameters.FromJson("{\"permissions\": [\"read\"]}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("not authenticated");
    }

    [Fact]
    public async Task EvaluateAsync_UserAuthenticated_NoUserId_ReturnsFail()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity("TestAuth"));
        var context = new AuthorizationHandlerContext([], user, null);
        var parameters = RuleParameters.FromJson("{\"permissions\": [\"read\"]}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("Could not determine user ID");
    }

    [Fact]
    public async Task EvaluateAsync_UserAuthenticated_NoTenantId_ReturnsFail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var context = new AuthorizationHandlerContext([], user, null);
        var parameters = RuleParameters.FromJson("{\"permissions\": [\"read\"]}");

        _mockTenantContext.Setup(x => x.TenantId).Returns((Guid?)null);

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("Could not determine tenant ID");
    }

    [Fact]
    public async Task EvaluateAsync_SystemAdministrator_ReturnsSuccessWithoutPermissionLookup()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, Policies.SystemAdmin)
        ], "TestAuth"));
        var context = new AuthorizationHandlerContext([], user, null);
        var parameters = RuleParameters.FromJson("{\"permissions\": [\"users:admin\"]}");

        var result = await _evaluator.EvaluateAsync(context, parameters);

        result.IsSuccess.Should().BeTrue();
        _mockPermissionService.Verify(
            x => x.HasAnyPermissionAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_HasAnyPermission_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimNames.TenantId, tenantId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var context = new AuthorizationHandlerContext([], user, null);
        var parameters = RuleParameters.FromJson("{\"permissions\": [\"read\", \"write\"]}");
        var permissions = new[] { "read", "write" };

        _mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        
        _mockPermissionService
            .Setup(x => x.HasAnyPermissionAsync(
                userId, 
                tenantId, 
                It.Is<IEnumerable<string>>(p => p.SequenceEqual(permissions)), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PermissionCheckResult.Partial(["read"], ["write"]));

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_HasNoneOfPermissions_ReturnsFail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimNames.TenantId, tenantId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var context = new AuthorizationHandlerContext([], user, null);
        var parameters = RuleParameters.FromJson("{\"permissions\": [\"read\", \"write\"]}");
        var permissions = new[] { "read", "write" };

        _mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        
        _mockPermissionService
            .Setup(x => x.HasAnyPermissionAsync(
                userId, 
                tenantId, 
                It.Is<IEnumerable<string>>(p => p.SequenceEqual(permissions)), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PermissionCheckResult.NonePresent(permissions));

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("None of the required permissions found");
    }
}
