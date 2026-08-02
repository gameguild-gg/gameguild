namespace GameGuild.Identity.Authentication;

/// <summary>
///     The verified claims of a Google ID token after cryptographic validation
///     by <see cref="IGoogleIdTokenVerifier" />. All fields come from the
///     Google-signed payload — never trust these claims without first passing
///     the token through <c>GoogleJsonWebSignature.ValidateAsync</c>.
/// </summary>
public sealed record VerifiedGoogleUser
{
    /// <summary>Google-stable subject identifier (the <c>sub</c> claim).</summary>
    public required string Sub { get; init; }

    /// <summary>The <c>email</c> claim. May be null if the token omitted the email scope.</summary>
    public required string Email { get; init; }

    /// <summary>True only when Google asserts the email is verified.</summary>
    public required bool EmailVerified { get; init; }

    /// <summary>Display name from the <c>name</c> claim, if present.</summary>
    public string? Name { get; init; }

    /// <summary>Avatar URL from the <c>picture</c> claim, if present.</summary>
    public string? Picture { get; init; }
}
