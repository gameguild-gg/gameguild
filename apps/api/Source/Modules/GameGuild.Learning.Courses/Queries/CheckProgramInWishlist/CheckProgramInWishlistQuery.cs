using GameGuild.CQRS;

using GameGuild.Enums;

namespace GameGuild.Programs;

/// <summary> Query to check if program is in user's wishlist </summary>
public record CheckProgramInWishlistQuery(Guid ProgramId, string UserId) : IQuery<bool>;
