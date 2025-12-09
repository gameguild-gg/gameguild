using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Queries;

/// <summary> Query to get recommended programs for user </summary>
public record GetRecommendedProgramsQuery(string UserId, int Take = 10) : IQuery<IEnumerable<Program>>;
