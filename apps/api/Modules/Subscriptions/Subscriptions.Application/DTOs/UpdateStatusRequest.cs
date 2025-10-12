namespace Subscriptions.Domain.SubscriptionPlans.Models;

/// <summary>
///     Request model for activating/deactivating a plan
/// </summary>
public record UpdateStatusRequest(bool IsActive, string? Reason = null);

