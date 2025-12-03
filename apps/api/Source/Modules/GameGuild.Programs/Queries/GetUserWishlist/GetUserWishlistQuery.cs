using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Queries;

/// <summary> Query to get user's wishlist programs </summary>
public record GetUserWishlistQuery(string UserId, int Skip = 0, int Take = 50) : IQuery<IEnumerable<Program>>;
