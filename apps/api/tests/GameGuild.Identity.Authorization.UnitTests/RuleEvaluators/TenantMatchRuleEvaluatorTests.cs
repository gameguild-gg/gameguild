using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.RuleEvaluators;

public class TenantMatchRuleEvaluatorTests
{
    private readonly Mock<IAuthorizationTenantContext> _mockTenantContext;
    private readonly TenantMatchRuleEvaluator _evaluator;

    public TenantMatchRuleEvaluatorTests()
    {
        _mockTenantContext = new Mock<IAuthorizationTenantContext>();
        _evaluator = new TenantMatchRuleEvaluator(_mockTenantContext.Object);
    }

    [Fact]
    public void RuleType_ReturnsTenantMatch()
    {
        // Assert
        _evaluator.RuleType.Should().Be(RuleTypes.TenantMatch);
    }

    [Fact]
    public async Task EvaluateAsync_UserNotAuthenticated_ReturnsFail()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated
        var context = new AuthorizationHandlerContext([], user, null);
        var parameters = RuleParameters.FromJson("{}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("not authenticated");
    }

    [Fact]
    public async Task EvaluateAsync_UserHasNoTenantClaim_ReturnsFail()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity("TestAuth"));
        var context = new AuthorizationHandlerContext([], user, null);
        var parameters = RuleParameters.FromJson("{}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("no tenant claim");
    }

    [Fact]
    public async Task EvaluateAsync_NoTenantContext_DoesNotAllowNoTenant_ReturnsFail()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var claims = new List<Claim> { new(ClaimNames.TenantId, tenantId.ToString()) };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var context = new AuthorizationHandlerContext([], user, null);
        var parameters = RuleParameters.FromJson("{\"allowNoTenant\": false}");

        _mockTenantContext.Setup(x => x.TenantId).Returns((Guid?)null);

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("No tenant context available");
    }
    
    [Fact]
    public async Task EvaluateAsync_NoTenantContext_AllowNoTenant_ReturnsSuccess()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var claims = new List<Claim> { new(ClaimNames.TenantId, tenantId.ToString()) };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var context = new AuthorizationHandlerContext([], user, null);
        var parameters = RuleParameters.FromJson("{\"allowNoTenant\": true}");

        _mockTenantContext.Setup(x => x.TenantId).Returns((Guid?)null);

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_TenantMismatch_ReturnsFail()
    {
        // Arrange
        var userTenantId = Guid.NewGuid();
        var requestTenantId = Guid.NewGuid();
        
        var claims = new List<Claim> { new(ClaimNames.TenantId, userTenantId.ToString()) };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var context = new AuthorizationHandlerContext([], user, null);
        var parameters = RuleParameters.FromJson("{}");

        _mockTenantContext.Setup(x => x.TenantId).Returns(requestTenantId);

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("Tenant mismatch");
    }

    [Fact]
    public async Task EvaluateAsync_SystemAdministratorCrossTenant_ReturnsSuccess()
    {
        var userTenantId = Guid.NewGuid();
        var requestTenantId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimNames.TenantId, userTenantId.ToString()),
            new(ClaimTypes.Role, Policies.SystemAdmin)
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var context = new AuthorizationHandlerContext([], user, null);
        var parameters = RuleParameters.FromJson("{}");

        _mockTenantContext.Setup(x => x.TenantId).Returns(requestTenantId);

        var result = await _evaluator.EvaluateAsync(context, parameters);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_TenantMatch_ReturnsSuccess()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        
        var claims = new List<Claim> { new(ClaimNames.TenantId, tenantId.ToString()) };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var context = new AuthorizationHandlerContext([], user, null);
        var parameters = RuleParameters.FromJson("{}");

        _mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
    
    [Fact]
    public async Task EvaluateAsync_TenantMatch_CaseInsensitive_ReturnsSuccess()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        
        var claims = new List<Claim> { new(ClaimNames.TenantId, tenantId.ToString().ToUpper()) };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var context = new AuthorizationHandlerContext([], user, null);
        var parameters = RuleParameters.FromJson("{}");

        _mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
