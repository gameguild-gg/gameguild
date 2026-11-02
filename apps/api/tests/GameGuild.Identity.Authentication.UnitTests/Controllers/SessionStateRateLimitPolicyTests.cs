using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Controllers;

/// <summary>
///     Regression coverage for issue #1035: the read endpoints the account-settings page
///     fans out on (GET /v1/auth/session-state and HEAD /v1/auth/external-logins) must
///     carry an action-level <see cref="RateLimitPolicies.Api"/> override so they do not
///     consume the class-level Authentication (10 req/min brute-force) budget. Credential
///     mutations stay on Authentication; all other actions keep inheriting the class policy.
/// </summary>
public sealed class SessionStateRateLimitPolicyTests
{
    private static List<EnableRateLimitingAttribute> GetActionAttributes(System.Reflection.MethodInfo action) =>
        action.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: false)
            .Cast<EnableRateLimitingAttribute>()
            .ToList();

    private static EnableRateLimitingAttribute? GetClassAttribute(Type controller) =>
        controller.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: false)
            .Cast<EnableRateLimitingAttribute>()
            .FirstOrDefault();

    [Fact]
    public void SessionState_UsesApiPolicyInsteadOfAuthenticationBudget()
    {
        var action = typeof(AuthController).GetMethod(nameof(AuthController.GetSessionState));

        action.Should().NotBeNull();
        var attributes = GetActionAttributes(action!);
        attributes.Should().ContainSingle();
        attributes[0].PolicyName.Should().Be(RateLimitPolicies.Api);
    }

    [Fact]
    public void PasswordChange_StillInheritsTheClassLevelAuthenticationPolicy()
    {
        var action = typeof(AuthController).GetMethod(nameof(AuthController.ChangePassword));

        action.Should().NotBeNull();
        GetActionAttributes(action!).Should().BeEmpty(
            "password:change is a credential mutation and must stay on the Authentication policy inherited from the controller");
    }

    [Fact]
    public void AuthController_ClassLevelAuthenticationPolicyIsUnchanged()
    {
        var classAttribute = GetClassAttribute(typeof(AuthController));

        classAttribute.Should().NotBeNull();
        classAttribute!.PolicyName.Should().Be(RateLimitPolicies.Authentication);
    }

    [Fact]
    public void ExternalLoginsHead_UsesApiPolicyInsteadOfAuthenticationBudget()
    {
        var action = typeof(ExternalLoginsController).GetMethod(nameof(ExternalLoginsController.GetExternalLogins));

        action.Should().NotBeNull();
        var attributes = GetActionAttributes(action!);
        attributes.Should().ContainSingle();
        attributes[0].PolicyName.Should().Be(RateLimitPolicies.Api);
    }

    [Fact]
    public void ExternalLoginsController_ClassLevelAuthenticationPolicyIsUnchanged()
    {
        var classAttribute = GetClassAttribute(typeof(ExternalLoginsController));

        classAttribute.Should().NotBeNull();
        classAttribute!.PolicyName.Should().Be(RateLimitPolicies.Authentication);
    }

    [Theory]
    [InlineData(nameof(ExternalLoginsController.LinkGoogle))]
    [InlineData(nameof(ExternalLoginsController.DiscordLinkAuthorize))]
    [InlineData(nameof(ExternalLoginsController.DiscordLinkCallback))]
    [InlineData(nameof(ExternalLoginsController.RevokeScopes))]
    [InlineData(nameof(ExternalLoginsController.Unlink))]
    public void ExternalLogins_MutationsStillInheritTheAuthenticationPolicy(string actionName)
    {
        var action = typeof(ExternalLoginsController).GetMethod(actionName);

        action.Should().NotBeNull();
        GetActionAttributes(action!).Should().BeEmpty(
            "link/unlink/scope-revoke are credential mutations and must stay on the Authentication policy");
    }
}
