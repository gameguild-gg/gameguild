using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Command to record a revenue event
/// </summary>
public record RecordRevenueEventCommand(RevenueEventType EventType, decimal Amount, string Currency, RevenueSource Source, string ReferenceId, Guid? UserId = null, string? Metadata = null) : ICommand<RevenueEvent>;
