using GameGuild.CQRS;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Command to track usage for a tenant
/// </summary>
public abstract record TrackUsageCommand(Guid TenantId, string ResourceType, string ActionType, int Quantity = 1, decimal? Cost = null, Dictionary<string, object>? Metadata = null) : ICommand<TrackUsageResponse>;
