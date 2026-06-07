using GameGuild.CQRS;


namespace GameGuild.TestingLab;

public sealed record TestingRequestCreatedEvent(Guid TestingRequestId, Guid? ProjectVersionId, string Title, Guid CreatedByUserId, DateTime CreatedAt) : INotification;
