using System.Globalization;
using FluentAssertions;
using GameGuild.Localization;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace GameGuild.Tests.Localization.Unit.Services;

/// <summary>
/// Tests for LocalizationContext to verify HTTP header reading behavior.
/// </summary>
public class LocalizationContextHttpTests
{
    [Fact]
    public void Constructor_ReadsAcceptLanguageHeader()
    {
        // Arrange
        var httpContext = CreateHttpContextWithHeaders(acceptLanguage: "fr-FR,fr;q=0.9,en;q=0.8");
        var httpContextAccessor = CreateHttpContextAccessor(httpContext);

        // Act
        var context = new LocalizationContext(httpContextAccessor);

        // Assert
        context.CurrentCulture.Name.Should().Be("fr-FR");
        context.CurrentUiCulture.Name.Should().Be("fr-FR");
    }

    [Fact]
    public void Constructor_HandlesNullHttpContextAccessor_Gracefully()
    {
        IHttpContextAccessor httpContextAccessor = null!;

        var context = new LocalizationContext(httpContextAccessor, userPreferenceProvider: null);

        context.CurrentCulture.Name.Should().Be("en-US");
        context.CurrentUiCulture.Name.Should().Be("en-US");
        context.CurrentTimeZone.Id.Should().Be("UTC");
    }

    [Fact]
    public void Constructor_ParsesPrimaryLanguageFromAcceptLanguageHeader()
    {
        // Arrange
        var httpContext = CreateHttpContextWithHeaders(acceptLanguage: "de-DE,de;q=0.9,en-US;q=0.8,en;q=0.7");
        var httpContextAccessor = CreateHttpContextAccessor(httpContext);

        // Act
        var context = new LocalizationContext(httpContextAccessor);

        // Assert - should pick the first (primary) language
        context.CurrentCulture.Name.Should().Be("de-DE");
    }

    [Fact]
    public void Constructor_ReadsXTimezoneHeader()
    {
        // Arrange
        var httpContext = CreateHttpContextWithHeaders(timezone: "America/New_York");
        var httpContextAccessor = CreateHttpContextAccessor(httpContext);

        // Act
        var context = new LocalizationContext(httpContextAccessor);

        // Assert - America/New_York may map to "America/New_York" on Linux or "Eastern Standard Time" on Windows
        (context.CurrentTimeZone.Id.Contains("America/New_York") ||
         context.CurrentTimeZone.Id.Contains("Eastern")).Should().BeTrue();
    }

    [Fact]
    public void Constructor_DefaultsToEnUS_WhenNoHeader()
    {
        // Arrange
        var httpContext = CreateHttpContextWithHeaders();
        var httpContextAccessor = CreateHttpContextAccessor(httpContext);

        // Act
        var context = new LocalizationContext(httpContextAccessor);

        // Assert
        context.CurrentCulture.Name.Should().Be("en-US");
        context.CurrentUiCulture.Name.Should().Be("en-US");
    }

    [Fact]
    public void Constructor_DefaultsToUtc_WhenNoTimezoneHeader()
    {
        // Arrange
        var httpContext = CreateHttpContextWithHeaders();
        var httpContextAccessor = CreateHttpContextAccessor(httpContext);

        // Act
        var context = new LocalizationContext(httpContextAccessor);

        // Assert
        context.CurrentTimeZone.Id.Should().Be("UTC");
    }

