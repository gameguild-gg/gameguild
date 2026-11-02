namespace GameGuild.Identity.Authentication;

/// <summary>Result of starting an email-MFA enrollment or challenge.</summary>
public sealed class EmailMfaChallengeResult
{
    /// <summary>Whether a code was generated and queued for delivery.</summary>
    public required bool Success { get; init; }

    /// <summary>Masked destination email address for user confirmation.</summary>
    public required string EmailMasked { get; init; }

    /// <summary>Seconds until the issued code expires.</summary>
    public required int ExpiresInSeconds { get; init; }

    /// <summary>Failure message when <see cref="Success"/> is false.</summary>
    public string? Message { get; init; }
}

/// <summary>
///     Issues and verifies one-time email-MFA codes (issue #1152). The service mirrors the
///     SMS OTP factor's security semantics: codes are six cryptographically random digits,
///     live for ten minutes, accept at most five verification attempts, are single-use,
///     throttled to one request per minute per user, and stored only as SHA-256 digests.
///     Delivery rides the Notifications email channel to the account's verified address.
/// </summary>
public interface IEmailMfaService
{
    /// <summary>Starts enrollment: sends a verification code to the account's email address for the authenticated user.</summary>
    Task<EmailMfaChallengeResult> StartEnrollmentAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Completes enrollment by verifying the code sent to the pending email address.</summary>
    Task<MfaVerificationResult> CompleteEnrollmentAsync(Guid userId, string code, CancellationToken cancellationToken = default);

    /// <summary>Sends a fresh sign-in challenge to the user's enrolled email address (throttled).</summary>
    Task<EmailMfaChallengeResult> SendChallengeAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Verifies a submitted email-MFA code (constant-time, single-use).</summary>
    Task<bool> VerifyCodeAsync(Guid userId, string code, CancellationToken cancellationToken = default);
}
