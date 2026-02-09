using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to record a revenue event
/// </summary>
public sealed record RecordRevenueEventCommand(RevenueEventType EventType, decimal Amount, string Currency, RevenueSource Source, string ReferenceId, Guid? UserId = null, string? Metadata = null) : ICommand<RevenueEvent>;
