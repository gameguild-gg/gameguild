using GameGuild.CQRS;


namespace GameGuild.TestingLab;

public sealed record TestingSessionCreatedEvent(Guid TestingSessionId, Guid TestingRequestId, string Title, DateTime ScheduledDate, Guid CreatedByUserId, DateTime CreatedAt) : INotification;
