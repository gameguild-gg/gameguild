using GameGuild.CQRS;
using GameGuild.Modules.Programs.DTOs;

namespace GameGuild.Modules.Programs.Queries;

public record GetUserProgramProgressQuery(Guid UserId, Guid ProgramId) : IQuery<ProgramUserProgress?>;
