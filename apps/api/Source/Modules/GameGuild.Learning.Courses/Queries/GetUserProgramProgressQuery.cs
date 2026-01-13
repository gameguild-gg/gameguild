using GameGuild.CQRS;

namespace GameGuild.Programs;

public record GetUserProgramProgressQuery(Guid UserId, Guid ProgramId) : IQuery<ProgramUserProgress?>;
