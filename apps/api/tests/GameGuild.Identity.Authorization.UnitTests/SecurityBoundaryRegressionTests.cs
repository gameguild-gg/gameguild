using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests;

public sealed class SecurityBoundaryRegressionTests
{
    [Fact]
    public async Task Build_InvalidConfiguration_Denies()
    {
        var policy = new DefaultPolicyMerger().Build(new PolicyDefinition
        {
            PolicyName = "InvalidPolicy",
            RequireAuthentication = true,
            IsConfigurationValid = false
        });
        var principal = new ClaimsPrincipal(new ClaimsIdentity([], "Test"));
        var context = new AuthorizationHandlerContext(policy.Requirements, principal, resource: null);

        foreach (var handler in policy.Requirements.OfType<IAuthorizationHandler>())
            await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Build_AnonymousPolicy_AllowsWithoutEmptyRuleset()
    {
        var policy = new DefaultPolicyMerger().Build(new PolicyDefinition
        {
            PolicyName = Policies.Anonymous,
            RequireAuthentication = false,
            UseRuleBasedEvaluation = false
        });
        var context = new AuthorizationHandlerContext(
            policy.Requirements,
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource: null);

        foreach (var handler in policy.Requirements.OfType<IAuthorizationHandler>())
            await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public void Build_ComposesRulesRolesPermissionsAndResourceAccess()
    {
        var definition = new PolicyDefinition
        {
            PolicyName = "ComposedPolicy",
            UseRuleBasedEvaluation = true,
            Rules =
            [
                new PolicyRule { Type = RuleTypes.RequireMfa }
            ],
            RequiredRoles = ["Admin"],
            RequiredPermissions = ["users:manage"],
            RequireAccessControlListAccess = true,
            ResourceType = "User",
            MinimumAccessLevel = "Write"
        };

        var policy = new DefaultPolicyMerger().Build(definition);

        policy.Requirements.Should().ContainSingle(requirement => requirement is RulesetRequirement);
        policy.Requirements.Should().ContainSingle(requirement => requirement is RolesAuthorizationRequirement);
        policy.Requirements.Should().ContainSingle(requirement => requirement is PermissionRequirement);
        policy.Requirements.Should().ContainSingle(requirement => requirement is ResourceAccessRequirement);
    }

    [Fact]
    public async Task PermissionHandler_DatabaseDenyOverridesPermissionClaim()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permission = "users:manage";
        var tenantContext = new Mock<IAuthorizationTenantContext>();
        tenantContext.SetupGet(context => context.HasTenant).Returns(true);
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        var permissionService = new Mock<IAuthorizationPermissionService>();
        permissionService
            .Setup(service => service.HasPermissionAsync(userId, tenantId, permission, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var tokenOptions = AuthorizationTokenOptions.CreateDefault();
        var handler = new PermissionHandler(
            tenantContext.Object,
            permissionService.Object,
            Options.Create(tokenOptions),
            NullLogger<PermissionHandler>.Instance);
        var requirement = new PermissionRequirement(permission);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(tokenOptions.TenantClaimType, tenantId.ToString()),
            new Claim(tokenOptions.PermissionClaimType, permission)
        ], "Test"));
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        context.HasFailed.Should().BeTrue();
        permissionService.Verify(
            service => service.HasPermissionAsync(userId, tenantId, permission, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(RuleTypes.RequireAllPermissions)]
    [InlineData(RuleTypes.RequireAnyPermission)]
    public void RuleDefinition_EmptyPermissionArray_IsInvalid(string ruleType)
    {
        var rule = new RuleDefinition
        {
            Type = ruleType,
            Params = new Dictionary<string, JsonElement>
            {
                ["permissions"] = JsonSerializer.SerializeToElement(Array.Empty<string>())
            }
        };

        rule.Validate().IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task SelfOrPermission_RouteTargetDifferentFromActor_DoesNotTreatSelfPermissionAsGlobal()
    {
        var actorId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permissionService = new Mock<IAuthorizationPermissionService>();
        permissionService
            .Setup(service => service.HasPermissionAsync(actorId, tenantId, "users:edit:self", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        permissionService
            .Setup(service => service.HasPermissionAsync(actorId, tenantId, "users:manage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var tenantContext = new Mock<IAuthorizationTenantContext>();
        tenantContext.SetupGet(context => context.HasTenant).Returns(true);
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        var tenantMembershipChecker = new Mock<ITenantMembershipChecker>();
        tenantMembershipChecker
            .Setup(checker => checker.IsUserMemberOfTenantAsync(targetId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var evaluator = new SelfOrPermissionRuleEvaluator(
            permissionService.Object,
            tenantContext.Object,
            tenantMembershipChecker.Object);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, actorId.ToString()),
            new Claim(ClaimNames.TenantId, tenantId.ToString())
        ], "Test"));
        var httpContext = new DefaultHttpContext { User = principal };
        httpContext.Request.RouteValues["userId"] = targetId;
        var authorizationContext = new AuthorizationHandlerContext([], principal, httpContext);
        var parameters = new RuleParameters(new Dictionary<string, JsonElement>
        {
            ["selfPermission"] = JsonSerializer.SerializeToElement("users:edit:self"),
            ["anyPermission"] = JsonSerializer.SerializeToElement("users:manage")
        });

        var result = await evaluator.EvaluateAsync(authorizationContext, parameters);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task OwnerOrAcl_MissingResource_FailsInsteadOfSkipping()
    {
        var evaluator = new OwnerOrAclRuleEvaluator(Mock.Of<IAccessControlListService>());
        var principal = new ClaimsPrincipal(new ClaimsIdentity([], "Test"));
        var context = new AuthorizationHandlerContext([], principal, resource: null);

        var result = await evaluator.EvaluateAsync(context, new RuleParameters());

        result.IsSuccess.Should().BeFalse();
        result.IsSkipped.Should().BeFalse();
    }
}
