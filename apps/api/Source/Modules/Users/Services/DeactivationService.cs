using GameGuild.Helpers;
using GameGuild.Core.Repositories;
using GameGuild.Modules.Users;
using GameGuild.Modules.Users.Entities;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Users.Services;

/// <summary>
/// Service interface for managing account deactivation workflows with grace periods.
/// </summary>
public interface IDeactivationService
{
    /// <summary>
    /// Requests account deactivation with a grace period before permanent deletion.
    /// </summary>
    /// <param name="userId">The user ID requesting deactivation.</param>
    /// <param name="reason">The reason for deactivation.</param>
    /// <param name="feedback">Optional detailed feedback.</param>
    /// <param name="gracePeriodDays">The number of days before permanent deletion (default: 30).</param>
    /// <param name="ipAddress">The IP address of the request.</param>
    /// <param name="userAgent">The user agent of the request.</param>
    /// <param name="metadata">Optional additional metadata.</param>
    /// <returns>The created deactivation request.</returns>
    Task<Result<AccountDeactivationRequest>> RequestDeactivationAsync(
        Guid userId,
        string? reason,
        string? feedback,
        int gracePeriodDays = 30,
        string? ipAddress = null,
        string? userAgent = null,
        string? metadata = null);

    /// <summary>
    /// Cancels a pending deactivation request and reactivates the account.
    /// </summary>
    /// <param name="userId">The user ID to cancel deactivation for.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> CancelDeactivationAsync(Guid userId);

    /// <summary>
    /// Processes pending deactivations whose grace period has expired.
    /// Typically called by a background job.
    /// </summary>
    /// <returns>The number of accounts processed.</returns>
    Task<Result<int>> ProcessDueDeactivationsAsync();

    /// <summary>
    /// Gets the pending deactivation request for a user, if any.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The deactivation request or null if not found.</returns>
    Task<AccountDeactivationRequest?> GetPendingDeactivationAsync(Guid userId);

    /// <summary>
    /// Sends reminder notifications to users with pending deactivations.
    /// </summary>
    /// <param name="daysBeforeDeletion">Send reminders for deactivations due within this many days.</param>
    /// <returns>The number of reminders sent.</returns>
    Task<Result<int>> SendDeactivationRemindersAsync(int daysBeforeDeletion = 7);

    /// <summary>
    /// Gets all pending deactivation requests.
    /// </summary>
    /// <returns>List of pending deactivation requests.</returns>
    Task<List<AccountDeactivationRequest>> GetAllPendingDeactivationsAsync();
}

/// <summary>
/// Service implementation for account deactivation workflow management.
/// </summary>
public class DeactivationService : IDeactivationService
{
    private readonly IRepository<AccountDeactivationRequest> _deactivationRepository;
    private readonly IRepository<User> _userRepository;
    private readonly ILogger<DeactivationService> _logger;

    public DeactivationService(
        IRepository<AccountDeactivationRequest> deactivationRepository,
        IRepository<User> userRepository,
        ILogger<DeactivationService> logger)
    {
        _deactivationRepository = deactivationRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<AccountDeactivationRequest>> RequestDeactivationAsync(
        Guid userId,
        string? reason,
        string? feedback,
        int gracePeriodDays = 30,
        string? ipAddress = null,
        string? userAgent = null,
        string? metadata = null)
    {
        // Validate user exists
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return Result<AccountDeactivationRequest>.Failure("User not found");
        }

        // Check if there's already a pending deactivation request
        var existingRequest = await GetPendingDeactivationAsync(userId);
        if (existingRequest != null)
        {
            return Result<AccountDeactivationRequest>.Failure(
                "A deactivation request is already pending for this user");
        }

        var now = DateTime.UtcNow;
        var deactivationRequest = new AccountDeactivationRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Reason = reason,
            Feedback = feedback,
            RequestedAt = now,
            ScheduledDeletionAt = now.AddDays(gracePeriodDays),
            Status = DeactivationStatus.Pending,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Metadata = metadata,
            RemindersSent = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _deactivationRepository.AddAsync(deactivationRequest);

        // Deactivate the user account (soft-lock)
        user.Deactivate();
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation(
            "Deactivation requested for user {UserId}. Scheduled deletion: {ScheduledDeletionAt}",
            userId, deactivationRequest.ScheduledDeletionAt);

        return Result<AccountDeactivationRequest>.Success(deactivationRequest);
    }

