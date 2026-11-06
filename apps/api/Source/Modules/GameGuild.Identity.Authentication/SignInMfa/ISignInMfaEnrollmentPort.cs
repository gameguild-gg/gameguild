namespace GameGuild.Identity.Authentication;

/// <summary>All operations require the same PostgreSQL transaction as challenge consumption and issuance.</summary>
public interface ISignInMfaEnrollmentPort
{
    Task AcquireSubjectLockAsync(Guid subjectId, CancellationToken cancellationToken);

    /// <summary>
    ///     Provisions or resumes the pending TOTP configuration for a sign-in-bound enrollment. The caller
    ///     must hold the subject lock, and the setup expires with the challenge it is bound to.
    /// </summary>
    Task<SignInMfaEnrollmentSetup> StartOrResumeAsync(Guid subjectId, string email, DateTimeOffset now,
        DateTimeOffset challengeExpiresAt, CancellationToken cancellationToken);

    Task<bool> MatchesAsync(SignInMfaChallenge challenge, bool enrolled, DateTimeOffset now,
        CancellationToken cancellationToken);
}

/// <summary>Internal integration data; only the secret, provisioning URI and expiry are returned for enrollment.</summary>
public sealed record SignInMfaEnrollmentSetup(Guid ConfigurationId, string SecretFingerprint, string SecretKey,
    string QrCodeUri, DateTimeOffset SetupExpiresAt);
