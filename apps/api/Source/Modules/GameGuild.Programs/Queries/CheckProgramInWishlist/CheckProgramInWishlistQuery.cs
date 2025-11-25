using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Queries;

/// <summary> Query to check if program is in user's wishlist </summary>
public record CheckProgramInWishlistQuery(Guid ProgramId, string UserId) : IQuery<bool>;
