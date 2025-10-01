using FluentAssertions;
using Xunit;

namespace GameGuild.Tests.Core.Unit.Providers;

/// <summary>
/// Unit tests for DateTimeProvider
/// </summary>
public class DateTimeProviderTests
{
    [Fact]
    public void UtcNow_Should_Return_Current_UTC_Time()
    {
        // Arrange
        DateTimeProvider provider = new();
        DateTime beforeCall = DateTime.UtcNow;

        // Act
        DateTime result = provider.UtcNow;
        DateTime afterCall = DateTime.UtcNow;

        // Assert
        _ = result.Should().BeOnOrAfter(beforeCall);
        _ = result.Should().BeOnOrBefore(afterCall);
        _ = result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Now_Should_Return_Current_Local_Time()
    {
        // Arrange
        DateTimeProvider provider = new();
        DateTime beforeCall = DateTime.Now;

        // Act
        DateTime result = provider.Now;
        DateTime afterCall = DateTime.Now;

        // Assert
        _ = result.Should().BeOnOrAfter(beforeCall);
        _ = result.Should().BeOnOrBefore(afterCall);
        _ = result.Kind.Should().Be(DateTimeKind.Local);
    }

    [Fact]
    public void Today_Should_Return_Current_Date()
    {
        // Arrange
        DateTimeProvider provider = new();
        DateOnly expectedToday = DateOnly.FromDateTime(DateTime.Today);

        // Act
        DateOnly result = provider.Today;

        // Assert
        _ = result.Should().Be(expectedToday);
    }

    [Fact]
    public void DateTimeProvider_Should_Implement_IDateTimeProvider()
    {
        // Arrange & Act
        DateTimeProvider provider = new();

        // Assert
        _ = provider.Should().BeAssignableTo<IDateTimeProvider>();
    }

    [Fact]
    public void Multiple_Calls_Should_Return_Different_Values()
    {
        // Arrange
        DateTimeProvider provider = new();

        // Act
        DateTime first = provider.UtcNow;
        Thread.Sleep(1); // Ensure time difference
        DateTime second = provider.UtcNow;

        // Assert
        _ = second.Should().BeAfter(first);
    }

    [Fact]
    public void UtcNow_And_Now_Should_Be_Close_In_Time()
    {
        // Arrange
        DateTimeProvider provider = new();

        // Act
        DateTime utcNow = provider.UtcNow;
        DateTime now = provider.Now;

        // Assert - They should be within seconds of each other (accounting for timezone offset)
        TimeSpan difference = now.ToUniversalTime() - utcNow;
        _ = Math.Abs(difference.TotalSeconds).Should().BeLessThan(1);
    }
}