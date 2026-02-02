using FluentAssertions;
using GameGuild.Features;
using Moq;
using Xunit;

namespace GameGuild.Tests.Features.Unit.Queries;

/// <summary>
/// Unit tests for EvaluateFeatureQueryHandler
/// </summary>
public class EvaluateFeatureQueryHandlerTests
{
    private readonly Mock<IFeatureFlagEvaluationService> _mockEvaluationService;
    private readonly EvaluateFeatureQueryHandler _handler;

    public EvaluateFeatureQueryHandlerTests()
    {
        _mockEvaluationService = new Mock<IFeatureFlagEvaluationService>();
        _handler = new EvaluateFeatureQueryHandler(_mockEvaluationService.Object);
    }

    [Fact]
    public async Task Handle_WhenFeatureEnabled_ReturnsEnabledResult()
    {
        // Arrange
        var query = new EvaluateFeatureQuery
        {
            FeatureKey = "test-feature",
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid()
        };

        var expectedResult = new FeatureEvaluationResult
        {
            FeatureKey = "test-feature",
            IsEnabled = true,
            Reason = "Feature enabled globally"
        };

        _mockEvaluationService
            .Setup(s => s.EvaluateAsync(query.FeatureKey, It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsEnabled.Should().BeTrue();
        result.FeatureKey.Should().Be("test-feature");
    }

    [Fact]
    public async Task Handle_WhenFeatureDisabled_ReturnsDisabledResult()
    {
        // Arrange
        var query = new EvaluateFeatureQuery
        {
            FeatureKey = "disabled-feature",
            UserId = Guid.NewGuid()
        };

        var expectedResult = new FeatureEvaluationResult
        {
            FeatureKey = "disabled-feature",
            IsEnabled = false,
            Reason = "Feature disabled globally"
        };

        _mockEvaluationService
            .Setup(s => s.EvaluateAsync(query.FeatureKey, It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithUserId_PassesUserIdToContext()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new EvaluateFeatureQuery
        {
            FeatureKey = "user-feature",
            UserId = userId
        };

        FeatureContext? capturedContext = null;
        _mockEvaluationService
            .Setup(s => s.EvaluateAsync(query.FeatureKey, It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .Callback<string, FeatureContext, CancellationToken>((_, ctx, _) => capturedContext = ctx)
            .ReturnsAsync(new FeatureEvaluationResult());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedContext.Should().NotBeNull();
        capturedContext!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WithTenantId_PassesTenantIdToContext()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var query = new EvaluateFeatureQuery
        {
            FeatureKey = "tenant-feature",
            TenantId = tenantId
        };

        FeatureContext? capturedContext = null;
        _mockEvaluationService
            .Setup(s => s.EvaluateAsync(query.FeatureKey, It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .Callback<string, FeatureContext, CancellationToken>((_, ctx, _) => capturedContext = ctx)
            .ReturnsAsync(new FeatureEvaluationResult());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedContext.Should().NotBeNull();
        capturedContext!.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task Handle_WithEnvironment_PassesEnvironmentToContext()
    {
        // Arrange
        var query = new EvaluateFeatureQuery
        {
            FeatureKey = "env-feature",
            Environment = "staging"
        };

        FeatureContext? capturedContext = null;
        _mockEvaluationService
            .Setup(s => s.EvaluateAsync(query.FeatureKey, It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .Callback<string, FeatureContext, CancellationToken>((_, ctx, _) => capturedContext = ctx)
            .ReturnsAsync(new FeatureEvaluationResult());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedContext.Should().NotBeNull();
        capturedContext!.Environment.Should().Be("staging");
    }

    [Fact]
    public async Task Handle_WithNullEnvironment_DefaultsToProduction()
    {
        // Arrange
        var query = new EvaluateFeatureQuery
        {
            FeatureKey = "default-env-feature",
            Environment = null
        };

        FeatureContext? capturedContext = null;
        _mockEvaluationService
            .Setup(s => s.EvaluateAsync(query.FeatureKey, It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .Callback<string, FeatureContext, CancellationToken>((_, ctx, _) => capturedContext = ctx)
            .ReturnsAsync(new FeatureEvaluationResult());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedContext.Should().NotBeNull();
        capturedContext!.Environment.Should().Be("production");
    }

    [Fact]
    public async Task Handle_WithPermissions_PassesPermissionsToContext()
    {
        // Arrange
        var permissions = new List<string> { "read", "write", "admin" };
        var query = new EvaluateFeatureQuery
        {
            FeatureKey = "permission-feature",
            Permissions = permissions
        };

        FeatureContext? capturedContext = null;
        _mockEvaluationService
            .Setup(s => s.EvaluateAsync(query.FeatureKey, It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .Callback<string, FeatureContext, CancellationToken>((_, ctx, _) => capturedContext = ctx)
            .ReturnsAsync(new FeatureEvaluationResult());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedContext.Should().NotBeNull();
        capturedContext!.Permissions.Should().BeEquivalentTo(permissions);
    }

    [Fact]
    public async Task Handle_WithCustomAttributes_PassesAttributesToContext()
    {
        // Arrange
        var customAttributes = new Dictionary<string, object>
        {
            { "region", "us-east" },
            { "tier", "premium" }
        };
        var query = new EvaluateFeatureQuery
        {
            FeatureKey = "custom-feature",
            CustomAttributes = customAttributes
        };

        FeatureContext? capturedContext = null;
        _mockEvaluationService
            .Setup(s => s.EvaluateAsync(query.FeatureKey, It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .Callback<string, FeatureContext, CancellationToken>((_, ctx, _) => capturedContext = ctx)
            .ReturnsAsync(new FeatureEvaluationResult());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedContext.Should().NotBeNull();
        capturedContext!.CustomAttributes.Should().BeEquivalentTo(customAttributes);
    }

    [Fact]
    public async Task Handle_WithNullOptionalParams_UsesEmptyDefaults()
    {
        // Arrange
        var query = new EvaluateFeatureQuery
        {
            FeatureKey = "minimal-feature",
            Permissions = null,
            CustomAttributes = null
        };

        FeatureContext? capturedContext = null;
        _mockEvaluationService
            .Setup(s => s.EvaluateAsync(query.FeatureKey, It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .Callback<string, FeatureContext, CancellationToken>((_, ctx, _) => capturedContext = ctx)
            .ReturnsAsync(new FeatureEvaluationResult());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedContext.Should().NotBeNull();
        capturedContext!.Permissions.Should().BeEmpty();
        capturedContext.CustomAttributes.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithRolloutPercentage_ReturnsRolloutInfo()
    {
        // Arrange
        var query = new EvaluateFeatureQuery
        {
            FeatureKey = "rollout-feature"
        };

        var expectedResult = new FeatureEvaluationResult
        {
            FeatureKey = "rollout-feature",
            IsEnabled = true,
            RolloutPercentage = 50,
            IsTargeted = false
        };

        _mockEvaluationService
            .Setup(s => s.EvaluateAsync(query.FeatureKey, It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.RolloutPercentage.Should().Be(50);
        result.IsTargeted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenTargeted_ReturnsTargetingInfo()
    {
        // Arrange
        var query = new EvaluateFeatureQuery
        {
            FeatureKey = "targeted-feature",
            UserId = Guid.NewGuid()
        };

        var expectedResult = new FeatureEvaluationResult
        {
            FeatureKey = "targeted-feature",
            IsEnabled = true,
            IsTargeted = true,
            TargetType = "user"
        };

        _mockEvaluationService
            .Setup(s => s.EvaluateAsync(query.FeatureKey, It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsTargeted.Should().BeTrue();
        result.TargetType.Should().Be("user");
    }
}
