using FluentAssertions;
using GameGuild;
using GameGuild.Features;
using Moq;
using Xunit;

namespace GameGuild.Tests.Features.Unit.Queries;

/// <summary>
/// Unit tests for GetAllFeatureFlagsQueryHandler
/// </summary>
public class GetAllFeatureFlagsQueryHandlerTests
{
    private readonly Mock<IFeatureFlagQueryRepository> _mockRepository;
    private readonly GetAllFeatureFlagsQueryHandler _handler;

    public GetAllFeatureFlagsQueryHandlerTests()
    {
        _mockRepository = new Mock<IFeatureFlagQueryRepository>();
        _handler = new GetAllFeatureFlagsQueryHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_WhenNoFilters_ReturnsAllFlags()
    {
        // Arrange
        var query = new GetAllFeatureFlagsQuery();
        var featureFlags = new List<FeatureFlag>
        {
            CreateFeatureFlag("feature-1"),
            CreateFeatureFlag("feature-2"),
            CreateFeatureFlag("feature-3")
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlags);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WhenFilterByEnvironment_FiltersCorrectly()
    {
        // Arrange
        var query = new GetAllFeatureFlagsQuery { Environment = "production" };
        var featureFlags = new List<FeatureFlag>
        {
            CreateFeatureFlag("feature-1", environment: "production"),
            CreateFeatureFlag("feature-2", environment: "staging"),
            CreateFeatureFlag("feature-3", environment: null) // Global - should be included
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlags);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(f => f.Key).Should().Contain("feature-1");
        result.Select(f => f.Key).Should().Contain("feature-3");
    }

    [Fact]
    public async Task Handle_WhenFilterByEnabled_FiltersCorrectly()
    {
        // Arrange
        var query = new GetAllFeatureFlagsQuery { IsEnabled = true };
        var featureFlags = new List<FeatureFlag>
        {
            CreateFeatureFlag("feature-1", isEnabled: true),
            CreateFeatureFlag("feature-2", isEnabled: false),
            CreateFeatureFlag("feature-3", isEnabled: true)
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlags);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.All(f => f.IsEnabled).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenFilterByDisabled_FiltersCorrectly()
    {
        // Arrange
        var query = new GetAllFeatureFlagsQuery { IsEnabled = false };
        var featureFlags = new List<FeatureFlag>
        {
            CreateFeatureFlag("feature-1", isEnabled: true),
            CreateFeatureFlag("feature-2", isEnabled: false),
            CreateFeatureFlag("feature-3", isEnabled: false)
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlags);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.All(f => !f.IsEnabled).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenFilterByGlobal_ReturnsOnlyGlobalFlags()
    {
        // Arrange
        var query = new GetAllFeatureFlagsQuery { IsGlobal = true };
        var featureFlags = new List<FeatureFlag>
        {
            CreateFeatureFlag("feature-1"), // Global (no tenant)
            CreateFeatureFlag("feature-2"), // Global (no tenant)
            CreateFeatureFlag("feature-3")  // Global (no tenant)
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlags);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.All(f => !f.TenantId.HasValue).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenFilterByTenantSpecific_ReturnsEmptyIfNoTenantFlags()
    {
        // Arrange - Note: We can't easily set TenantId as it's protected
        // This test verifies the filtering logic when there are no tenant-specific flags
        var query = new GetAllFeatureFlagsQuery { IsGlobal = false };
        var featureFlags = new List<FeatureFlag>
        {
            CreateFeatureFlag("feature-1"), // Global
            CreateFeatureFlag("feature-2"), // Global
            CreateFeatureFlag("feature-3")  // Global
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlags);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - All flags are global, so filtering for tenant-specific returns none
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCombiningFilters_AppliesAllFilters()
    {
        // Arrange
        var query = new GetAllFeatureFlagsQuery
        {
            Environment = "production",
            IsEnabled = true,
            IsGlobal = true
        };

        var featureFlags = new List<FeatureFlag>
        {
            CreateFeatureFlag("feature-1", isEnabled: true, environment: "production"),
            CreateFeatureFlag("feature-2", isEnabled: true, environment: "staging"),
            CreateFeatureFlag("feature-3", isEnabled: false, environment: "production"),
            CreateFeatureFlag("feature-4", isEnabled: true, environment: "production")
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlags);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - Only feature-1 and feature-4 match all criteria
        result.Should().HaveCount(2);
        result.Select(f => f.Key).Should().Contain("feature-1");
        result.Select(f => f.Key).Should().Contain("feature-4");
    }

    [Fact]
    public async Task Handle_WhenNoFlagsExist_ReturnsEmptyCollection()
    {
        // Arrange
        var query = new GetAllFeatureFlagsQuery();

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeatureFlag>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MapsToDto()
    {
        // Arrange
        var featureId = Guid.NewGuid();
        var query = new GetAllFeatureFlagsQuery();
        var featureFlag = new FeatureFlag
        {
            Id = featureId,
            Key = "test-key",
            Name = "Test Feature",
            Description = "A test feature",
            IsEnabled = true,
            Type = FeatureFlagType.Toggle,
            Environment = "production"
        };
        typeof(EntityBase).GetProperty(nameof(EntityBase.CreatedAt))!.SetValue(featureFlag, DateTime.UtcNow);

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeatureFlag> { featureFlag });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.First();
        dto.Id.Should().Be(featureId);
        dto.Key.Should().Be("test-key");
        dto.Name.Should().Be("Test Feature");
        dto.Description.Should().Be("A test feature");
        dto.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        // Arrange
        var query = new GetAllFeatureFlagsQuery();
        using var cts = new CancellationTokenSource();

        _mockRepository
            .Setup(r => r.GetAllAsync(cts.Token))
            .ReturnsAsync(new List<FeatureFlag>());

        // Act
        await _handler.Handle(query, cts.Token);

        // Assert
        _mockRepository.Verify(r => r.GetAllAsync(cts.Token), Times.Once);
    }

    private static FeatureFlag CreateFeatureFlag(
        string key,
        bool isEnabled = true,
        string? environment = null)
    {
        var flag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = $"Feature {key}",
            IsEnabled = isEnabled,
            Type = FeatureFlagType.Toggle,
            Environment = environment ?? "production"
        };
        typeof(EntityBase).GetProperty(nameof(EntityBase.CreatedAt))!.SetValue(flag, DateTime.UtcNow);
        return flag;
    }
}
