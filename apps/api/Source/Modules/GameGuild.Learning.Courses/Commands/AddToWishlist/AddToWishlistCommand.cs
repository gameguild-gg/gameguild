using GameGuild.CQRS;

namespace GameGuild.Programs;

/// <summary> Command to add program to wishlist </summary>
public record AddToWishlistCommand(Guid ProgramId, string UserId) : ICommand<ProgramWishlist>;
