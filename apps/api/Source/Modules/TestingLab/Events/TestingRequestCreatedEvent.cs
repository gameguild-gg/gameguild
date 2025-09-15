using GameGuild.CQRS;

using GameGuild.CQRS;

namespace GameGuild.Modules.TestingLab;

public record TestingRequestCreatedEvent(
  Guid TestingRequestId,
  Guid ProjectVersionId,
  string Title,
  Guid CreatedByUserId,
  DateTime CreatedAt
) : INotification;
