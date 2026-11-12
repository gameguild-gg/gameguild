namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for MFA backup code operations.
///     Handles backup code generation, hashing, and verification.
/// </summary>
public interface IBackupCodeMfaService
{
    /// <summary>
    ///     Generates backup codes for account recovery.
    /// </summary>
    Task<string[]> GenerateBackupCodesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Verifies a backup code and invalidates it (single-use).
    /// </summary>
    Task<bool> VerifyBackupCodeAsync(Guid userId, string backupCode, string? deviceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Generates a random 8-character alphanumeric backup code.
    /// </summary>
    string GenerateBackupCode();

    /// <summary>
    ///     Hashes a backup code for secure storage.
    /// </summary>
    Task<string> HashBackupCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores hashed backup codes for a user during MFA setup (before MFA is fully enabled).
    /// </summary>
    Task StoreBackupCodesForSetupAsync(Guid userId, IReadOnlyList<string> plainTextCodes, CancellationToken cancellationToken = default);
}
