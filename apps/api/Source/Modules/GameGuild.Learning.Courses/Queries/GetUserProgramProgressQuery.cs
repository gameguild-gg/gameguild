using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

public record GetUserProgramProgressQuery(Guid UserId, Guid ProgramId) : IQuery<ProgramUserProgress?>;
