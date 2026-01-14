using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get user's wishlist programs </summary>
public record GetUserWishlistQuery(string UserId, int Skip = 0, int Take = 50) : IQuery<IEnumerable<Program>>;
