namespace GameGuild.Modules.TestingLab;

public record GetTestingSessionsQuery(
  int Skip = 0,
  int Take = 50,
  Guid? TestingRequestId = null,
  SessionStatus? Status = null,
  DateTime? FromDate = null,
  DateTime? ToDate = null
) : IRequest<IEnumerable<TestingSession>>;
