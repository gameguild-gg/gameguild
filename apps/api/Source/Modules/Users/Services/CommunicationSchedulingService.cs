using GameGuild.Core.Helpers;
using GameGuild.Core.Repositories;
using GameGuild.Modules.Users.Entities;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Users.Services;

/// <summary>
/// Service interface for managing communication preferences and scheduling.
/// </summary>
public interface ICommunicationSchedulingService
{
    /// <summary>
    /// Gets or creates communication preferences for a user and channel.
    /// </summary>
    Task<Result<CommunicationPreference>> GetOrCreatePreferenceAsync(Guid userId, string channel);

    /// <summary>
    /// Updates communication preferences for a specific channel.
    /// </summary>
    Task<Result<CommunicationPreference>> UpdatePreferenceAsync(
        Guid userId,
        string channel,
        bool? isEnabled = null,
        string? locale = null,
        string? timezone = null,
        string? quietHoursStart = null,
        string? quietHoursEnd = null,
        string? preferredDeliveryStart = null,
        string? preferredDeliveryEnd = null,
        string? allowedDeliveryDays = null,
        bool? respectQuietHoursForUrgent = null,
        string? digestFrequency = null,
        string? digestDeliveryTime = null,
        int? priorityThreshold = null);

    /// <summary>
    /// Gets all communication preferences for a user.
    /// </summary>
    Task<List<CommunicationPreference>> GetUserPreferencesAsync(Guid userId);

    /// <summary>
    /// Schedules a communication for delivery, respecting user preferences.
    /// </summary>
    Task<Result<ScheduledCommunication>> ScheduleCommunicationAsync(
        Guid userId,
        string channel,
        string content,
        string? subject = null,
        int priority = 2,
        string? metadata = null);

    /// <summary>
    /// Processes due communications and attempts delivery.
    /// </summary>
    Task<Result<int>> ProcessDueCommunicationsAsync();

    /// <summary>
    /// Calculates the optimal delivery time based on user preferences.
    /// </summary>
    Task<DateTime> CalculateOptimalDeliveryTimeAsync(Guid userId, string channel, int priority);

    /// <summary>
    /// Cancels a scheduled communication.
    /// </summary>
    Task<Result> CancelScheduledCommunicationAsync(Guid communicationId);
}

/// <summary>
/// Service implementation for communication scheduling with timezone-aware delivery.
/// </summary>
public class CommunicationSchedulingService : ICommunicationSchedulingService
{
    private readonly IRepository<CommunicationPreference> _preferenceRepository;
    private readonly IRepository<ScheduledCommunication> _communicationRepository;
    private readonly IRepository<User> _userRepository;
    private readonly ILogger<CommunicationSchedulingService> _logger;

