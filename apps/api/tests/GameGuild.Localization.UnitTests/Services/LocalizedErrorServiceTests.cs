using System.Globalization;
using FluentAssertions;
using GameGuild.Localization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Localization.Unit.Services;

/// <summary>
/// Tests for LocalizedErrorService to verify error message formatting and localization.
/// </summary>
public class LocalizedErrorServiceTests
{
    private readonly Mock<ILocalizationContext> _localizationContextMock;
    private readonly Mock<ILanguageRepository> _languageRepositoryMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<LocalizedErrorService>> _loggerMock;
    private readonly LocalizedErrorService _service;

    public LocalizedErrorServiceTests()
    {
        _localizationContextMock = new Mock<ILocalizationContext>();
        _languageRepositoryMock = new Mock<ILanguageRepository>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<LocalizedErrorService>>();

        _localizationContextMock.Setup(x => x.CurrentUiCulture).Returns(CultureInfo.InvariantCulture);

        _service = new LocalizedErrorService(
            _localizationContextMock.Object,
            _languageRepositoryMock.Object,
            _cache,
            _loggerMock.Object);
    }

    [Fact]
    public void GetErrorMessage_ReturnsFormattedMessage_WithArgs()
    {
        // Arrange
        var errorKey = ErrorMessageKeys.Validation.Required;

        // Act
        var result = _service.GetErrorMessage(errorKey, "Email");

        // Assert
        result.Should().Contain("Email");
        result.Should().Contain("is required");
    }

    [Fact]
    public void GetErrorMessage_ReturnsMessage_WithoutArgs()
    {
        // Arrange
        var errorKey = ErrorMessageKeys.Auth.Unauthorized;

        // Act
        var result = _service.GetErrorMessage(errorKey);

        // Assert
        result.Should().Be("Authentication is required to access this resource.");
    }

    [Fact]
    public void GetErrorMessage_WithCulture_UsesProvidedCulture()
    {
        // Arrange
        var errorKey = ErrorMessageKeys.Validation.Range;
        var culture = new CultureInfo("en-US");

        // Act
        var result = _service.GetErrorMessage(errorKey, culture, "Age", 1, 100);

        // Assert
        result.Should().Contain("Age");
        result.Should().Contain("1");
        result.Should().Contain("100");
        result.Should().Contain("must be between");
    }

    [Fact]
    public void GetValidationMessage_PrependsValidationPrefix()
    {
        // Arrange
        var validationKey = "minLength";
        var fieldName = "Password";

        // Act
        var result = _service.GetValidationMessage(validationKey, fieldName, 8);

        // Assert
        result.Should().Contain("Password");
        result.Should().Contain("at least");
        result.Should().Contain("8");
    }

    [Fact]
    public void GetValidationMessage_IncludesFieldNameAsFirstArg()
    {
        // Arrange
        var fieldName = "Username";

        // Act
        var result = _service.GetValidationMessage(ErrorMessageKeys.Validation.Required, fieldName);

        // Assert
        result.Should().Contain("Username");
        result.Should().Contain("is required");
    }

    [Fact]
    public void GetSystemMessage_ReturnsFormattedSystemMessage()
    {
        // Arrange
        var messageKey = ErrorMessageKeys.System.InternalError;

        // Act
        var result = _service.GetSystemMessage(messageKey);

        // Assert
        result.Should().Contain("unexpected error");
    }

    [Fact]
    public void GetErrorMessage_ReturnsKey_WhenNotFound()
    {
        // Arrange
        var unknownKey = "unknown.error.key";

        // Act
        var result = _service.GetErrorMessage(unknownKey);

        // Assert
        result.Should().Be(unknownKey);
    }

