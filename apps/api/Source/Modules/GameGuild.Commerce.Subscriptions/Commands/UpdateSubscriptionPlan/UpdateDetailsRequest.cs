namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for updating plan details
/// </summary>
public sealed record UpdateDetailsRequest(string Name, string? Description = null, int? SortOrder = null);
