namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for updating plan features
/// </summary>
public record UpdateFeaturesRequest(bool? HasPrioritySupport = null, bool? HasAdvancedAnalytics = null, bool? HasCustomBranding = null, string? Features = null);