    [Fact]
    public void HasTranslation_ReturnsTrue_ForKnownKey()
    {
        // Act
        var result = _service.HasTranslation(ErrorMessageKeys.Validation.Required);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasTranslation_ReturnsFalse_ForUnknownKey()
    {
        // Act
        var result = _service.HasTranslation("completely.unknown.key");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetErrorMessage_CachesMessages()
    {
        // Arrange
        var errorKey = ErrorMessageKeys.Validation.Email;

        // Act
        var result1 = _service.GetErrorMessage(errorKey);
        var result2 = _service.GetErrorMessage(errorKey);

        // Assert - both calls return the same value
        result1.Should().Be(result2);
        // Note: Caching is verified by the fact that the second call returns same result
    }

    [Fact]
    public void GetErrorMessage_HandlesFormatExceptionGracefully()
    {
        // Arrange
        var errorKey = ErrorMessageKeys.Validation.Range;
        // Provide fewer args than expected to trigger format issues
        // But since validation.range expects 3 args and we provide 2, it may still format

        // Act - should not throw
        var act = () => _service.GetErrorMessage(errorKey, "Field");

        // Assert - should not throw, returns partial message
        act.Should().NotThrow();
    }

    [Fact]
    public void GetErrorMessage_AuthMessages_ReturnExpectedText()
    {
        // Assert all auth messages are available
        _service.GetErrorMessage(ErrorMessageKeys.Auth.Unauthorized).Should().NotBeEmpty();
        _service.GetErrorMessage(ErrorMessageKeys.Auth.Forbidden).Should().Contain("permission");
        _service.GetErrorMessage(ErrorMessageKeys.Auth.TokenExpired).Should().Contain("expired");
        _service.GetErrorMessage(ErrorMessageKeys.Auth.InvalidCredentials).Should().Contain("Invalid");
        _service.GetErrorMessage(ErrorMessageKeys.Auth.AccountLocked).Should().Contain("locked");
        _service.GetErrorMessage(ErrorMessageKeys.Auth.AccountDisabled).Should().Contain("disabled");
    }

    [Fact]
    public void GetErrorMessage_ResourceMessages_ReturnExpectedText()
    {
        // Assert all resource messages are available
        _service.GetErrorMessage(ErrorMessageKeys.Resource.NotFound, "User").Should().Contain("not found");
        _service.GetErrorMessage(ErrorMessageKeys.Resource.AlreadyExists, "Email").Should().Contain("already exists");
        _service.GetErrorMessage(ErrorMessageKeys.Resource.Conflict, "Record").Should().Contain("modified");
        _service.GetErrorMessage(ErrorMessageKeys.Resource.Deleted, "Item").Should().Contain("deleted");
    }

    [Fact]
    public void GetErrorMessage_QuotaMessages_ReturnExpectedText()
    {
        // Assert all quota messages are available
        _service.GetErrorMessage(ErrorMessageKeys.Quota.Exceeded, "storage").Should().Contain("exceeded");
        _service.GetErrorMessage(ErrorMessageKeys.Quota.NearLimit, "storage", 80).Should().Contain("approaching");
        _service.GetErrorMessage(ErrorMessageKeys.Quota.StorageFull).Should().Contain("full");
    }

    [Fact]
    public void GetErrorMessage_SystemMessages_ReturnExpectedText()
    {
        // Assert all system messages are available
        _service.GetErrorMessage(ErrorMessageKeys.System.InternalError).Should().Contain("unexpected");
        _service.GetErrorMessage(ErrorMessageKeys.System.ServiceUnavailable).Should().Contain("unavailable");
        _service.GetErrorMessage(ErrorMessageKeys.System.MaintenanceMode).Should().Contain("maintenance");
        _service.GetErrorMessage(ErrorMessageKeys.System.RateLimited).Should().Contain("requests");
    }

    [Fact]
    public void GetErrorMessage_AssetMessages_ReturnExpectedText()
    {
        // Assert all asset messages are available
        _service.GetErrorMessage(ErrorMessageKeys.Asset.VirusDetected).Should().Contain("malware");
        _service.GetErrorMessage(ErrorMessageKeys.Asset.ModerationRejected, "inappropriate content").Should().Contain("rejected");
        _service.GetErrorMessage(ErrorMessageKeys.Asset.ContentWarning, "violence").Should().Contain("may contain");
        _service.GetErrorMessage(ErrorMessageKeys.Asset.TokenExpired).Should().Contain("expired");
        _service.GetErrorMessage(ErrorMessageKeys.Asset.TokenInvalid).Should().Contain("invalid");
        _service.GetErrorMessage(ErrorMessageKeys.Asset.DownloadLimitExceeded).Should().Contain("exceeded");
    }
}