    public CommunicationSchedulingService(
        IRepository<CommunicationPreference> preferenceRepository,
        IRepository<ScheduledCommunication> communicationRepository,
        IRepository<User> userRepository,
        ILogger<CommunicationSchedulingService> logger)
    {
        _preferenceRepository = preferenceRepository;
        _communicationRepository = communicationRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<CommunicationPreference>> GetOrCreatePreferenceAsync(Guid userId, string channel)
    {
        var existing = await _preferenceRepository.FindAsync(
            p => p.UserId == userId && p.Channel == channel);

        if (existing.Any())
        {
            return Result<CommunicationPreference>.Success(existing.First());
        }

        // Create default preference
        var preference = new CommunicationPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Channel = channel,
            IsEnabled = true,
            Locale = "en-US",
            Timezone = "UTC",
            RespectQuietHoursForUrgent = false,
            DigestFrequency = "Realtime",
            PriorityThreshold = 0, // Accept all priorities
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _preferenceRepository.AddAsync(preference);

        return Result<CommunicationPreference>.Success(preference);
    }

    public async Task<Result<CommunicationPreference>> UpdatePreferenceAsync(
        Guid userId,
        string channel,
        bool? isEnabled = null,
        string? locale = null,
        string? timezone = null,
        string? quietHoursStart = null,
        string? quietHoursEnd = null,
        string? preferredDeliveryStart = null,
        string? preferredDeliveryEnd = null,
        string? allowedDeliveryDays = null,
        bool? respectQuietHoursForUrgent = null,
        string? digestFrequency = null,
        string? digestDeliveryTime = null,
        int? priorityThreshold = null)
    {
        var preferenceResult = await GetOrCreatePreferenceAsync(userId, channel);
        if (!preferenceResult.IsSuccess)
        {
            return preferenceResult;
        }

        var preference = preferenceResult.Data!;

        if (isEnabled.HasValue)
        {
            if (isEnabled.Value) preference.Enable(); else preference.Disable();
        }

        if (locale != null) preference.Locale = locale;
        if (timezone != null) preference.Timezone = timezone;
        if (quietHoursStart != null) preference.QuietHoursStart = quietHoursStart;
        if (quietHoursEnd != null) preference.QuietHoursEnd = quietHoursEnd;
        if (preferredDeliveryStart != null) preference.PreferredDeliveryStart = preferredDeliveryStart;
        if (preferredDeliveryEnd != null) preference.PreferredDeliveryEnd = preferredDeliveryEnd;
        if (allowedDeliveryDays != null) preference.AllowedDeliveryDays = allowedDeliveryDays;
        if (respectQuietHoursForUrgent.HasValue) preference.RespectQuietHoursForUrgent = respectQuietHoursForUrgent.Value;
        if (digestFrequency != null) preference.DigestFrequency = digestFrequency;
        if (digestDeliveryTime != null) preference.DigestDeliveryTime = digestDeliveryTime;
        if (priorityThreshold.HasValue) preference.PriorityThreshold = priorityThreshold.Value;

        preference.MarkReviewed();

        await _preferenceRepository.UpdateAsync(preference);

        return Result<CommunicationPreference>.Success(preference);
    }

    public async Task<List<CommunicationPreference>> GetUserPreferencesAsync(Guid userId)
    {
        return await _preferenceRepository.FindAsync(p => p.UserId == userId);
    }

    public async Task<Result<ScheduledCommunication>> ScheduleCommunicationAsync(
        Guid userId,
        string channel,
        string content,
        string? subject = null,
        int priority = 2,
        string? metadata = null)
    {
        // Validate user exists
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return Result<ScheduledCommunication>.Failure("User not found");
        }

        // Get channel preference
        var preferenceResult = await GetOrCreatePreferenceAsync(userId, channel);
        if (!preferenceResult.IsSuccess)
        {
            return Result<ScheduledCommunication>.Failure(preferenceResult.Error!);
        }

        var preference = preferenceResult.Data!;

        // Check if channel is enabled
        if (!preference.IsEnabled)
        {
            return Result<ScheduledCommunication>.Failure($"Channel {channel} is disabled for this user");
        }

        // Check priority threshold
        if (priority < preference.PriorityThreshold)
        {
            return Result<ScheduledCommunication>.Failure(
                $"Message priority {priority} below user threshold {preference.PriorityThreshold}");
        }

        // Calculate optimal delivery time
        var deliveryTime = await CalculateOptimalDeliveryTimeAsync(userId, channel, priority);

        var scheduledComm = new ScheduledCommunication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Channel = channel,
            Content = content,
            Subject = subject,
            Priority = priority,
            QueuedAt = DateTime.UtcNow,
            ScheduledDeliveryAt = deliveryTime,
            Status = DeliveryStatus.Scheduled,
            DeliveryAttempts = 0,
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _communicationRepository.AddAsync(scheduledComm);

        _logger.LogInformation(
            "Scheduled communication for user {UserId} on channel {Channel}. Delivery at {DeliveryTime}",
            userId, channel, deliveryTime);

        return Result<ScheduledCommunication>.Success(scheduledComm);
    }

