using MediatR;

namespace GameGuild.Modules.TestingLab.Events;

public record TestingRequestCreatedEvent(
    Guid TestingRequestId,
    Guid ProjectVersionId,
    string Title,
    Guid CreatedByUserId,
    DateTime CreatedAt
) : INotification;