    public async Task<Result> CancelDeactivationAsync(Guid userId)
    {
        var deactivationRequest = await GetPendingDeactivationAsync(userId);
        if (deactivationRequest == null)
        {
            return Result.Failure("No pending deactivation request found for this user");
        }

        // Cancel the deactivation request
        deactivationRequest.Cancel();
        await _deactivationRepository.UpdateAsync(deactivationRequest);

        // Reactivate the user account
        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            user.Activate();
            await _userRepository.UpdateAsync(user);
        }

        _logger.LogInformation("Deactivation cancelled for user {UserId}", userId);

        return Result.Success();
    }

    public async Task<Result<int>> ProcessDueDeactivationsAsync()
    {
        var now = DateTime.UtcNow;
        var dueDeactivations = await _deactivationRepository
            .FindAsync(d => d.Status == DeactivationStatus.Pending && d.ScheduledDeletionAt <= now);

        var processedCount = 0;

        foreach (var deactivation in dueDeactivations)
        {
            try
            {
                // Hard delete the user
                var user = await _userRepository.GetByIdAsync(deactivation.UserId);
                if (user != null)
                {
                    await _userRepository.DeleteAsync(user);
                }

                // Mark deactivation as completed
                deactivation.MarkCompleted();
                await _deactivationRepository.UpdateAsync(deactivation);

                processedCount++;

                _logger.LogInformation(
                    "Processed deactivation for user {UserId}. Account permanently deleted.",
                    deactivation.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process deactivation for user {UserId}",
                    deactivation.UserId);

                deactivation.Status = DeactivationStatus.Failed;
                await _deactivationRepository.UpdateAsync(deactivation);
            }
        }

        _logger.LogInformation("Processed {Count} due deactivations", processedCount);

        return Result<int>.Success(processedCount);
    }

    public async Task<AccountDeactivationRequest?> GetPendingDeactivationAsync(Guid userId)
    {
        var requests = await _deactivationRepository
            .FindAsync(d => d.UserId == userId && d.Status == DeactivationStatus.Pending);

        return requests.FirstOrDefault();
    }

    public async Task<Result<int>> SendDeactivationRemindersAsync(int daysBeforeDeletion = 7)
    {
        var reminderThreshold = DateTime.UtcNow.AddDays(daysBeforeDeletion);

        var pendingDeactivations = await _deactivationRepository.FindAsync(d =>
            d.Status == DeactivationStatus.Pending &&
            d.ScheduledDeletionAt <= reminderThreshold &&
            d.ScheduledDeletionAt > DateTime.UtcNow);

        var remindersSent = 0;

        foreach (var deactivation in pendingDeactivations)
        {
            // TODO: Integrate with notification service to send actual reminders
            // For now, just record that a reminder was sent
            deactivation.RecordReminderSent();
            await _deactivationRepository.UpdateAsync(deactivation);

            remindersSent++;

            _logger.LogInformation(
                "Reminder sent for user {UserId}. Deletion scheduled for {ScheduledDeletionAt}",
                deactivation.UserId, deactivation.ScheduledDeletionAt);
        }

        _logger.LogInformation("Sent {Count} deactivation reminders", remindersSent);

        return Result<int>.Success(remindersSent);
    }

    public async Task<List<AccountDeactivationRequest>> GetAllPendingDeactivationsAsync()
    {
        return await _deactivationRepository
            .FindAsync(d => d.Status == DeactivationStatus.Pending);
    }
}
