using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.RuleEvaluators;

public class OwnerOrAclRuleEvaluatorTests
{
    private readonly Mock<IAccessControlListService> _aclServiceMock;
    private readonly OwnerOrAclRuleEvaluator _evaluator;

    public OwnerOrAclRuleEvaluatorTests()
    {
        _aclServiceMock = new Mock<IAccessControlListService>();
        _evaluator = new OwnerOrAclRuleEvaluator(_aclServiceMock.Object);
    }

    [Fact]
    public void RuleType_ReturnsOwnerOrAcl()
    {
        _evaluator.RuleType.Should().Be(RuleTypes.OwnerOrAcl);
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
    public async Task EvaluateAsync_UserIsOwner_ReturnsSuccess()
    {
        var ownerId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", ownerId.ToString())
        ], "test"));

        var resource = new TestOwnedResource { OwnerId = ownerId };
        var context = new AuthorizationHandlerContext([], user, resource);
        var parameters = RuleParameters.FromJson("{}");

        var result = await _evaluator.EvaluateAsync(context, parameters);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_UserAclAccessGranted_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", userId.ToString()),
            new Claim("TenantId", tenantId.ToString())
        ], "test"));

        var resource = new TestPermissionedResource { ResourceType = "doc", ResourceId = "1" };
        var context = new AuthorizationHandlerContext([], user, resource);
        var parameters = RuleParameters.FromJson("{\"minimumAccessLevel\":\"Read\"}");

        _aclServiceMock.Setup(x => x.HasAccessAsync(
                It.IsAny<AclSubject>(),
                tenantId,
                "doc",
                "1",
                AccessLevel.Read,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _evaluator.EvaluateAsync(context, parameters);

        result.IsSuccess.Should().BeTrue();
    }
    
    [Fact]
    public async Task EvaluateAsync_UserAclAccessDenied_ReturnsFail()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", userId.ToString()),
            new Claim("TenantId", tenantId.ToString())
        ], "test"));

        var resource = new TestPermissionedResource { ResourceType = "doc", ResourceId = "1" };
        var context = new AuthorizationHandlerContext([], user, resource);
        var parameters = RuleParameters.FromJson("{\"minimumAccessLevel\":\"Write\"}");

        _aclServiceMock.Setup(x => x.HasAccessAsync(
                It.IsAny<AclSubject>(),
                tenantId,
                "doc",
                "1",
                AccessLevel.Write,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _evaluator.EvaluateAsync(context, parameters);

        result.IsSuccess.Should().BeFalse();
    }

    private class TestOwnedResource : IOwnedResource
    {
        public Guid OwnerId { get; set; }
        public Guid TenantId { get; set; }
    }

    private class TestPermissionedResource : IAccessControlListResource
    {
        public string ResourceType { get; set; } = "";
        public string ResourceId { get; set; } = "";
        public Guid OwnerId { get; set; }
        public Guid TenantId { get; set; }
    }
}
