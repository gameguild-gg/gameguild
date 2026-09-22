using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.RuleEvaluators;

public sealed class RequireAnyRoleRuleEvaluatorTests
{
    private readonly RequireAnyRoleRuleEvaluator _evaluator = new();

    [Fact]
    public async Task EvaluateAsync_AllowsAnAuthenticatedUserWithAnAcceptedRole()
    {
        var principal = Principal(new Claim(ClaimTypes.Role, "PropertyManager"));
        var context = new AuthorizationHandlerContext([], principal, null);
        var parameters = RuleParameters.FromJson("""{"roles":["PropertyManager","TenantAdmin"]}""");

        var result = await _evaluator.EvaluateAsync(context, parameters);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_DeniesAnAuthenticatedUserWithoutAnAcceptedRole()
    {
        var principal = Principal(new Claim(ClaimTypes.Role, "Renter"));
        var context = new AuthorizationHandlerContext([], principal, null);
        var parameters = RuleParameters.FromJson("""{"roles":["PropertyManager"]}""");

        var result = await _evaluator.EvaluateAsync(context, parameters);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("required roles");
    }

    [Fact]
    public async Task EvaluateAsync_DeniesUnauthenticatedUsersAndEmptyRoleRules()
    {
        var unauthenticated = new AuthorizationHandlerContext([], new ClaimsPrincipal(new ClaimsIdentity()), null);
        var authenticated = new AuthorizationHandlerContext([], Principal(), null);

        (await _evaluator.EvaluateAsync(
            unauthenticated,
            RuleParameters.FromJson("""{"roles":["PropertyManager"]}"""))).IsSuccess.Should().BeFalse();
        (await _evaluator.EvaluateAsync(
            authenticated,
            RuleParameters.FromJson("""{"roles":[]}"""))).IsSuccess.Should().BeFalse();
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "TestAuth"));
}
