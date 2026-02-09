using GameGuild.CQRS;
using GameGuild.Resources;


namespace GameGuild.TestingLab;

/// <summary>
///     Command to create a new testing session
/// </summary>
[RequiresQuota(ResourceUsageType.TestingSessions, Source = "CreateTestingSession")]
public sealed record CreateTestingSessionCommand(
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