    [Fact]
    public void Constructor_UserPreferenceTakesPrecedenceOverHeader()
    {
        // Arrange
        var httpContext = CreateHttpContextWithHeaders(acceptLanguage: "fr-FR");
        var httpContextAccessor = CreateHttpContextAccessor(httpContext);

        var userPreferences = new Mock<IUserLocalizationPreferenceProvider>();
        userPreferences.Setup(x => x.GetPreferredCulture()).Returns(CultureInfo.GetCultureInfo("ja-JP"));
        userPreferences.Setup(x => x.GetPreferredTimeZone()).Returns(TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"));

        // Act
        var context = new LocalizationContext(httpContextAccessor, userPreferences.Object);

        // Assert - User preference wins
        context.CurrentCulture.Name.Should().Be("ja-JP");
    }

    [Fact]
    public void Constructor_FallsBackToHeader_WhenUserPreferenceIsNull()
    {
        // Arrange
        var httpContext = CreateHttpContextWithHeaders(acceptLanguage: "es-ES");
        var httpContextAccessor = CreateHttpContextAccessor(httpContext);

        var userPreferences = new Mock<IUserLocalizationPreferenceProvider>();
        userPreferences.Setup(x => x.GetPreferredCulture()).Returns((CultureInfo?)null);
        userPreferences.Setup(x => x.GetPreferredTimeZone()).Returns((TimeZoneInfo?)null);

        // Act
        var context = new LocalizationContext(httpContextAccessor, userPreferences.Object);

        // Assert - Falls back to header
        context.CurrentCulture.Name.Should().Be("es-ES");
    }

    [Fact]
    public void Constructor_HandlesInvalidCultureGracefully()
    {
        // Arrange
        var httpContext = CreateHttpContextWithHeaders(acceptLanguage: "invalid-culture-code");
        var httpContextAccessor = CreateHttpContextAccessor(httpContext);

        // Act
        var context = new LocalizationContext(httpContextAccessor);

        // Assert - Falls back to default
        context.CurrentCulture.Name.Should().Be("en-US");
    }

    [Fact]
    public void Constructor_HandlesInvalidTimezoneGracefully()
    {
        // Arrange
        var httpContext = CreateHttpContextWithHeaders(timezone: "Invalid/Timezone");
        var httpContextAccessor = CreateHttpContextAccessor(httpContext);

        // Act
        var context = new LocalizationContext(httpContextAccessor);

        // Assert - Falls back to UTC
        context.CurrentTimeZone.Id.Should().Be("UTC");
    }

    [Fact]
    public void TestingConstructor_SetsCultureAndTimeZone()
    {
        // Arrange
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");

        // Act
        var context = new LocalizationContext(culture, timeZone);

        // Assert
        context.CurrentCulture.Name.Should().Be("pt-BR");
        context.CurrentTimeZone.Id.Should().Be("E. South America Standard Time");
    }

    [Fact]
    public void DefaultConstructor_UsesDefaults()
    {
        // Act
        var context = new LocalizationContext();

        // Assert
        context.CurrentCulture.Name.Should().Be("en-US");
        context.CurrentTimeZone.Id.Should().Be("UTC");
    }

    [Fact]
    public void TestingConstructor_UsesDefaults_WhenCultureAndTimeZoneAreNull()
    {
        CultureInfo culture = null!;
        TimeZoneInfo timeZone = null!;

        var context = new LocalizationContext(culture, timeZone);

        context.CurrentCulture.Name.Should().Be("en-US");
        context.CurrentUiCulture.Name.Should().Be("en-US");
        context.CurrentTimeZone.Id.Should().Be("UTC");
    }

    [Fact]
    public void ConvertToLocalTime_ConvertsCorrectly()
    {
        // Arrange
        var culture = CultureInfo.GetCultureInfo("en-US");
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        var context = new LocalizationContext(culture, timeZone);
        var utcTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var localTime = context.ConvertToLocalTime(utcTime);

        // Assert - Pacific is UTC-8 in winter
        localTime.Should().Be(new DateTime(2024, 1, 15, 4, 0, 0));
    }

    [Fact]
    public void ConvertToUtcTime_ConvertsCorrectly()
    {
        // Arrange
        var culture = CultureInfo.GetCultureInfo("en-US");
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        var context = new LocalizationContext(culture, timeZone);
        var localTime = new DateTime(2024, 1, 15, 4, 0, 0);

        // Act
        var utcTime = context.ConvertToUtcTime(localTime);

        // Assert - Pacific is UTC-8 in winter
        utcTime.Should().Be(new DateTime(2024, 1, 15, 12, 0, 0));
    }

    [Fact]
    public void GetCurrentLocalTime_ReturnsTimeInLocalTimeZone()
    {
        // Arrange
        var culture = CultureInfo.GetCultureInfo("en-US");
        var timeZone = TimeZoneInfo.Utc;
        var context = new LocalizationContext(culture, timeZone);

        // Act
        var localTime = context.GetCurrentLocalTime();

        // Assert - should be close to UTC now
        var utcNow = DateTime.UtcNow;
        localTime.Should().BeCloseTo(utcNow, TimeSpan.FromSeconds(1));
    }

    private static DefaultHttpContext CreateHttpContextWithHeaders(
        string? acceptLanguage = null,
        string? timezone = null)
    {
        var httpContext = new DefaultHttpContext();

        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            httpContext.Request.Headers.AcceptLanguage = acceptLanguage;
        }

        if (!string.IsNullOrEmpty(timezone))
        {
            httpContext.Request.Headers["X-Timezone"] = timezone;
        }

        return httpContext;
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(HttpContext httpContext)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(httpContext);
        return accessor.Object;
    }
}
