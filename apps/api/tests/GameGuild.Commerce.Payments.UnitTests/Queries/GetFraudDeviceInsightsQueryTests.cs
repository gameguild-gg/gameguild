using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.FraudQueries;

/// <summary>
///     Issue #409 remainder: the cross-account device listing. Fingerprints shared by
///     multiple customer accounts surface with their per-account breakdown; single-account
///     devices stay off the listing.
/// </summary>
public class GetFraudDeviceInsightsQueryTests : IAsyncDisposable
{
    private readonly PaymentsPersistenceTestDbContext _context = new(
        new DbContextOptionsBuilder<PaymentsPersistenceTestDbContext>()
            .UseInMemoryDatabase($"fraud-device-insights-{Guid.NewGuid():N}")
            .Options);

    private readonly Guid _tenantId = Guid.NewGuid();

    public GetFraudDeviceInsightsQueryTests()
    {
        Seed("fp-shared", "cus_a");
        Seed("fp-shared", "cus_b");
        Seed("fp-shared", "cus_b");
        Seed("fp-solo", "cus_a");
        _context.SaveChanges();
    }

    [Fact]
    public async Task Handle_Should_List_Only_Cross_Account_Devices_With_Their_Account_Breakdown()
    {
        var handler = new GetFraudDeviceInsightsQueryHandler(_context, Monitor());

        var insights = await handler.Handle(new GetFraudDeviceInsightsQuery(TenantId: _tenantId), CancellationToken.None);

        var insight = insights.SingleOrDefault(i => i.DeviceFingerprint == "fp-shared");
        insight.Should().NotBeNull();
        insight!.AccountCount.Should().Be(2);
        insight.AttemptCount.Should().Be(3);
        insight.Accounts.Should().HaveCount(2);
        insight.Accounts.Single(a => a.AccountKey == "cus_b").AttemptCount.Should().Be(2);
        insights.Should().NotContain(i => i.DeviceFingerprint == "fp-solo");
    }

    [Fact]
    public async Task Handle_Should_Respect_The_MinAccounts_Filter()
    {
        var handler = new GetFraudDeviceInsightsQueryHandler(_context, Monitor());

        var insights = await handler.Handle(
            new GetFraudDeviceInsightsQuery(TenantId: _tenantId, MinAccounts: 3),
            CancellationToken.None);

        insights.Should().BeEmpty("no device in the fixture reached three accounts");
    }

    private static Microsoft.Extensions.Options.IOptionsMonitor<FraudAssessmentOptions> Monitor()
    {
        var monitor = new Mock<Microsoft.Extensions.Options.IOptionsMonitor<FraudAssessmentOptions>>();
        monitor.SetupGet(m => m.CurrentValue).Returns(new FraudAssessmentOptions());
        return monitor.Object;
    }

    private void Seed(string deviceFingerprint, string externalCustomerId)
    {
        var payment = Payment.Create(
            _tenantId,
            10m,
            "USD",
            $"key-{Guid.NewGuid():N}",
            subscriptionId: Guid.NewGuid(),
            externalCustomerId: externalCustomerId);
        payment.RecordFraudAttemptContext(null, deviceFingerprint);
        _context.Set<Payment>().Add(payment);
    }

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}
