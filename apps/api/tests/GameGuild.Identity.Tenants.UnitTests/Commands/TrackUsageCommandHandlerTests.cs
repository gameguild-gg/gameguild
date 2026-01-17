using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public class TrackUsageCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly Mock<IUsageTrackingService> _usageTrackingServiceMock;
    private readonly TrackUsageCommandHandler _handler;

    public TrackUsageCommandHandlerTests()
    {
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _usageTrackingServiceMock = new Mock<IUsageTrackingService>();
        _handler = new TrackUsageCommandHandler(_tenantRepositoryMock.Object, _usageTrackingServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ShouldReturnFailure()
    {
        var tenantId = Guid.NewGuid();
        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var result = await _handler.Handle(new TestTrackUsageCommand(tenantId, "api", "call", 1, 0m, null), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_Should_Track_Usage()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Tenant", Slug = "tenant" };
        var trackingId = Guid.NewGuid();
        UsageTracking? captured = null;

        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _usageTrackingServiceMock
            .Setup(s => s.TrackUsageAsync(It.IsAny<UsageTracking>(), It.IsAny<CancellationToken>()))
            .Callback<UsageTracking, CancellationToken>((usage, _) => captured = usage)
            .ReturnsAsync(trackingId);

        var result = await _handler.Handle(new TestTrackUsageCommand(tenantId, "api", "call", 5, 2.5m, new Dictionary<string, object> { ["key"] = "value" }), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TrackingId.Should().Be(trackingId);
        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(tenantId);
        captured.ResourceType.Should().Be("api");
        captured.UsageAmount.Should().Be(5);
        captured.Cost.Should().Be(2.5m);
    }

    private sealed record TestTrackUsageCommand(
        Guid TenantId,
        string ResourceType,
        string ActionType,
        int Quantity = 1,
        decimal? Cost = null,
        Dictionary<string, object>? Metadata = null)
        : TrackUsageCommand(TenantId, ResourceType, ActionType, Quantity, Cost, Metadata);
}
