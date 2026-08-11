using FluentAssertions;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Funding;
using GameGuild.Economy.Risk;
using Microsoft.Extensions.Options;

namespace GameGuild.Economy.UnitTests.Risk;

public sealed class HardToSoftConversionRiskDecisionIssuerTests
{
    [Fact]
    public async Task IssueAsync_RejectsAnUnconfiguredDailyLimitBeforeTouchingPersistence()
    {
        var issuer = new PostgreSqlHardToSoftConversionRiskDecisionIssuer(
            null!,
            Options.Create(new SelfServiceHardToSoftRiskDecisionOptions
            {
                MaxHardCoinUnitsPerDay = 0
            }));

        var act = () => issuer.IssueAsync(Request(), CancellationToken.None);

        await act.Should().ThrowAsync<EconomySelfServiceCommandRejectedException>()
            .WithMessage("*daily risk limit*");
    }

    [Fact]
    public async Task IssueAsync_RejectsARequestThatExceedsTheConfiguredDailyLimitBeforeTouchingPersistence()
    {
        var issuer = new PostgreSqlHardToSoftConversionRiskDecisionIssuer(
            null!,
            Options.Create(new SelfServiceHardToSoftRiskDecisionOptions
            {
                MaxHardCoinUnitsPerDay = 99
            }));

        var act = () => issuer.IssueAsync(Request() with { TotalHardCoinUnits = 100 }, CancellationToken.None);

        await act.Should().ThrowAsync<EconomySelfServiceCommandRejectedException>()
            .WithMessage("*exceeds the configured daily HardCoin risk limit*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task IssueAsync_RejectsMissingRequiredIdentifiersBeforeTouchingPersistence(int missingIdentifier)
    {
        var request = Request();
        request = missingIdentifier switch
        {
            0 => request with { ActorId = Guid.Empty },
            1 => request with { TenantId = Guid.Empty },
            2 => request with { ReservationOperationId = Guid.Empty },
            _ => throw new ArgumentOutOfRangeException(nameof(missingIdentifier))
        };
        var issuer = new PostgreSqlHardToSoftConversionRiskDecisionIssuer(
            null!,
            Options.Create(new SelfServiceHardToSoftRiskDecisionOptions
            {
                MaxHardCoinUnitsPerDay = 200
            }));

        var act = () => issuer.IssueAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("request");
    }

    [Theory]
    [InlineData(29)]
    [InlineData(901)]
    public async Task IssueAsync_RejectsAnUnsafeDecisionLifetimeBeforeTouchingPersistence(int lifetimeSeconds)
    {
        var issuer = new PostgreSqlHardToSoftConversionRiskDecisionIssuer(
            null!,
            Options.Create(new SelfServiceHardToSoftRiskDecisionOptions
            {
                MaxHardCoinUnitsPerDay = 200,
                DecisionLifetimeSeconds = lifetimeSeconds
            }));

        var act = () => issuer.IssueAsync(Request(), CancellationToken.None);

        await act.Should().ThrowAsync<EconomySelfServiceCommandRejectedException>()
            .WithMessage("*lifetime must be between 30 and 900 seconds*");
    }

    private static HardToSoftConversionRiskDecisionRequest Request()
    {
        var now = DateTimeOffset.UtcNow;
        return new HardToSoftConversionRiskDecisionRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new WalletId(Guid.NewGuid()),
            Guid.NewGuid(),
            new IdempotencyKey("issuer-test-key"),
            0,
            100,
            [
                new(ExternalRiskSource.FinancialCrime, 1, now.AddMinutes(-1), now.AddMinutes(5), ExternalRiskOutcome.Allow, "financial-crime"),
                new(ExternalRiskSource.TrustSafety, 1, now.AddMinutes(-1), now.AddMinutes(5), ExternalRiskOutcome.Allow, "trust-safety")
            ],
            now);
    }
}
