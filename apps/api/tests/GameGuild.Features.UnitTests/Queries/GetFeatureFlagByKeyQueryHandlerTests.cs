using FluentAssertions;
using GameGuild;
using GameGuild.Features;
using Moq;
using Xunit;

namespace GameGuild.Tests.Features.Unit.Queries;

/// <summary>
/// Unit tests for GetFeatureFlagByKeyQueryHandler
/// </summary>
public class GetFeatureFlagByKeyQueryHandlerTests
{
    private readonly Mock<IFeatureFlagQueryRepository> _mockRepository;
    private readonly GetFeatureFlagByKeyQueryHandler _handler;

    public GetFeatureFlagByKeyQueryHandlerTests()
    {
        _mockRepository = new Mock<IFeatureFlagQueryRepository>();
        _handler = new GetFeatureFlagByKeyQueryHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_WhenFeatureExists_ReturnsDto()
    {
        // Arrange
        var query = new GetFeatureFlagByKeyQuery { Key = "existing-feature" };
        var featureFlag = CreateFeatureFlag("existing-feature");

        _mockRepository
            .Setup(r => r.GetByKeyAsync("existing-feature", It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlag);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Key.Should().Be("existing-feature");
    }

    [Fact]
    public async Task Handle_WhenFeatureDoesNotExist_ReturnsNull()
    {
        // Arrange
        var query = new GetFeatureFlagByKeyQuery { Key = "non-existing-feature" };

        _mockRepository
            .Setup(r => r.GetByKeyAsync("non-existing-feature", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MapsEntityToDto_Correctly()
    {
        // Arrange
        var featureId = Guid.NewGuid();
        var query = new GetFeatureFlagByKeyQuery { Key = "test-feature" };
        var featureFlag = new FeatureFlag
        {
            Id = featureId,
            Key = "test-feature",
            Name = "Test Feature",
            Description = "A test feature",
            IsEnabled = true,
            Type = FeatureFlagType.Toggle,
            Environment = "production"
        };
        typeof(EntityBase).GetProperty(nameof(EntityBase.CreatedAt))!.SetValue(featureFlag, DateTime.UtcNow);

        _mockRepository
            .Setup(r => r.GetByKeyAsync("test-feature", It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlag);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(featureId);
        result.Key.Should().Be("test-feature");
        result.Name.Should().Be("Test Feature");
        result.Description.Should().Be("A test feature");
        result.IsEnabled.Should().BeTrue();
        result.Type.Should().Be(FeatureFlagType.Toggle);
        result.Environment.Should().Be("production");
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectKey()
    {
        // Arrange
        var query = new GetFeatureFlagByKeyQuery { Key = "specific-key" };

        _mockRepository
            .Setup(r => r.GetByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByKeyAsync("specific-key", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        // Arrange
        var query = new GetFeatureFlagByKeyQuery { Key = "test-key" };
        using var cts = new CancellationTokenSource();

        _mockRepository
            .Setup(r => r.GetByKeyAsync(It.IsAny<string>(), cts.Token))
            .ReturnsAsync((FeatureFlag?)null);

        // Act
        await _handler.Handle(query, cts.Token);

        // Assert
        _mockRepository.Verify(r => r.GetByKeyAsync("test-key", cts.Token), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_MapsIsEnabled_Correctly(bool isEnabled)
    {
        // Arrange
        var query = new GetFeatureFlagByKeyQuery { Key = "toggle-feature" };
        var featureFlag = CreateFeatureFlag("toggle-feature", isEnabled);

        _mockRepository
            .Setup(r => r.GetByKeyAsync("toggle-feature", It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlag);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IsEnabled.Should().Be(isEnabled);
    }

    private static FeatureFlag CreateFeatureFlag(string key, bool isEnabled = true)
    {
        var flag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = $"Feature {key}",
            IsEnabled = isEnabled,
            Type = FeatureFlagType.Toggle
        };
        typeof(EntityBase).GetProperty(nameof(EntityBase.CreatedAt))!.SetValue(flag, DateTime.UtcNow);
        return flag;
    }
}
