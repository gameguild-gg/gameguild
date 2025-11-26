using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to add program to wishlist </summary>
public record AddToWishlistCommand(Guid ProgramId, string UserId) : ICommand<ProgramWishlist>;
