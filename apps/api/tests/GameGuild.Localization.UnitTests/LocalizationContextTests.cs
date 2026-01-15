using System.Globalization;
using FluentAssertions;
using GameGuild.Localization;
using Xunit;

namespace GameGuild.Tests.Localization.Unit;

/// <summary>
/// Unit tests for the LocalizationContext service - tests default constructor behavior
/// </summary>
public class LocalizationContextTests
{
    private readonly LocalizationContext _localizationContext;

    public LocalizationContextTests()
    {
        // Use the default constructor which sets en-US and UTC
        _localizationContext = new LocalizationContext();
    }

    [Fact]
    public void Constructor_Should_Initialize_Without_Throwing()
    {
        // Act
        var context = new LocalizationContext();

        // Assert
        context.Should().NotBeNull();
        context.Should().BeAssignableTo<ILocalizationContext>();
    }

    [Fact]
    public void CurrentCulture_Should_Return_Default_EnUS_Culture()
    {
        // Act
        var culture = _localizationContext.CurrentCulture;

        // Assert
        culture.Should().NotBeNull();
        culture.Name.Should().Be("en-US");
        culture.Should().Be(CultureInfo.GetCultureInfo("en-US"));
    }

    [Fact]
    public void CurrentUiCulture_Should_Return_Default_EnUS_Culture()
    {
        // Act
        var uiCulture = _localizationContext.CurrentUiCulture;

        // Assert
        uiCulture.Should().NotBeNull();
        uiCulture.Name.Should().Be("en-US");
        uiCulture.Should().Be(CultureInfo.GetCultureInfo("en-US"));
    }

    [Fact]
    public void CurrentTimeZone_Should_Return_UTC_TimeZone()
    {
        // Act
        var timeZone = _localizationContext.CurrentTimeZone;

        // Assert
        timeZone.Should().NotBeNull();
        timeZone.Should().Be(TimeZoneInfo.Utc);
        timeZone.Id.Should().Be("UTC");
    }

    [Fact]
    public void TimeZoneId_Should_Return_UTC()
    {
        // Act
        var timeZoneId = _localizationContext.TimeZoneId;

        // Assert
        timeZoneId.Should().Be("UTC");
    }

    [Fact]
    public void GetCurrentLocalTime_Should_Return_UTC_Time()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;

        // Act
        var currentTime = _localizationContext.GetCurrentLocalTime();
        var afterCall = DateTime.UtcNow;

        // Assert
        currentTime.Should().BeOnOrAfter(beforeCall);
        currentTime.Should().BeOnOrBefore(afterCall);
        currentTime.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ConvertToLocalTime_Should_Return_Same_Time_For_UTC_TimeZone()
    {
        // Arrange
        var utcTime = new DateTime(2023, 12, 25, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var localTime = _localizationContext.ConvertToLocalTime(utcTime);

        // Assert
        localTime.Should().Be(utcTime);
        localTime.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ConvertToUtcTime_Should_Return_Same_Time_For_UTC_TimeZone()
    {
        // Arrange
        var localTime = new DateTime(2023, 12, 25, 10, 30, 0, DateTimeKind.Unspecified);

        // Act
        var utcTime = _localizationContext.ConvertToUtcTime(localTime);

        // Assert
        utcTime.Should().Be(localTime);
        utcTime.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData("2023-01-01T00:00:00Z")]
    [InlineData("2023-06-15T12:30:45Z")]
    [InlineData("2023-12-31T23:59:59Z")]
    public void ConvertToLocalTime_Should_Handle_Various_UTC_Times(string utcTimeString)
    {
        // Arrange
        var utcTime = DateTime.Parse(utcTimeString, null, DateTimeStyles.RoundtripKind);

        // Act
        var localTime = _localizationContext.ConvertToLocalTime(utcTime);

        // Assert
        localTime.Should().Be(utcTime); // For UTC timezone, local time equals UTC time
    }

    [Theory]
    [InlineData("2023-01-01T00:00:00")]
    [InlineData("2023-06-15T12:30:45")]
    [InlineData("2023-12-31T23:59:59")]
    public void ConvertToUtcTime_Should_Handle_Various_Local_Times(string localTimeString)
    {
        // Arrange
        var localTime = DateTime.Parse(localTimeString);

        // Act
        var utcTime = _localizationContext.ConvertToUtcTime(localTime);

        // Assert
        utcTime.Should().Be(localTime); // For UTC timezone, UTC time equals local time
        utcTime.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Properties_Should_Be_Consistent_Across_Multiple_Calls()
    {
        // Act
        var culture1 = _localizationContext.CurrentCulture;
        var culture2 = _localizationContext.CurrentCulture;
        var uiCulture1 = _localizationContext.CurrentUiCulture;
        var uiCulture2 = _localizationContext.CurrentUiCulture;
        var timeZone1 = _localizationContext.CurrentTimeZone;
        var timeZone2 = _localizationContext.CurrentTimeZone;

        // Assert
        culture1.Should().Be(culture2);
        uiCulture1.Should().Be(uiCulture2);
        timeZone1.Should().Be(timeZone2);
    }

    [Fact]
    public void Should_Implement_ILocalizationContext_Interface()
    {
        // Act & Assert
        _localizationContext.Should().BeAssignableTo<ILocalizationContext>();
    }

    [Fact]
    public void Should_Create_With_Default_Constructor()
    {
        // Act
        var context = new LocalizationContext();

        // Assert
        context.Should().NotBeNull();
    }

    [Fact]
    public void Should_Create_With_Culture_And_TimeZone_Constructor()
    {
        // Arrange
        var culture = CultureInfo.GetCultureInfo("fr-FR");
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

        // Act
        var context = new LocalizationContext(culture, timeZone);

        // Assert
        context.Should().NotBeNull();
        context.CurrentCulture.Name.Should().Be("fr-FR");
    }

    [Fact]
    public void Time_Conversion_Should_Be_Reversible()
    {
        // Arrange
        var originalTime = new DateTime(2023, 12, 25, 15, 30, 45, DateTimeKind.Unspecified);

        // Act
        var utcTime = _localizationContext.ConvertToUtcTime(originalTime);
        var backToLocal = _localizationContext.ConvertToLocalTime(utcTime);

        // Assert
        backToLocal.Should().Be(originalTime);
    }

    [Fact]
    public void GetCurrentLocalTime_Should_Use_CurrentTimeZone()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;

        // Act
        var localTime = _localizationContext.GetCurrentLocalTime();

        // Assert
        // Since CurrentTimeZone is UTC, local time should be approximately equal to UTC time
        localTime.Should().BeCloseTo(beforeCall, TimeSpan.FromSeconds(1));
    }
}