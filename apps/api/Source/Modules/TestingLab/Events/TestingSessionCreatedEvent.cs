namespace GameGuild.Modules.TestingLab.Events;

public record TestingSessionCreatedEvent(
  Guid TestingSessionId,
  Guid TestingRequestId,
  string Title,
  DateTime ScheduledDate,
  Guid CreatedByUserId,
  DateTime CreatedAt
) : INotification;
