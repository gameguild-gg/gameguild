using System.Security.Claims;
using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.RuleEvaluators;

/// <summary>
/// Unit tests for RequireMfaRuleEvaluator
/// </summary>
public class RequireMfaRuleEvaluatorTests
{
    private readonly RequireMfaRuleEvaluator _evaluator;

    public RequireMfaRuleEvaluatorTests()
    {
        _evaluator = new RequireMfaRuleEvaluator();
    }

    [Fact]
    public void RuleType_ReturnsRequireMfa()
    {
        // Assert
        _evaluator.RuleType.Should().Be(RuleTypes.RequireMfa);
    }

    [Fact]
    public async Task EvaluateAsync_WithUnauthenticatedUser_ReturnsFail()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated
        var context = new AuthorizationHandlerContext(
            new[] { new TestRequirement() },
            user,
            null);
        var parameters = RuleParameters.FromJson("{}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("authenticated");
    }

    [Fact]
    public async Task EvaluateAsync_WithMfaAmrClaim_ReturnsSuccess()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("amr", "mfa")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new AuthorizationHandlerContext(
            new[] { new TestRequirement() },
            user,
            null);
        var parameters = RuleParameters.FromJson("{}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WithMfaVerifiedClaim_ReturnsSuccess()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("mfa_verified", "true")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new AuthorizationHandlerContext(
            new[] { new TestRequirement() },
            user,
            null);
        var parameters = RuleParameters.FromJson("{}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WithoutMfaClaims_ReturnsFail()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("sub", "user123")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new AuthorizationHandlerContext(
            new[] { new TestRequirement() },
            user,
            null);
        var parameters = RuleParameters.FromJson("{}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("Multi-factor authentication");
    }

    [Fact]
    public async Task EvaluateAsync_WithCustomMfaClaimType_ReturnsSuccess()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("custom_mfa", "verified")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new AuthorizationHandlerContext(
            new[] { new TestRequirement() },
            user,
            null);
        var parameters = RuleParameters.FromJson("{\"mfaClaimType\":\"custom_mfa\",\"mfaClaimValue\":\"verified\"}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WithRecentMfaRequirement_AndValidTimestamp_ReturnsSuccess()
    {
        // Arrange
        var mfaTime = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds().ToString();
        var claims = new List<Claim>
        {
            new("amr", "mfa"),
            new("mfa_time", mfaTime)
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new AuthorizationHandlerContext(
            new[] { new TestRequirement() },
            user,
            null);
        var parameters = RuleParameters.FromJson("{\"requireRecent\":true,\"maxAgeMinutes\":30}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WithRecentMfaRequirement_AndStaleTimestamp_ReturnsFail()
    {
        // Arrange
        var mfaTime = DateTimeOffset.UtcNow.AddMinutes(-60).ToUnixTimeSeconds().ToString();
        var claims = new List<Claim>
        {
            new("amr", "mfa"),
            new("mfa_time", mfaTime)
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new AuthorizationHandlerContext(
            new[] { new TestRequirement() },
            user,
            null);
        var parameters = RuleParameters.FromJson("{\"requireRecent\":true,\"maxAgeMinutes\":30}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("expired");
    }

    [Fact]
    public async Task EvaluateAsync_WithRecentMfaRequirement_AndNoTimestamp_ReturnsFail()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("amr", "mfa")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new AuthorizationHandlerContext(
            new[] { new TestRequirement() },
            user,
            null);
        var parameters = RuleParameters.FromJson("{\"requireRecent\":true,\"maxAgeMinutes\":30}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("timestamp");
    }

    private class TestRequirement : IAuthorizationRequirement { }
}
