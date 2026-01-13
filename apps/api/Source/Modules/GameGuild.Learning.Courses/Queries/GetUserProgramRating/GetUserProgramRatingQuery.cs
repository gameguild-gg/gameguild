using GameGuild.CQRS;

using GameGuild.Enums;

namespace GameGuild.Programs;

/// <summary> Query to get user's rating for a program </summary>
public record GetUserProgramRatingQuery(Guid ProgramId, string UserId) : IQuery<ProgramRating?>;
