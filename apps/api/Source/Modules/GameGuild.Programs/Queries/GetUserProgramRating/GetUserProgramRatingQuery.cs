using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Queries;

/// <summary> Query to get user's rating for a program </summary>
public record GetUserProgramRatingQuery(Guid ProgramId, string UserId) : IQuery<ProgramRating?>;
