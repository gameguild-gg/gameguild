using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Queries;

/// <summary> Query to check if user is enrolled in program </summary>
public record CheckUserEnrollmentQuery(Guid ProgramId, string UserId) : IQuery<ProgramUser?>;
