using GameGuild.CQRS;

using GameGuild.Enums;

namespace GameGuild.Learning.Courses;

/// <summary> Query to check if program is in user's wishlist </summary>
public record CheckProgramInWishlistQuery(Guid ProgramId, string UserId) : IQuery<bool>;
