using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to remove program from wishlist </summary>
public record RemoveFromWishlistCommand(Guid ProgramId, string UserId) : ICommand<bool>;
