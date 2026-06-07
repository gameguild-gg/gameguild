using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Features.UnitTests.Services;

public class FeatureFlagManagementServiceTests
{
    private readonly Mock<IFeatureFlagQueryRepository> _queryRepositoryMock;
    private readonly Mock<IFeatureFlagTargetingRepository> _targetingRepositoryMock;
    private readonly Mock<ILogger<FeatureFlagManagementService>> _loggerMock;
    private readonly Mock<IOptions<FeatureFlagOptions>> _optionsMock;
    private readonly FeatureFlagManagementService _service;

    public FeatureFlagManagementServiceTests()
    {
        _queryRepositoryMock = new Mock<IFeatureFlagQueryRepository>();
        _targetingRepositoryMock = new Mock<IFeatureFlagTargetingRepository>();
        _loggerMock = new Mock<ILogger<FeatureFlagManagementService>>();
        _optionsMock = new Mock<IOptions<FeatureFlagOptions>>();

        _optionsMock.Setup(o => o.Value).Returns(new FeatureFlagOptions());

        _service = new FeatureFlagManagementService(
            _queryRepositoryMock.Object,
            _targetingRepositoryMock.Object,
            _loggerMock.Object,
            _optionsMock.Object
        );
    }

    [Fact]
    public async Task CreateFeatureFlagAsync_CreatesFlag_WithCorrectProperties()
    {
        // Arrange
        var request = new CreateFeatureFlagRequest
        {
            Key = "new-feature",
            Name = "New Feature",
            Description = "Test feature",
            Type = FeatureFlagType.Toggle,
            DefaultValue = "false",
            IsEnabled = true,
            RolloutPercentage = 50,
            Environment = "production"
        };

        _queryRepositoryMock.Setup(r => r.GetByKeyAsync(request.Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        var featureFlag = new FeatureFlag { Id = Guid.NewGuid() };
        _queryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<FeatureFlag>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlag);

        // Act
        var result = await _service.CreateFeatureFlagAsync(request);

        // Assert
        result.Should().Be(featureFlag.Id);
        _queryRepositoryMock.Verify(r => r.AddAsync(It.Is<FeatureFlag>(f =>
            f.Key == request.Key &&
            f.Name == request.Name &&
            f.IsEnabled == request.IsEnabled &&
            f.RolloutPercentage == request.RolloutPercentage
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateFeatureFlagAsync_ThrowsException_WhenKeyAlreadyExists()
    {
        // Arrange
        var request = new CreateFeatureFlagRequest
        {
            Key = "existing-feature",
            Name = "Feature",
            Type = FeatureFlagType.Toggle,
            DefaultValue = "true"
        };

        var existingFlag = new FeatureFlag { Id = Guid.NewGuid(), Key = request.Key };
        _queryRepositoryMock.Setup(r => r.GetByKeyAsync(request.Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFlag);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateFeatureFlagAsync(request));
    }

    [Fact]
    public async Task CreateFeatureFlagAsync_ThrowsException_WhenKeyIsEmpty()
    {
        // Arrange
        var request = new CreateFeatureFlagRequest
        {
            Key = "",
            Name = "Feature",
            Type = FeatureFlagType.Toggle,
            DefaultValue = "true"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateFeatureFlagAsync(request));
    }

    [Fact]
    public async Task CreateFeatureFlagAsync_ThrowsException_WhenNameIsEmpty()
    {
        // Arrange
        var request = new CreateFeatureFlagRequest
        {
            Key = "feature",
            Name = "",
            Type = FeatureFlagType.Toggle,
            DefaultValue = "true"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateFeatureFlagAsync(request));
    }

    [Fact]
    public async Task CreateFeatureFlagAsync_ThrowsException_WhenRolloutPercentageInvalid()
    {
        // Arrange
        var request = new CreateFeatureFlagRequest
        {
            Key = "feature",
            Name = "Feature",
            Type = FeatureFlagType.Toggle,
            DefaultValue = "true",
            RolloutPercentage = 150 // Invalid
        };

        _queryRepositoryMock.Setup(r => r.GetByKeyAsync(request.Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.CreateFeatureFlagAsync(request));
    }

    [Fact]
    public async Task UpdateFeatureFlagAsync_UpdatesProperties_WhenFlagExists()
    {
        // Arrange
        var flagId = Guid.NewGuid();
        var existingFlag = new FeatureFlag
        {
            Id = flagId,
            Key = "feature",
            Name = "Old Name",
            Description = "Old Description",
            IsEnabled = false,
            RolloutPercentage = 0
        };

        var request = new UpdateFeatureFlagRequest
        {
            Name = "New Name",
            Description = "New Description",
            IsEnabled = true,
            RolloutPercentage = 75
        };

        _queryRepositoryMock.Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFlag);

        // Act
        await _service.UpdateFeatureFlagAsync(flagId, request);

        // Assert
        existingFlag.Name.Should().Be("New Name");
        existingFlag.Description.Should().Be("New Description");
        existingFlag.IsEnabled.Should().BeTrue();
        existingFlag.RolloutPercentage.Should().Be(75);
        _queryRepositoryMock.Verify(r => r.UpdateAsync(existingFlag, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateFeatureFlagAsync_ThrowsException_WhenFlagNotFound()
    {
        // Arrange
        var flagId = Guid.NewGuid();
        var request = new UpdateFeatureFlagRequest { Name = "New Name" };

        _queryRepositoryMock.Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateFeatureFlagAsync(flagId, request));
    }

    [Fact]
    public async Task UpdateFeatureFlagAsync_ThrowsException_WhenRolloutPercentageInvalid()
    {
        // Arrange
        var flagId = Guid.NewGuid();
        var existingFlag = new FeatureFlag { Id = flagId };
        var request = new UpdateFeatureFlagRequest { RolloutPercentage = -10 };

        _queryRepositoryMock.Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFlag);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.UpdateFeatureFlagAsync(flagId, request));
    }

    [Fact]
    public async Task DeleteFeatureFlagAsync_DeletesFlag_WhenExists()
    {
        // Arrange
        var flagId = Guid.NewGuid();
        var existingFlag = new FeatureFlag { Id = flagId };

        _queryRepositoryMock.Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFlag);

        // Act
        await _service.DeleteFeatureFlagAsync(flagId);

        // Assert
        _targetingRepositoryMock.Verify(r => r.DeleteTargetsByFeatureFlagAsync(flagId, It.IsAny<CancellationToken>()), Times.Once);
        _queryRepositoryMock.Verify(r => r.RemoveAsync(flagId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteFeatureFlagAsync_ThrowsException_WhenFlagNotFound()
    {
        // Arrange
        var flagId = Guid.NewGuid();

        _queryRepositoryMock.Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeleteFeatureFlagAsync(flagId));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsFlag_WhenExists()
    {
        // Arrange
        var flagId = Guid.NewGuid();
        var flag = new FeatureFlag { Id = flagId, Key = "test-feature" };

        _queryRepositoryMock.Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flag);

        // Act
        var result = await _service.GetByIdAsync(flagId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(flagId);
        result.Key.Should().Be("test-feature");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        var flagId = Guid.NewGuid();

        _queryRepositoryMock.Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        // Act
        var result = await _service.GetByIdAsync(flagId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByKeyAsync_ReturnsFlag_WhenExists()
    {
        // Arrange
        var key = "my-feature";
        var flag = new FeatureFlag { Id = Guid.NewGuid(), Key = key };

        _queryRepositoryMock.Setup(r => r.GetByKeyAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flag);

        // Act
        var result = await _service.GetByKeyAsync(key);

        // Assert
        result.Should().NotBeNull();
        result!.Key.Should().Be(key);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllFlags_WhenNoFilters()
    {
        // Arrange
        var flags = new List<FeatureFlag>
        {
            new() { Id = Guid.NewGuid(), Key = "feature1" },
            new() { Id = Guid.NewGuid(), Key = "feature2" }
        };

        _queryRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(f => f.Key == "feature1");
        result.Should().Contain(f => f.Key == "feature2");
    }

    [Fact]
    public async Task GetAllAsync_FiltersByEnvironment_WhenProvided()
    {
        // Arrange
        var environment = "staging";
        var flags = new List<FeatureFlag>
        {
            new() { Id = Guid.NewGuid(), Key = "feature1", Environment = "staging" }
        };

        _queryRepositoryMock.Setup(r => r.GetByEnvironmentAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);

        // Act
        var result = await _service.GetAllAsync(environment);

        // Assert
        result.Should().HaveCount(1);
        result.First().Environment.Should().Be("staging");
        _queryRepositoryMock.Verify(r => r.GetByEnvironmentAsync(environment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnableFeatureAsync_EnablesFlag_Successfully()
    {
        // Arrange
        var flagId = Guid.NewGuid();
        var flag = new FeatureFlag { Id = flagId, IsEnabled = false };

        _queryRepositoryMock.Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flag);

        // Act
        await _service.EnableFeatureAsync(flagId);

        // Assert
        flag.IsEnabled.Should().BeTrue();
        _queryRepositoryMock.Verify(r => r.UpdateAsync(flag, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisableFeatureAsync_DisablesFlag_Successfully()
    {
        // Arrange
        var flagId = Guid.NewGuid();
        var flag = new FeatureFlag { Id = flagId, IsEnabled = true };

        _queryRepositoryMock.Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flag);

        // Act
        await _service.DisableFeatureAsync(flagId);

        // Assert
        flag.IsEnabled.Should().BeFalse();
        _queryRepositoryMock.Verify(r => r.UpdateAsync(flag, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRolloutPercentageAsync_UpdatesPercentage_Successfully()
    {
        // Arrange
        var flagId = Guid.NewGuid();
        var flag = new FeatureFlag { Id = flagId, RolloutPercentage = 0 };

        _queryRepositoryMock.Setup(r => r.GetByIdAsync(flagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flag);

        // Act
        await _service.UpdateRolloutPercentageAsync(flagId, 75);

        // Assert
        flag.RolloutPercentage.Should().Be(75);
        _queryRepositoryMock.Verify(r => r.UpdateAsync(flag, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTargetingRuleAsync_CreatesRule_Successfully()
    {
        // Arrange
        var featureFlagId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var flag = new FeatureFlag
        {
            Id = featureFlagId,
            Key = "advanced-inspections"
        };
        var request = new TestTargetingRequest
        {
            FeatureFlagId = featureFlagId,
            FeatureKey = flag.Key,
            TargetType = "tenant",
            TargetIdentifier = "tenant-alpha",
            IsEnabled = true,
            RolloutPercentage = 65,
            CustomValue = "enabled",
            Priority = 9,
            Metadata = new Dictionary<string, object>
            {
                ["source"] = "unit-test",
                ["plan"] = "enterprise"
            }
        };

        _queryRepositoryMock.Setup(r => r.GetByIdAsync(featureFlagId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flag);
        _targetingRepositoryMock.Setup(r => r.CreateTargetAsync(It.IsAny<FeatureFlagTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetId);

        // Act
        var result = await _service.CreateTargetingRuleAsync(request);

        // Assert
        result.Should().Be(targetId);
        _targetingRepositoryMock.Verify(r => r.CreateTargetAsync(It.Is<FeatureFlagTarget>(target =>
            target.FeatureFlagId == featureFlagId &&
            target.TargetType == "tenant" &&
            target.TargetIdentifier == "tenant-alpha" &&
            target.IsEnabled &&
            target.RolloutPercentage == 65 &&
            target.CustomValue == "enabled" &&
            target.Priority == 9 &&
            target.Metadata != null &&
            target.Metadata.Contains("unit-test") &&
            target.Metadata.Contains("enterprise")
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTargetingRuleAsync_DeletesRule_Successfully()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var target = new FeatureFlagTarget { Id = targetId };

        _targetingRepositoryMock.Setup(r => r.GetTargetByIdAsync(targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        // Act
        await _service.DeleteTargetingRuleAsync(targetId);

        // Assert
        _targetingRepositoryMock.Verify(r => r.DeleteTargetAsync(targetId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class TestTargetingRequest : FeatureFlagTargetingRequest;
}