    public async Task<DateTime> CalculateOptimalDeliveryTimeAsync(Guid userId, string channel, int priority)
    {
        var preferenceResult = await GetOrCreatePreferenceAsync(userId, channel);
        if (!preferenceResult.IsSuccess)
        {
            // Default to immediate delivery if preference not found
            return DateTime.UtcNow;
        }

        var preference = preferenceResult.Data!;
        var now = DateTime.UtcNow;

        // Urgent messages bypass scheduling (priority 4)
        if (priority >= 4 && !preference.RespectQuietHoursForUrgent)
        {
            return now;
        }

        // Get user's local time
        var userTimezone = TimeZoneInfo.FindSystemTimeZoneById(preference.Timezone ?? "UTC");
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(now, userTimezone);

        // Check if within quiet hours
        if (!string.IsNullOrEmpty(preference.QuietHoursStart) && !string.IsNullOrEmpty(preference.QuietHoursEnd))
        {
            if (IsWithinQuietHours(localNow, preference.QuietHoursStart, preference.QuietHoursEnd))
            {
                // Schedule for after quiet hours end
                var quietEndTime = TimeOnly.Parse(preference.QuietHoursEnd);
                var nextDelivery = localNow.Date.Add(quietEndTime.ToTimeSpan());

                // If quiet hours end has passed today, it's tomorrow
                if (nextDelivery <= localNow)
                {
                    nextDelivery = nextDelivery.AddDays(1);
                }

                return TimeZoneInfo.ConvertTimeToUtc(nextDelivery, userTimezone);
            }
        }

        // Check if within preferred delivery window
        if (!string.IsNullOrEmpty(preference.PreferredDeliveryStart) && !string.IsNullOrEmpty(preference.PreferredDeliveryEnd))
        {
            if (!IsWithinDeliveryWindow(localNow, preference.PreferredDeliveryStart, preference.PreferredDeliveryEnd))
            {
                // Schedule for next delivery window
                var windowStart = TimeOnly.Parse(preference.PreferredDeliveryStart);
                var nextDelivery = localNow.Date.Add(windowStart.ToTimeSpan());

                if (nextDelivery <= localNow)
                {
                    nextDelivery = nextDelivery.AddDays(1);
                }

                return TimeZoneInfo.ConvertTimeToUtc(nextDelivery, userTimezone);
            }
        }

        // Check allowed delivery days
        if (!string.IsNullOrEmpty(preference.AllowedDeliveryDays))
        {
            var allowedDays = preference.AllowedDeliveryDays.Split(',').Select(d => d.Trim()).ToList();
            if (!allowedDays.Contains(localNow.DayOfWeek.ToString()))
            {
                // Find next allowed day
                var daysToAdd = 1;
                var nextDay = localNow.AddDays(daysToAdd);

                while (!allowedDays.Contains(nextDay.DayOfWeek.ToString()) && daysToAdd < 7)
                {
                    daysToAdd++;
                    nextDay = localNow.AddDays(daysToAdd);
                }

                // Schedule at start of delivery window on next allowed day
                var windowStart = TimeOnly.Parse(preference.PreferredDeliveryStart ?? "09:00");
                var nextDelivery = nextDay.Date.Add(windowStart.ToTimeSpan());

                return TimeZoneInfo.ConvertTimeToUtc(nextDelivery, userTimezone);
            }
        }

        // No restrictions, deliver immediately
        return now;
    }

    public async Task<Result<int>> ProcessDueCommunicationsAsync()
    {
        var dueCommunications = await _communicationRepository.FindAsync(c => c.IsDue);

        var processedCount = 0;

        foreach (var comm in dueCommunications)
        {
            try
            {
                // TODO: Integrate with actual communication delivery service
                // For now, just mark as delivered
                comm.MarkDelivered();
                await _communicationRepository.UpdateAsync(comm);

                processedCount++;

                _logger.LogInformation(
                    "Delivered communication {Id} to user {UserId} on channel {Channel}",
                    comm.Id, comm.UserId, comm.Channel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to deliver communication {Id} to user {UserId}",
                    comm.Id, comm.UserId);

                comm.RecordFailure(ex.Message);
                await _communicationRepository.UpdateAsync(comm);
            }
        }

        _logger.LogInformation("Processed {Count} due communications", processedCount);

        return Result<int>.Success(processedCount);
    }

    public async Task<Result> CancelScheduledCommunicationAsync(Guid communicationId)
    {
        var comm = await _communicationRepository.GetByIdAsync(communicationId);
        if (comm == null)
        {
            return Result.Failure("Scheduled communication not found");
        }

        comm.Cancel();
        await _communicationRepository.UpdateAsync(comm);

        return Result.Success();
    }

    private bool IsWithinQuietHours(DateTime localTime, string quietStart, string quietEnd)
    {
        var currentTime = TimeOnly.FromDateTime(localTime);
        var start = TimeOnly.Parse(quietStart);
        var end = TimeOnly.Parse(quietEnd);

        // Handle overnight quiet hours (e.g., 22:00 to 08:00)
        if (start > end)
        {
            return currentTime >= start || currentTime < end;
        }

        return currentTime >= start && currentTime < end;
    }

    private bool IsWithinDeliveryWindow(DateTime localTime, string windowStart, string windowEnd)
    {
        var currentTime = TimeOnly.FromDateTime(localTime);
        var start = TimeOnly.Parse(windowStart);
        var end = TimeOnly.Parse(windowEnd);

        // Handle overnight windows
        if (start > end)
        {
            return currentTime >= start || currentTime < end;
        }

        return currentTime >= start && currentTime < end;
    }
}
