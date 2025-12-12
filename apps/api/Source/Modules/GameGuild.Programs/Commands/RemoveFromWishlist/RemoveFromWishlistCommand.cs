using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to remove program from wishlist </summary>
public record RemoveFromWishlistCommand(Guid ProgramId, string UserId) : ICommand<bool>;
