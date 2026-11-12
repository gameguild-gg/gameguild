using System.Security.Claims;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Issues and validates guest access tokens (issue #213). Guest tokens are HS256 JWTs in
///     their own audience namespace (<see cref="GuestAccessDefaults.Audience"/>) carrying only
///     the guest session id and the guest role — never a user id, email or tenant claim.
/// </summary>
public interface IGuestTokenService
{
    /// <summary>
    ///     Issues a short-lived guest token for an already-persisted guest session. The token
    ///     expires exactly at <paramref name="expiresAt"/>.
    /// </summary>
    string Issue(Guid guestSessionId, DateTime expiresAt);

    /// <summary>
    ///     Validates a guest token (signature, issuer, guest audience, lifetime) and returns
    ///     the principal. Returns null for any invalid or non-guest token; never throws for
    ///     malformed input.
    /// </summary>
    ClaimsPrincipal? Validate(string? guestToken);

    /// <summary>
    ///     Reads a guest token whose signature, issuer and guest audience are valid but whose
    ///     lifetime has PASSED (issue #1128): the guest session expired mid-use and the caller
    ///     wants to know which session it named and when it expired, so the upgrade path can
    ///     decide whether the still-retained snapshot qualifies for continuity migration.
    ///     Returns false for malformed, badly signed or non-guest tokens; a structurally valid
    ///     token that has not expired yet is reported with its actual expiry.
    /// </summary>
    bool TryReadExpired(string? guestToken, out Guid guestSessionId, out DateTime expiresAt);
}
