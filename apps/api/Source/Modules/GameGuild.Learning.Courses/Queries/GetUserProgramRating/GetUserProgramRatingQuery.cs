using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get user's rating for a program </summary>
public record GetUserProgramRatingQuery(Guid ProgramId, string UserId) : IQuery<ProgramRating?>;
