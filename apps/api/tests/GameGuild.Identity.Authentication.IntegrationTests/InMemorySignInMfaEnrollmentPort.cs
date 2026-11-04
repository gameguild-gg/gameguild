using System.Security.Cryptography;
using System.Text;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Authentication;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Tests.Authentication.Integration;

/// <summary>
/// Provider-agnostic stand-in for <see cref="PostgreSqlSignInMfaEnrollmentPort"/> used by the
/// in-memory <see cref="AuthenticationApiFactory"/>. Mirrors the PostgreSQL port's provisioning
/// and challenge-matching semantics against the same EF repositories, minus the advisory-lock
/// SQL (there is no owning Npgsql transaction to serialize on in-memory).
/// </summary>
public sealed class InMemorySignInMfaEnrollmentPort(IApplicationDbContext context,
    IUserMfaConfigurationRepository configurations, IEncryptionService encryption, MfaOptions options) :
    ISignInMfaEnrollmentPort
{
    public Task AcquireSubjectLockAsync(Guid subjectId, CancellationToken cancellationToken)
    {
        if (subjectId == Guid.Empty) { throw new ArgumentException("A subject is required.", nameof(subjectId)); }
        return Task.CompletedTask;
    }

    public async Task<SignInMfaEnrollmentSetup> StartOrResumeAsync(Guid subjectId, string email, DateTimeOffset now,
        DateTimeOffset challengeExpiresAt, CancellationToken cancellationToken)
    {
        if (subjectId == Guid.Empty) { throw new ArgumentException("A subject is required.", nameof(subjectId)); }
        if (string.IsNullOrWhiteSpace(email)) { throw new ArgumentException("An email is required.", nameof(email)); }
        if (!options.Enabled) { throw new InvalidOperationException("Limited MFA enrollment requires enabled MFA options."); }
        if (challengeExpiresAt <= now) { throw new ArgumentOutOfRangeException(nameof(challengeExpiresAt)); }

        var secret = GenerateBase32Secret();
        var configuration = await configurations.GetByUserIdAsync(subjectId, cancellationToken).ConfigureAwait(false);
        if (configuration is { IsEnabled: true })
        {
            throw new InvalidOperationException("The subject already has an active MFA configuration.");
        }
        var created = configuration is null;
        configuration ??= new UserMfaConfiguration { Id = Guid.NewGuid(), UserId = subjectId };
        configuration.PreferredMethod = MfaMethod.Totp;
        configuration.TotpSecretKey = encryption.Encrypt(secret);
        configuration.IsEnabled = false;
        configuration.IsSetupComplete = false;
        configuration.SetupExpiresAt = challengeExpiresAt.UtcDateTime;
        configuration.BackupCodes = null;
        configuration.QrCodeSetupData = null;
        configuration = created
            ? await configurations.CreateAsync(configuration, cancellationToken).ConfigureAwait(false)
            : await configurations.UpdateAsync(configuration, cancellationToken).ConfigureAwait(false);
        return new(configuration.Id, Fingerprint(secret), secret,
            GenerateTotpUri(email, secret, options.TotpIssuer, options.TotpTimeStepSeconds), challengeExpiresAt);
    }

    public async Task<bool> MatchesAsync(SignInMfaChallenge challenge, bool enrolled, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var configuration = await context.Set<UserMfaConfiguration>().AsNoTracking()
            .SingleOrDefaultAsync(row => row.UserId == challenge.SubjectId, cancellationToken).ConfigureAwait(false);
        return Matches(challenge, configuration, enrolled, now);
    }

    private bool Matches(SignInMfaChallenge challenge, UserMfaConfiguration? configuration, bool enrolled, DateTimeOffset now)
    {
        if (!options.Enabled || challenge.Purpose != SignInMfaPurpose.EnrollFactor || challenge.ExpiresAt <= now ||
            challenge.EnrollmentConfigurationId is not { } configurationId || configurationId == Guid.Empty ||
            challenge.EnrollmentInitializedAt is not { } initialized || initialized < challenge.CreatedAt ||
            initialized > now || initialized >= challenge.ExpiresAt ||
            !IsDigest(challenge.EnrollmentSecretFingerprint ?? string.Empty) ||
            configuration is null || configuration.Id != configurationId ||
            configuration.IsEnabled != enrolled || configuration.IsSetupComplete != enrolled ||
            configuration.LockedOutUntil is { } lockedUntil && lockedUntil > now.UtcDateTime ||
            string.IsNullOrWhiteSpace(configuration.TotpSecretKey)) { return false; }
        if (!enrolled && (configuration.SetupExpiresAt is not { } expires || expires <= now.UtcDateTime ||
            expires > challenge.ExpiresAt.UtcDateTime)) { return false; }
        return string.Equals(Fingerprint(encryption.Decrypt(configuration.TotpSecretKey)),
            challenge.EnrollmentSecretFingerprint, StringComparison.Ordinal);
    }

    private static bool IsDigest(string? value) => value is { Length: 64 } &&
        value.All(character => char.IsAsciiDigit(character) || character is >= 'a' and <= 'f');

    private static string Fingerprint(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(
        secret.ToUpperInvariant().Replace(" ", "").Replace("-", "")))).ToLowerInvariant();

    private static string GenerateBase32Secret()
    {
        const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var secret = new char[32];
        for (var i = 0; i < secret.Length; i++) { secret[i] = base32Chars[RandomNumberGenerator.GetInt32(base32Chars.Length)]; }
        return new string(secret);
    }

    private static string GenerateTotpUri(string userEmail, string secretKey, string issuer, int timeStepSeconds)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(userEmail);
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secretKey}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period={timeStepSeconds}";
    }
}
