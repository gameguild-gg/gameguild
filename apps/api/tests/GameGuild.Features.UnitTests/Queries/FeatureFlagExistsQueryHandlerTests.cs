using FluentAssertions;
using GameGuild.Features;
using Moq;
using Xunit;

namespace GameGuild.Tests.Features.Unit.Queries;

/// <summary>
/// Unit tests for FeatureFlagExistsQueryHandler
/// </summary>
public class FeatureFlagExistsQueryHandlerTests
{
    private readonly Mock<IFeatureFlagQueryRepository> _mockRepository;
    private readonly FeatureFlagExistsQueryHandler _handler;

    public FeatureFlagExistsQueryHandlerTests()
    {
        _mockRepository = new Mock<IFeatureFlagQueryRepository>();
        _handler = new FeatureFlagExistsQueryHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_WhenFeatureExists_ReturnsTrue()
    {
        // Arrange
        var query = new FeatureFlagExistsQuery { Key = "existing-feature" };
        var featureFlag = new FeatureFlag { Key = "existing-feature", Name = "Test Feature" };

        _mockRepository
            .Setup(r => r.GetByKeyAsync("existing-feature", It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlag);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenFeatureDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var query = new FeatureFlagExistsQuery { Key = "non-existing-feature" };

        _mockRepository
            .Setup(r => r.GetByKeyAsync("non-existing-feature", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectKey()
    {
        // Arrange
        var query = new FeatureFlagExistsQuery { Key = "test-key" };

        _mockRepository
            .Setup(r => r.GetByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            r => r.GetByKeyAsync("test-key", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        // Arrange
        var query = new FeatureFlagExistsQuery { Key = "test-key" };
        using var cts = new CancellationTokenSource();

        _mockRepository
            .Setup(r => r.GetByKeyAsync(It.IsAny<string>(), cts.Token))
            .ReturnsAsync((FeatureFlag?)null);

        // Act
        await _handler.Handle(query, cts.Token);

        // Assert
        _mockRepository.Verify(r => r.GetByKeyAsync("test-key", cts.Token), Times.Once);
    }
}
