namespace GameGuild.Modules.TestingLab;

public record GetParticipantsQuery(
  Guid? TestingSessionId = null,
  Guid? TestingRequestId = null,
  AttendanceStatus? AttendanceStatus = null,
  int Skip = 0,
  int Take = 50
) : IRequest<IEnumerable<TestingParticipant>>;
