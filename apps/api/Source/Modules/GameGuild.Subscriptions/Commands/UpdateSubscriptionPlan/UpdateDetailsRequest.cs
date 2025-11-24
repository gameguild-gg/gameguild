namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Request model for updating plan details
/// </summary>
public record UpdateDetailsRequest(string Name, string? Description = null, int? SortOrder = null);
