using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.RuleEvaluators;

public class SelfOrPermissionRuleEvaluatorTests
{
    private readonly Mock<IAuthorizationPermissionService> _permissionServiceMock;
    private readonly Mock<IAuthorizationTenantContext> _tenantContextMock;
    private readonly Mock<ITenantMembershipChecker> _tenantMembershipCheckerMock;
    private readonly SelfOrPermissionRuleEvaluator _evaluator;

    public SelfOrPermissionRuleEvaluatorTests()
    {
        _permissionServiceMock = new Mock<IAuthorizationPermissionService>();
        _tenantContextMock = new Mock<IAuthorizationTenantContext>();
        _tenantMembershipCheckerMock = new Mock<ITenantMembershipChecker>();
        _evaluator = new SelfOrPermissionRuleEvaluator(
            _permissionServiceMock.Object,
            _tenantContextMock.Object,
            _tenantMembershipCheckerMock.Object);
    }

    [Fact]
    public void RuleType_ReturnsSelfOrPermission()
    {
        _evaluator.RuleType.Should().Be(RuleTypes.SelfOrPermission);
    }

    [Fact]
    public async Task EvaluateAsync_UnauthenticatedUser_ReturnsFail()
    {
        var context = new AuthorizationHandlerContext([], new ClaimsPrincipal(new ClaimsIdentity()), null);
        var parameters = RuleParameters.FromJson("{}");

        var result = await _evaluator.EvaluateAsync(context, parameters);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("authenticated");
    }

    [Fact]
    public async Task EvaluateAsync_TargetMemberAndHasAnyPermission_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", userId.ToString()),
            new Claim("tid", tenantId.ToString())
        ], "test"));
        
        _tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);
        _tenantContextMock.Setup(x => x.HasTenant).Returns(true);
        
        var targetUserId = Guid.NewGuid();
        _tenantMembershipCheckerMock
            .Setup(x => x.IsUserMemberOfTenantAsync(targetUserId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _permissionServiceMock.Setup(x => x.HasPermissionAsync(userId, tenantId, "users:manage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var context = new AuthorizationHandlerContext([], user, new TestUserResource { UserId = targetUserId });
        var parameters = RuleParameters.FromJson("{\"anyPermission\":\"users:manage\"}");

        var result = await _evaluator.EvaluateAsync(context, parameters);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_SystemAdministratorTargetingAnotherUser_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", userId.ToString()),
            new Claim("tid", tenantId.ToString()),
            new Claim(ClaimTypes.Role, "SystemAdmin")
        ], "test"));

        _tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);
        _tenantContextMock.Setup(x => x.HasTenant).Returns(true);

        var context = new AuthorizationHandlerContext(
            [],
            user,
            new TestUserResource { UserId = targetUserId });
        var parameters = RuleParameters.FromJson("{\"anyPermission\":\"users:manage\"}");

        var result = await _evaluator.EvaluateAsync(context, parameters);

        result.IsSuccess.Should().BeTrue();
        _tenantMembershipCheckerMock.Verify(
            x => x.IsUserMemberOfTenantAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _permissionServiceMock.Verify(
            x => x.HasPermissionAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_IsSelf_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", userId.ToString()),
            new Claim("tid", tenantId.ToString())
        ], "test"));

        // Resource is the user itself (or an object with UserId property)
        var resource = new TestUserResource { UserId = userId };

        _tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);
        _tenantContextMock.Setup(x => x.HasTenant).Returns(true);

        var context = new AuthorizationHandlerContext([], user, resource);
        var parameters = RuleParameters.FromJson("{\"selfPermission\":\"users:edit:self\"}");

        // Also mock self permission check to be true if implementation checks it
        // The implementation checks: If targetUserId found:
        // if (targetUserId == currentUserIdStr)
        // {
        //    if (!string.IsNullOrEmpty(selfPermission)) -> checks permission
        // Wait, usually "Self" implies implicit access OR needs a self-permission.
        // Reading source:
        // if (targetUserId == currentUserIdStr) {
        //   if (!string.IsNullOrEmpty(selfPermission)) { return await HasPermission(selfPermission); }
        //   else { return Success(); } // Implicit self access if no selfPermission defined? 
        // Let's verify source code.
        
        _permissionServiceMock.Setup(x => x.HasPermissionAsync(userId, tenantId, "users:edit:self", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _evaluator.EvaluateAsync(context, parameters);

        result.IsSuccess.Should().BeTrue();
    }
    
    [Fact]
    public async Task EvaluateAsync_NotSelf_NoAnyPermission_ReturnsFail()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", userId.ToString()),
            new Claim("tid", tenantId.ToString())
        ], "test"));

        var resource = new TestUserResource { UserId = otherUserId };
        _tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);
        _tenantContextMock.Setup(x => x.HasTenant).Returns(true);

        var context = new AuthorizationHandlerContext([], user, resource);
        var parameters = RuleParameters.FromJson("{\"selfPermission\":\"users:edit:self\"}");

        var result = await _evaluator.EvaluateAsync(context, parameters);

        result.IsSuccess.Should().BeFalse();
    }

    private class TestUserResource
    {
        public Guid UserId { get; set; }
    }
}
