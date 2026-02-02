using FluentAssertions;
using GameGuild.Features;
using Moq;
using Xunit;

namespace GameGuild.Tests.Features.Unit.Queries;

/// <summary>
/// Unit tests for BulkEvaluateFeaturesQueryHandler
/// </summary>
public class BulkEvaluateFeaturesQueryHandlerTests
{
    private readonly Mock<IFeatureFlagEvaluationService> _mockEvaluationService;
    private readonly BulkEvaluateFeaturesQueryHandler _handler;

    public BulkEvaluateFeaturesQueryHandlerTests()
    {
        _mockEvaluationService = new Mock<IFeatureFlagEvaluationService>();
        _handler = new BulkEvaluateFeaturesQueryHandler(_mockEvaluationService.Object);
    }

    [Fact]
    public async Task Handle_WhenSingleFeature_ReturnsResult()
    {
        // Arrange
        var context = new FeatureContext { UserId = Guid.NewGuid() };
        var query = new BulkEvaluateFeaturesQuery
        {
            FeatureKeys = new[] { "feature-1" },
            Context = context
        };

        var result1 = new FeatureEvaluationResult { FeatureKey = "feature-1", IsEnabled = true };

        _mockEvaluationService
            .Setup(s => s.EvaluateAsync("feature-1", context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result1);

        // Act
        var response = await _handler.Handle(query, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Results.Should().HaveCount(1);
        response.Results["feature-1"].IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenMultipleFeatures_ReturnsAllResults()
    {
        // Arrange
        var context = new FeatureContext { UserId = Guid.NewGuid() };
        var query = new BulkEvaluateFeaturesQuery
        {
            FeatureKeys = new[] { "feature-1", "feature-2", "feature-3" },
            Context = context
        };

        _mockEvaluationService
            .Setup(s => s.EvaluateAsync("feature-1", context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeatureEvaluationResult { FeatureKey = "feature-1", IsEnabled = true });

        _mockEvaluationService
            .Setup(s => s.EvaluateAsync("feature-2", context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeatureEvaluationResult { FeatureKey = "feature-2", IsEnabled = false });

        _mockEvaluationService
            .Setup(s => s.EvaluateAsync("feature-3", context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeatureEvaluationResult { FeatureKey = "feature-3", IsEnabled = true });

        // Act
        var response = await _handler.Handle(query, CancellationToken.None);

        // Assert
        response.Results.Should().HaveCount(3);
        response.Results["feature-1"].IsEnabled.Should().BeTrue();
        response.Results["feature-2"].IsEnabled.Should().BeFalse();
        response.Results["feature-3"].IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EvaluatesAllFeaturesWithSameContext()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var context = new FeatureContext
        {
            UserId = userId,
            TenantId = tenantId,
            Environment = "staging"
        };
        var query = new BulkEvaluateFeaturesQuery
        {
            FeatureKeys = new[] { "feature-1", "feature-2" },
            Context = context
        };

        _mockEvaluationService
            .Setup(s => s.EvaluateAsync(It.IsAny<string>(), context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeatureEvaluationResult());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockEvaluationService.Verify(
            s => s.EvaluateAsync("feature-1", context, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockEvaluationService.Verify(
            s => s.EvaluateAsync("feature-2", context, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmptyFeatureKeys_ReturnsEmptyResults()
    {
        // Arrange
        var context = new FeatureContext();
        var query = new BulkEvaluateFeaturesQuery
        {
            FeatureKeys = Array.Empty<string>(),
            Context = context
        };

        // Act
        var response = await _handler.Handle(query, CancellationToken.None);

        // Assert
        response.Results.Should().BeEmpty();
        _mockEvaluationService.Verify(
            s => s.EvaluateAsync(It.IsAny<string>(), It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SetsEvaluatedAtTimestamp()
    {
        // Arrange
        var beforeTime = DateTime.UtcNow;
        var context = new FeatureContext();
        var query = new BulkEvaluateFeaturesQuery
        {
            FeatureKeys = new[] { "feature-1" },
            Context = context
        };

        _mockEvaluationService
            .Setup(s => s.EvaluateAsync(It.IsAny<string>(), It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeatureEvaluationResult());

        // Act
        var response = await _handler.Handle(query, CancellationToken.None);
        var afterTime = DateTime.UtcNow;

        // Assert
        response.EvaluatedAt.Should().BeOnOrAfter(beforeTime);
        response.EvaluatedAt.Should().BeOnOrBefore(afterTime);
    }

    [Fact]
    public async Task Handle_WithMixedEnabledDisabled_PreservesIndividualStates()
    {
        // Arrange
        var context = new FeatureContext { UserId = Guid.NewGuid() };
        var featureKeys = Enumerable.Range(1, 5).Select(i => $"feature-{i}").ToArray();
        var query = new BulkEvaluateFeaturesQuery
        {
            FeatureKeys = featureKeys,
            Context = context
        };

        // Enable odd features, disable even
        foreach (var key in featureKeys)
        {
            var index = int.Parse(key.Split('-')[1]);
            var isEnabled = index % 2 == 1;

            _mockEvaluationService
                .Setup(s => s.EvaluateAsync(key, context, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FeatureEvaluationResult { FeatureKey = key, IsEnabled = isEnabled });
        }

        // Act
        var response = await _handler.Handle(query, CancellationToken.None);

        // Assert
        response.Results["feature-1"].IsEnabled.Should().BeTrue();
        response.Results["feature-2"].IsEnabled.Should().BeFalse();
        response.Results["feature-3"].IsEnabled.Should().BeTrue();
        response.Results["feature-4"].IsEnabled.Should().BeFalse();
        response.Results["feature-5"].IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PreservesMetadataFromIndividualEvaluations()
    {
        // Arrange
        var context = new FeatureContext();
        var query = new BulkEvaluateFeaturesQuery
        {
            FeatureKeys = new[] { "meta-feature" },
            Context = context
        };

        var metadata = new Dictionary<string, object>
        {
            { "source", "test" },
            { "version", 1 }
        };

        _mockEvaluationService
            .Setup(s => s.EvaluateAsync("meta-feature", context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeatureEvaluationResult
            {
                FeatureKey = "meta-feature",
                IsEnabled = true,
                Metadata = metadata
            });

        // Act
        var response = await _handler.Handle(query, CancellationToken.None);

        // Assert
        response.Results["meta-feature"].Metadata.Should().BeEquivalentTo(metadata);
    }
}
