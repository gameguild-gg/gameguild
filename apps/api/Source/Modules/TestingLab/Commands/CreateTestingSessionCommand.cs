using MediatR;

namespace GameGuild.Modules.TestingLab.Commands;

public record CreateTestingSessionCommand(
    Guid TestingRequestId,
    string Title,
    string? Description,
    DateTime ScheduledDate,
    TimeSpan Duration,
    TestingMode Mode,
    Guid? LocationId,
    int MaxParticipants,
    RegistrationType RegistrationType,
    bool IsActive = true
) : IRequest<TestingSession>;
