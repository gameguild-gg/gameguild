using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Models;

public sealed class AuthenticationBranchModelTests
{
    [Fact]
    public void VerifiableCredential_IsValid_ShouldReflectExpirationRules()
    {
        var noExpiration = new TestVerifiableCredential();
        var futureExpiration = new TestVerifiableCredential { ExpirationDate = DateTime.UtcNow.AddMinutes(5) };
        var pastExpiration = new TestVerifiableCredential { ExpirationDate = DateTime.UtcNow.AddMinutes(-5) };

        noExpiration.IsValid.Should().BeTrue();
        futureExpiration.IsValid.Should().BeTrue();
        pastExpiration.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AuthenticationFlowState_NextStep_ShouldReturnFirstRemainingRequiredStep()
    {
        var state = new TestAuthenticationFlowState
        {
            RequiredSteps = [AuthenticationStep.PrimaryCredential, AuthenticationStep.MfaVerification, AuthenticationStep.DeviceTrust],
            CompletedSteps = [AuthenticationStep.PrimaryCredential]
        };

        state.NextStep.Should().Be(AuthenticationStep.MfaVerification);
    }

    [Fact]
    public void AuthenticationFlowState_ShouldReportCompletionAndExpirationState()
    {
        var completed = new TestAuthenticationFlowState
        {
            RequiredSteps = [AuthenticationStep.PrimaryCredential],
            CompletedSteps = [AuthenticationStep.PrimaryCredential],
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        var expired = new TestAuthenticationFlowState
        {
            RequiredSteps = [AuthenticationStep.PrimaryCredential],
            CompletedSteps = [],
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        };

        completed.NextStep.Should().Be(AuthenticationStep.PrimaryCredential);
        completed.IsExpired.Should().BeFalse();
        expired.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void RefreshTokenResponse_ToDto_ShouldProjectExpiryFromExpiresIn()
    {
        var response = new RefreshTokenResponse
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            ExpiresIn = 120,
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };

        var dto = response.ToDto();

        dto.Should().NotBeSameAs(response);
        dto.AccessToken.Should().Be("access-token");
        dto.RefreshToken.Should().Be("refresh-token");
        dto.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddSeconds(120), TimeSpan.FromSeconds(5));
    }

    private sealed class TestVerifiableCredential : VerifiableCredential
    {
    }

    private sealed class TestAuthenticationFlowState : AuthenticationFlowState
    {
    }
}