
using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Rule that restricts access to specific time windows.
///     Parameters:
///     - windows: object[] - List of time windows, each with:
///         - daysOfWeek: int[] - Days of week (0=Sunday, 6=Saturday)
///         - startTime: string - Start time in HH:mm format (24-hour)
///         - endTime: string - End time in HH:mm format (24-hour)
///         - timezone: string - IANA timezone name (default: UTC)
/// </summary>
public sealed class RequireTimeWindowRuleEvaluator : IRuleEvaluator
{
    public string RuleType => RuleTypes.RequireTimeWindow;

    public Task<RuleEvaluationResult> EvaluateAsync(
        AuthorizationHandlerContext context,
        RuleParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var windowsJson = parameters.GetRaw("windows");
        if (windowsJson is null)
        {
            // No time restrictions - pass
            return Task.FromResult(RuleEvaluationResult.Success());
        }

        // Get timezone (default to UTC)
        var timezoneName = parameters.GetString("timezone") ?? "UTC";
        TimeZoneInfo timezone;

        try
        {
            timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneName);
        }
        catch (TimeZoneNotFoundException)
        {
            // Try IANA to Windows mapping on Windows
            try
            {
                timezone = TimeZoneInfo.FindSystemTimeZoneById(
                    ConvertIanaToWindows(timezoneName));
            }
            catch
            {
                return Task.FromResult(RuleEvaluationResult.Fail(
                    $"Unknown timezone: '{timezoneName}'"));
            }
        }

        var now = TimeZoneInfo.ConvertTimeFromUtc(SystemClock.UtcNow, timezone);
        var currentDayOfWeek = (int)now.DayOfWeek;
        var currentTime = now.TimeOfDay;

        // Parse windows from JSON
        System.Text.Json.JsonElement windowsArray;
        if (windowsJson.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            windowsArray = windowsJson.Value;
        }
        else if (windowsJson.Value.ValueKind == System.Text.Json.JsonValueKind.Object &&
                 windowsJson.Value.TryGetProperty("windows", out var innerArray) &&
                 innerArray.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            windowsArray = innerArray;
        }
        else
        {
            return Task.FromResult(RuleEvaluationResult.Fail(
                "Invalid time windows configuration"));
        }

        foreach (var window in windowsArray.EnumerateArray())
        {
            if (IsWithinWindow(window, currentDayOfWeek, currentTime))
            {
                return Task.FromResult(RuleEvaluationResult.Success());
            }
        }

        return Task.FromResult(RuleEvaluationResult.Fail(
            $"Access is not allowed at this time ({now:yyyy-MM-dd HH:mm} {timezoneName})"));
    }

    private static bool IsWithinWindow(System.Text.Json.JsonElement window, int currentDayOfWeek, TimeSpan currentTime)
    {
        // Check days of week
        if (window.TryGetProperty("daysOfWeek", out var daysProperty))
        {
            var allowedDays = new HashSet<int>();
            if (daysProperty.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var day in daysProperty.EnumerateArray())
                {
                    if (day.TryGetInt32(out var dayValue))
                    {
                        allowedDays.Add(dayValue);
                    }
                }
            }

            if (allowedDays.Count > 0 && !allowedDays.Contains(currentDayOfWeek))
            {
                return false;
            }
        }

        // Check time range
        TimeSpan? startTime = null;
        TimeSpan? endTime = null;

        if (window.TryGetProperty("startTime", out var startProp) &&
            TimeSpan.TryParse(startProp.GetString(), out var parsedStart))
        {
            startTime = parsedStart;
        }

        if (window.TryGetProperty("endTime", out var endProp) &&
            TimeSpan.TryParse(endProp.GetString(), out var parsedEnd))
        {
            endTime = parsedEnd;
        }

        // If no time constraints, day match is sufficient
        if (!startTime.HasValue && !endTime.HasValue)
        {
            return true;
        }

        // Handle overnight windows (e.g., 22:00 to 06:00)
        if (startTime.HasValue && endTime.HasValue)
        {
            if (startTime.Value > endTime.Value)
            {
                // Overnight window
                return currentTime >= startTime.Value || currentTime <= endTime.Value;
            }
            else
            {
                // Same-day window
                return currentTime >= startTime.Value && currentTime <= endTime.Value;
            }
        }

        // Only start or only end specified. The no-constraint case returned above.
        return startTime.HasValue
            ? currentTime >= startTime.Value
            : currentTime <= endTime!.Value;
    }

    private static string ConvertIanaToWindows(string ianaId)
    {
        // Common IANA to Windows timezone mappings
        return ianaId switch
        {
            "America/New_York" => "Eastern Standard Time",
            "America/Chicago" => "Central Standard Time",
            "America/Denver" => "Mountain Standard Time",
            "America/Los_Angeles" => "Pacific Standard Time",
            "America/Sao_Paulo" => "E. South America Standard Time",
            "Europe/London" => "GMT Standard Time",
            "Europe/Paris" => "Romance Standard Time",
            "Europe/Berlin" => "W. Europe Standard Time",
            "Asia/Tokyo" => "Tokyo Standard Time",
            "Asia/Shanghai" => "China Standard Time",
            "Asia/Singapore" => "Singapore Standard Time",
            "Australia/Sydney" => "AUS Eastern Standard Time",
            "UTC-Fallback" => "UTC",
            "UTC" => "UTC",
            _ => ianaId // Return as-is if no mapping found
        };
    }
}
