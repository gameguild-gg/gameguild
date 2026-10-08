using GameGuild;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Users;
using Moq;
using Xunit;

public sealed class SignInMfaMappingOutcomeTests
{
    [Theory]
    [InlineData(SignInMfaPurpose.VerifyFactor, false)]
    [InlineData(SignInMfaPurpose.EnrollFactor, false)]
    [InlineData(SignInMfaPurpose.VerifyFactor, true)]
    public async Task PendingMappingPreservesTheServerOutcomeAndExposesOnlyTheLimitedChallenge(SignInMfaPurpose purpose, bool risk)
    {
        var bearer = SignInMfaChallengeToken.Create();
        var expiry = DateTimeOffset.UtcNow.AddMinutes(5);
        var pending = new SignInMfaPendingResponse(bearer, expiry, purpose, risk);
        var repository = new Mock<IUserRepository>(MockBehavior.Strict);
        var mapped = await pending.ToDto(repository.Object, default);
        Assert.Same(pending, mapped);
        Assert.IsType<SignInMfaPendingResponse>(mapped);
        Assert.False(CommandOutcome.ShouldRollback(mapped));
        Assert.False(mapped.Success);
        Assert.True(mapped.RequiresMfa);
        Assert.Equal(bearer, mapped.MfaToken);
        Assert.Equal(risk, mapped.RequiresStepUp);
        Assert.Equal(risk ? bearer : null, mapped.StepUpToken);
        Assert.Equal(expiry.UtcDateTime, mapped.StepUpExpiresAt);
        Assert.Empty(mapped.AccessToken);
        Assert.Empty(mapped.RefreshToken);
        Assert.Equal(Guid.Empty, mapped.UserId);
        Assert.Equal(Guid.Empty, mapped.SessionId);
        AssertNoAuthenticatedProfile(mapped);
        Assert.Null(mapped.TenantId);
        Assert.Null(mapped.AvailableTenants);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TrustedCommittedDenialKeepsFailedProofAccountingWithoutProfileOrCredentials()
    {
        var denial = new SignInMfaCommittedDenial();
        var repository = new Mock<IUserRepository>(MockBehavior.Strict);
        var mapped = await denial.ToDto(repository.Object, default);
        Assert.Same(denial, mapped);
        Assert.IsType<SignInMfaCommittedDenial>(mapped);
        Assert.False(CommandOutcome.ShouldRollback(mapped));
        Assert.False(mapped.Success);
        Assert.Empty(mapped.AccessToken);
        Assert.Empty(mapped.RefreshToken);
        AssertNoAuthenticatedProfile(mapped);
        Assert.Equal(Guid.Empty, mapped.UserId);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublicMfaFlagsCannotCreateTheServerOnlyCommitOutcome()
    {
        var response = new SignInResponse { Success = false, RequiresMfa = true, MfaToken = SignInMfaChallengeToken.Create() };
        var repository = new Mock<IUserRepository>(MockBehavior.Strict);
        repository.Setup(port => port.GetByIdAsync(Guid.Empty, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var mapped = await response.ToDto(repository.Object, default);
        Assert.NotSame(response, mapped);
        Assert.IsType<SignInResponse>(mapped);
        Assert.True(CommandOutcome.ShouldRollback(mapped));
        Assert.True(mapped.RequiresMfa);
        Assert.Equal(response.MfaToken, mapped.MfaToken);
        repository.Verify(port => port.GetByIdAsync(Guid.Empty, It.IsAny<CancellationToken>()), Times.Once);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CancellationIsObservedBeforeReturningThePreservedPendingOutcome()
    {
        var pending = new SignInMfaPendingResponse(SignInMfaChallengeToken.Create(), DateTimeOffset.UtcNow.AddMinutes(5), SignInMfaPurpose.VerifyFactor, false);
        var repository = new Mock<IUserRepository>(MockBehavior.Strict);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.ToDto(repository.Object, cancellation.Token));
        repository.VerifyNoOtherCalls();
    }
    private static void AssertNoAuthenticatedProfile(SignInResponse response)
    {
        // The existing public contract requires User; preserve it with only the empty DTO.
        Assert.NotNull(response.User);
        Assert.Equal(Guid.Empty, response.User.Id);
        Assert.Empty(response.User.Email);
        Assert.Empty(response.User.Username);
        Assert.Null(response.User.FirstName);
        Assert.Null(response.User.LastName);
        Assert.Null(response.User.PhoneNumber);
        Assert.Null(response.User.LastLoginAt);
        Assert.False(response.User.EmailVerified);
        Assert.False(response.User.PhoneNumberVerified);
        Assert.Equal(default, response.User.CreatedAt);
    }
}
