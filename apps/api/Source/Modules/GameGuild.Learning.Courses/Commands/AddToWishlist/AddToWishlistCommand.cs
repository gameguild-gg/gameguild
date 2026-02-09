using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to add program to wishlist </summary>
public sealed record AddToWishlistCommand(Guid ProgramId, string UserId) : ICommand<ProgramWishlist>;
