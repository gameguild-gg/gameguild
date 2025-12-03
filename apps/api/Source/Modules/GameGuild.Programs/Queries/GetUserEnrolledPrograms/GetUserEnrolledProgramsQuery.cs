using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Queries;

/// <summary> Query to get enrolled programs for a user </summary>
public record GetUserEnrolledProgramsQuery(string UserId, int Skip = 0, int Take = 50, bool OnlyActive = true) : IQuery<IEnumerable<Program>>;
