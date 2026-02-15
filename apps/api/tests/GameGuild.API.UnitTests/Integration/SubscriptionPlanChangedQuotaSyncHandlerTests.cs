using FluentAssertions;
using GameGuild.API.Integration;
using GameGuild.Commerce.Subscriptions;
using GameGuild.Resources;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameGuild.API.UnitTests.Integration;

public class SubscriptionPlanChangedQuotaSyncHandlerTests
{
    private readonly Mock<ISubscriptionPlanRepository> _planRepoMock = new();
    private readonly Mock<IResourceQuotaService> _quotaServiceMock = new();
    private readonly Mock<ILogger<SubscriptionPlanChangedQuotaSyncHandler>> _loggerMock = new();
    private readonly SubscriptionPlanChangedQuotaSyncHandler _handler;

    public SubscriptionPlanChangedQuotaSyncHandlerTests()
    {
        _handler = new SubscriptionPlanChangedQuotaSyncHandler(
            _planRepoMock.Object,
            _quotaServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNewPlanNotFound_ShouldSkip()
    {
        var evt = CreateEvent();
        _planRepoMock.Setup(r => r.GetByIdAsync(evt.NewPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionPlan?)null);

        await _handler.Handle(evt, CancellationToken.None);

        _quotaServiceMock.Verify(
            s => s.SetQuotaAsync(It.IsAny<Guid>(), It.IsAny<ResourceUsageType>(),
                It.IsAny<long?>(), It.IsAny<long?>(),
                It.IsAny<ResourceQuotaPeriod>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPlanHasNoLimits_ShouldNotSetQuotas()
    {
        var evt = CreateEvent();
        var plan = CreatePlan(maxUsers: null, maxStorageMb: null, maxApiCalls: null);
        _planRepoMock.Setup(r => r.GetByIdAsync(evt.NewPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        await _handler.Handle(evt, CancellationToken.None);

        _quotaServiceMock.Verify(
            s => s.SetQuotaAsync(It.IsAny<Guid>(), It.IsAny<ResourceUsageType>(),
                It.IsAny<long?>(), It.IsAny<long?>(),
                It.IsAny<ResourceQuotaPeriod>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPlanHasMaxUsers_ShouldSetUsersQuota()
    {
        var evt = CreateEvent();
        var plan = CreatePlan(maxUsers: 100);
        _planRepoMock.Setup(r => r.GetByIdAsync(evt.NewPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        await _handler.Handle(evt, CancellationToken.None);

        _quotaServiceMock.Verify(
            s => s.SetQuotaAsync(evt.TenantId, ResourceUsageType.Users,
                80L, 100L, ResourceQuotaPeriod.Monthly, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPlanHasMaxStorage_ShouldConvertMbToBytes()
    {
        var evt = CreateEvent();
        var plan = CreatePlan(maxStorageMb: 500);
        _planRepoMock.Setup(r => r.GetByIdAsync(evt.NewPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        await _handler.Handle(evt, CancellationToken.None);

        var expectedBytes = 500L * 1024 * 1024;
        var expectedSoftLimit = (long)(expectedBytes * 0.8);
        _quotaServiceMock.Verify(
            s => s.SetQuotaAsync(evt.TenantId, ResourceUsageType.Storage,
                expectedSoftLimit, expectedBytes,
                ResourceQuotaPeriod.Monthly, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPlanHasAllLimits_ShouldSetAllQuotas()
    {
        var evt = CreateEvent();
        var plan = CreatePlan(maxUsers: 50, maxStorageMb: 1000, maxApiCalls: 10000);
        _planRepoMock.Setup(r => r.GetByIdAsync(evt.NewPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        await _handler.Handle(evt, CancellationToken.None);

        _quotaServiceMock.Verify(
            s => s.SetQuotaAsync(evt.TenantId, ResourceUsageType.Users,
                It.IsAny<long?>(), It.IsAny<long?>(),
                ResourceQuotaPeriod.Monthly, It.IsAny<CancellationToken>()),
            Times.Once);
        _quotaServiceMock.Verify(
            s => s.SetQuotaAsync(evt.TenantId, ResourceUsageType.Storage,
                It.IsAny<long?>(), It.IsAny<long?>(),
                ResourceQuotaPeriod.Monthly, It.IsAny<CancellationToken>()),
            Times.Once);
        _quotaServiceMock.Verify(
            s => s.SetQuotaAsync(evt.TenantId, ResourceUsageType.ApiCalls,
                It.IsAny<long?>(), It.IsAny<long?>(),
                ResourceQuotaPeriod.Monthly, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenQuotaSetFails_ShouldNotThrow()
    {
        var evt = CreateEvent();
        var plan = CreatePlan(maxUsers: 10);
        _planRepoMock.Setup(r => r.GetByIdAsync(evt.NewPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        _quotaServiceMock.Setup(s => s.SetQuotaAsync(
                It.IsAny<Guid>(), It.IsAny<ResourceUsageType>(),
                It.IsAny<long?>(), It.IsAny<long?>(),
                It.IsAny<ResourceQuotaPeriod>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Quota service down"));

        var act = () => _handler.Handle(evt, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    // Helpers
    private static SubscriptionPlanChangedEvent CreateEvent(
        decimal oldAmount = 10m, decimal newAmount = 20m)
    {
        return new SubscriptionPlanChangedEvent(
            Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(),
            new Money(oldAmount), new Money(newAmount));
    }

    private static SubscriptionPlan CreatePlan(
        int? maxUsers = null, long? maxStorageMb = null, long? maxApiCalls = null)
    {
        var plan = new SubscriptionPlan("Test Plan", "test-plan", 1000);
        plan.MaxUsers = maxUsers;
        plan.MaxStorageMb = maxStorageMb;
        plan.MaxApiCallsPerMonth = maxApiCalls;
        return plan;
    }
}
