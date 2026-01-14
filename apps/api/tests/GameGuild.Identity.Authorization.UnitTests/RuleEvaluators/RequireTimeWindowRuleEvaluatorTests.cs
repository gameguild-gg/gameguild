using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.RuleEvaluators;

public class RequireTimeWindowRuleEvaluatorTests
{
    private readonly RequireTimeWindowRuleEvaluator _evaluator;

    public RequireTimeWindowRuleEvaluatorTests()
    {
        _evaluator = new RequireTimeWindowRuleEvaluator();
    }

    [Fact]
    public async Task EvaluateAsync_NoWindows_ReturnsSuccess()
    {
        // Arrange
        var context = new AuthorizationHandlerContext(
            [],
            new ClaimsPrincipal(new ClaimsIdentity("Bearer")),
            null);
        var parameters = RuleParameters.FromJson("{}");

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_InsideTimeWindow_ReturnsSuccess()
    {
        // Arrange
        var context = new AuthorizationHandlerContext(
            [],
            new ClaimsPrincipal(new ClaimsIdentity("Bearer")),
            null);

        // Create a window that covers the whole day (00:00 to 23:59) for all days
        var window = new
        {
            daysOfWeek = new[] { 0, 1, 2, 3, 4, 5, 6 },
            startTime = "00:00",
            endTime = "23:59"
        };
        
        var json = JsonSerializer.Serialize(new { windows = new[] { window }, timezone = "UTC" });
        var parameters = RuleParameters.FromJson(json);

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_OutsideTimeWindow_ReturnsFail()
    {
        // Arrange
        var context = new AuthorizationHandlerContext(
            [],
            new ClaimsPrincipal(new ClaimsIdentity("Bearer")),
            null);

        // Calculate a time window that definitely does not include Now.
        // We'll set a 1-hour window that ended 2 hours ago.
        var now = DateTime.UtcNow;
        var twoHoursAgo = now.AddHours(-2);
        var oneHourAgo = now.AddHours(-1);
        
        var window = new
        {
            daysOfWeek = new[] { 0, 1, 2, 3, 4, 5, 6 },
            startTime = twoHoursAgo.ToString("HH:mm"),
            endTime = oneHourAgo.ToString("HH:mm")
        };

        var json = JsonSerializer.Serialize(new { windows = new[] { window }, timezone = "UTC" });
        var parameters = RuleParameters.FromJson(json);

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_WrongDay_ReturnsFail()
    {
        // Arrange
        var context = new AuthorizationHandlerContext(
            [],
            new ClaimsPrincipal(new ClaimsIdentity("Bearer")),
            null);

        // Currently it is some day. Let's pick a day that is NOT today.
        var today = (int)DateTime.UtcNow.DayOfWeek;
        var tomorrow = (today + 1) % 7;

        var window = new
        {
            daysOfWeek = new[] { tomorrow }, // Only allowed tomorrow
            startTime = "00:00",
            endTime = "23:59"
        };

        var json = JsonSerializer.Serialize(new { windows = new[] { window }, timezone = "UTC" });
        var parameters = RuleParameters.FromJson(json);

        // Act
        var result = await _evaluator.EvaluateAsync(context, parameters);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }
}
