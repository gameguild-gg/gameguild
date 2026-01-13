using GameGuild.CQRS;

namespace GameGuild.Programs;

/// <summary> Command to remove program from wishlist </summary>
public record RemoveFromWishlistCommand(Guid ProgramId, string UserId) : ICommand<bool>;
