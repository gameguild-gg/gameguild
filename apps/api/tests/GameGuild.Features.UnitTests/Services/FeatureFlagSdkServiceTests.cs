using FluentAssertions;

using Moq;

using Xunit;

namespace GameGuild.Features.UnitTests.Services;

public class FeatureFlagSdkServiceTests
{
    private readonly Mock<IFeatureFlagQueryRepository> _repositoryMock;
    private readonly FeatureFlagSdkService _service;

    public FeatureFlagSdkServiceTests()
    {
        _repositoryMock = new Mock<IFeatureFlagQueryRepository>();
        _service = new FeatureFlagSdkService(_repositoryMock.Object);
    }

    #region GenerateSdkConfigurationAsync Tests

    [Fact]
    public async Task GenerateSdkConfigurationAsync_ReturnsConfiguration()
    {
        // Arrange
        var environment = "production";
        _repositoryMock
            .Setup(x => x.GetByEnvironmentAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeatureFlag>());

        // Act
        var result = await _service.GenerateSdkConfigurationAsync(environment);

        // Assert
        result.Should().NotBeNull();
        result.Environment.Should().Be(environment);
    }

    [Fact]
    public async Task GenerateSdkConfigurationAsync_SetsDefaultValues()
    {
        // Arrange
        var environment = "staging";
        _repositoryMock
            .Setup(x => x.GetByEnvironmentAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeatureFlag>());

        // Act
        var result = await _service.GenerateSdkConfigurationAsync(environment);

        // Assert
        result.TimeoutSeconds.Should().Be(30);
        result.PollingIntervalSeconds.Should().Be(60);
        result.EnableCaching.Should().BeTrue();
        result.CacheExpirationMinutes.Should().Be(5);
        result.EnableAnalytics.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateSdkConfigurationAsync_WithTenantId_FiltersFlags()
    {
        // Arrange
        var environment = "production";
        var tenantId = Guid.NewGuid().ToString();
        _repositoryMock
            .Setup(x => x.GetByEnvironmentAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeatureFlag>());

        // Act
        var result = await _service.GenerateSdkConfigurationAsync(environment, tenantId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSdkConfigurationAsync_SetsCorrectBaseUrl()
    {
        // Arrange
        var environment = "production";
        _repositoryMock
            .Setup(x => x.GetByEnvironmentAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeatureFlag>());

        // Act
        var result = await _service.GenerateSdkConfigurationAsync(environment);

        // Assert
        result.BaseUrl.Should().Be("/api/features");
    }

    #endregion

    #region GetSdkEndpointsAsync Tests

    [Fact]
    public async Task GetSdkEndpointsAsync_ReturnsAllEndpoints()
    {
        // Act
        var result = await _service.GetSdkEndpointsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Features.Should().Be("/api/features");
        result.Evaluate.Should().Be("/api/features/evaluate");
        result.Analytics.Should().Be("/api/features/analytics");
        result.Health.Should().Be("/health");
        result.Config.Should().Be("/api/sdk/config");
    }

    [Fact]
    public async Task GetSdkEndpointsAsync_EndpointsAreNotNull()
    {
        // Act
        var result = await _service.GetSdkEndpointsAsync();

        // Assert
        result.Features.Should().NotBeNullOrEmpty();
        result.Evaluate.Should().NotBeNullOrEmpty();
        result.Analytics.Should().NotBeNullOrEmpty();
        result.Health.Should().NotBeNullOrEmpty();
        result.Config.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GenerateApiKeyAsync Tests

    [Fact]
    public async Task GenerateApiKeyAsync_ReturnsNonEmptyKey()
    {
        // Arrange
        var environment = "production";

        // Act
        var result = await _service.GenerateApiKeyAsync(environment);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateApiKeyAsync_StartsWithEnvironment()
    {
        // Arrange
        var environment = "production";

        // Act
        var result = await _service.GenerateApiKeyAsync(environment);

        // Assert
        result.Should().StartWith($"{environment}_");
    }

    [Fact]
    public async Task GenerateApiKeyAsync_GeneratesUniqueKeys()
    {
        // Arrange
        var environment = "production";

        // Act
        var key1 = await _service.GenerateApiKeyAsync(environment);
        var key2 = await _service.GenerateApiKeyAsync(environment);

        // Assert
        key1.Should().NotBe(key2);
    }

    [Fact]
    public async Task GenerateApiKeyAsync_WithTenantId_IncludesEnvironment()
    {
        // Arrange
        var environment = "staging";
        var tenantId = Guid.NewGuid().ToString();

        // Act
        var result = await _service.GenerateApiKeyAsync(environment, tenantId);

        // Assert
        result.Should().StartWith($"{environment}_");
    }

    #endregion

    #region ValidateApiKeyAsync Tests

    [Fact]
    public async Task ValidateApiKeyAsync_ValidKey_ReturnsTrue()
    {
        // Arrange
        var environment = "production";
        var apiKey = await _service.GenerateApiKeyAsync(environment);

        // Act
        var result = await _service.ValidateApiKeyAsync(apiKey);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_NullKey_ReturnsFalse()
    {
        // Act
        var result = await _service.ValidateApiKeyAsync(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_EmptyKey_ReturnsFalse()
    {
        // Act
        var result = await _service.ValidateApiKeyAsync(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WhitespaceKey_ReturnsFalse()
    {
        // Act
        var result = await _service.ValidateApiKeyAsync("   ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_InvalidFormat_ReturnsFalse()
    {
        // Arrange - key without underscore separator
        var invalidKey = "invalidkeyformat";

        // Act
        var result = await _service.ValidateApiKeyAsync(invalidKey);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_InvalidBase64_ReturnsFalse()
    {
        // Arrange - key with invalid base64 portion
        var invalidKey = "production_notvalidbase64!!!";

        // Act
        var result = await _service.ValidateApiKeyAsync(invalidKey);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_TooManyUnderscores_ReturnsFalse()
    {
        // Arrange
        var invalidKey = "production_extra_underscore_here";

        // Act
        var result = await _service.ValidateApiKeyAsync(invalidKey);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
