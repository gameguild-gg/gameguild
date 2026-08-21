using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Notifications.Services.Email;

/// <summary>
/// Canonical email address normalization for the Notifications module.
/// ALL suppression comparisons (dispatcher pre-send checks, event RecipientEmail storage,
/// deadletter sweeps, admin filters) MUST pass through <see cref="Normalize"/> — never
/// inline Trim/ToLower calls — so every side of a comparison normalizes identically.
/// </summary>
public static class EmailAddressNormalizer
{
    /// <summary>
    /// Trims surrounding whitespace and lowercases invariantly (culture-independent,
    /// so Turkish locale hosts still map 'I' to 'i'). Null is rejected up front;
    /// empty/whitespace strings pass through as empty and simply never match a real address.
    /// </summary>
    public static string Normalize(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        return email.Trim().ToLowerInvariant();
    }
}
