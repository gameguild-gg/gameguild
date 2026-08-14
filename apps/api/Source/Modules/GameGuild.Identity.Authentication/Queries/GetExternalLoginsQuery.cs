using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Query to list the external logins linked to a user, newest first.
/// </summary>
public sealed record GetExternalLoginsQuery : IQuery<List<ExternalLoginDto>>
{
    public required Guid UserId { get; init; }
}
