using GameGuild.CQRS;

using GameGuild.Enums;

namespace GameGuild.Programs;

/// <summary> Query to check if user is enrolled in program </summary>
public record CheckUserEnrollmentQuery(Guid ProgramId, string UserId) : IQuery<ProgramUser?>;
