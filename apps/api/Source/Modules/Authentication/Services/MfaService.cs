using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameGuild.Database;
using GameGuild.Modules.Authentication.Models;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OtpNet;
using QRCoder;

namespace GameGuild.Modules.Authentication.Services;

/// <summary>
/// Service for managing Multi-Factor Authentication (MFA) including TOTP and backup codes
/// </summary>
public interface IMfaService {
    Task<MfaSetupResult> InitiateMfaSetupAsync(Guid userId);
    Task<MfaVerificationResult> CompleteMfaSetupAsync(Guid userId, string totpCode);
    Task<MfaVerificationResult> VerifyMfaAsync(Guid userId, string code, MfaMethod method = MfaMethod.Totp);
    Task<bool> DisableMfaAsync(Guid userId, string confirmationCode);
    Task<string[]> GenerateBackupCodesAsync(Guid userId);
    Task<byte[]> GenerateQrCodeAsync(string qrCodeData);
    Task<bool> IsMfaEnabledAsync(Guid userId);
    Task<bool> IsMfaRequiredAsync(Guid userId);
    Task ResetMfaFailedAttemptsAsync(Guid userId);
    Task<bool> IsUserLockedOutAsync(Guid userId);
}

public class MfaService : IMfaService {
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MfaService> _logger;
    private readonly MfaOptions _options;
    private readonly IEncryptionService _encryptionService;

    public MfaService(
      ApplicationDbContext context,
      ILogger<MfaService> logger,
      IOptions<MfaOptions> options,
      IEncryptionService encryptionService) {
        _context = context;
        _logger = logger;
        _options = options.Value;
        _encryptionService = encryptionService;
    }

    public async Task<MfaSetupResult> InitiateMfaSetupAsync(Guid userId) {
        try {
            var existingConfig = await _context.Set<UserMfaConfiguration>()
              .FirstOrDefaultAsync(x => x.UserId == userId);

            if (existingConfig?.IsEnabled == true) {
                throw new InvalidOperationException("MFA is already enabled for this user");
            }

            // Generate TOTP secret
            var secretKey = KeyGeneration.GenerateRandomKey(20);
            var base32Secret = Base32Encoding.ToString(secretKey);

            // Create QR code data
            var issuer = _options.Issuer;
            var accountTitle = $"{issuer}";
            var qrCodeData = $"otpauth://totp/{Uri.EscapeDataString(accountTitle)}?secret={base32Secret}&issuer={Uri.EscapeDataString(issuer)}";

            // Encrypt the secret before storing
            var encryptedSecret = await _encryptionService.EncryptAsync(base32Secret);

            if (existingConfig == null) {
                existingConfig = new UserMfaConfiguration {
                    UserId = userId,
                    TotpSecretKey = encryptedSecret,
                    QrCodeSetupData = qrCodeData,
                    IsEnabled = false,
                    IsSetupComplete = false
                };
                _context.Set<UserMfaConfiguration>().Add(existingConfig);
            }
            else {
                existingConfig.TotpSecretKey = encryptedSecret;
                existingConfig.QrCodeSetupData = qrCodeData;
                existingConfig.IsEnabled = false;
                existingConfig.IsSetupComplete = false;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("MFA setup initiated for user {UserId}", userId);

            return MfaSetupResult.Success(base32Secret, qrCodeData);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to initiate MFA setup for user {UserId}", userId);
            return MfaSetupResult.Failure("Failed to initiate MFA setup");
        }
    }

    public async Task<MfaVerificationResult> CompleteMfaSetupAsync(Guid userId, string totpCode) {
        try {
            var config = await _context.Set<UserMfaConfiguration>()
              .FirstOrDefaultAsync(x => x.UserId == userId);

            if (config == null || string.IsNullOrEmpty(config.TotpSecretKey)) {
                return MfaVerificationResult.Failure("MFA setup not initiated");
            }

            if (config.IsEnabled) {
                return MfaVerificationResult.Failure("MFA is already enabled");
            }

            // Decrypt secret and verify TOTP code
            var decryptedSecret = await _encryptionService.DecryptAsync(config.TotpSecretKey);
            var isValid = VerifyTotpCode(decryptedSecret, totpCode);

            if (!isValid) {
                await RecordMfaAttempt(userId, MfaMethod.Totp, false, "Invalid TOTP code during setup");
                return MfaVerificationResult.Failure("Invalid TOTP code");
            }

            // Generate backup codes
            var backupCodes = GenerateBackupCodes();
            var encryptedBackupCodes = await _encryptionService.EncryptAsync(JsonSerializer.Serialize(backupCodes));

            // Complete setup
            config.IsEnabled = true;
            config.IsSetupComplete = true;
            config.EnabledAt = DateTime.UtcNow;
            config.BackupCodes = encryptedBackupCodes;
            config.QrCodeSetupData = null; // Clear temporary QR data
            config.FailedAttempts = 0;
            config.LockedOutUntil = null;

            await _context.SaveChangesAsync();

            await RecordMfaAttempt(userId, MfaMethod.Totp, true, null);

            _logger.LogInformation("MFA setup completed for user {UserId}", userId);

            return MfaVerificationResult.Success("MFA enabled successfully", backupCodes.ToArray());
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to complete MFA setup for user {UserId}", userId);
            return MfaVerificationResult.Failure("Failed to complete MFA setup");
        }
    }

    public async Task<MfaVerificationResult> VerifyMfaAsync(Guid userId, string code, MfaMethod method = MfaMethod.Totp) {
        try {
            if (await IsUserLockedOutAsync(userId)) {
                return MfaVerificationResult.Failure("User is temporarily locked out due to failed attempts");
            }

            var config = await _context.Set<UserMfaConfiguration>()
              .FirstOrDefaultAsync(x => x.UserId == userId);

            if (config == null || !config.IsEnabled) {
                return MfaVerificationResult.Failure("MFA is not enabled for this user");
            }

            bool isValid = false;
            string? failureReason = null;

            switch (method) {
                case MfaMethod.Totp:
                    if (string.IsNullOrEmpty(config.TotpSecretKey)) {
                        failureReason = "TOTP not configured";
                        break;
                    }
                    var decryptedSecret = await _encryptionService.DecryptAsync(config.TotpSecretKey);
                    isValid = VerifyTotpCode(decryptedSecret, code);
                    if (!isValid) failureReason = "Invalid TOTP code";
                    break;

                case MfaMethod.BackupCode:
                    if (string.IsNullOrEmpty(config.BackupCodes)) {
                        failureReason = "Backup codes not configured";
                        break;
                    }
                    var decryptedBackupCodes = await _encryptionService.DecryptAsync(config.BackupCodes);
                    var backupCodes = JsonSerializer.Deserialize<List<string>>(decryptedBackupCodes) ?? new List<string>();
                    isValid = backupCodes.Contains(code);

                    if (isValid) {
                        // Remove used backup code
                        backupCodes.Remove(code);
                        config.BackupCodes = await _encryptionService.EncryptAsync(JsonSerializer.Serialize(backupCodes));
                    }
                    else {
                        failureReason = "Invalid backup code";
                    }
                    break;

                default:
                    failureReason = "Unsupported MFA method";
                    break;
            }

            if (isValid) {
                // Reset failed attempts and update last used
                config.FailedAttempts = 0;
                config.LockedOutUntil = null;
                config.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await RecordMfaAttempt(userId, method, true, null);
                return MfaVerificationResult.Success("MFA verification successful");
            }
            else {
                // Increment failed attempts
                config.FailedAttempts++;

                // Lock out user if too many failed attempts
                if (config.FailedAttempts >= _options.MaxFailedAttempts) {
                    config.LockedOutUntil = DateTime.UtcNow.AddMinutes(_options.LockoutDurationMinutes);
                    _logger.LogWarning("User {UserId} locked out due to {FailedAttempts} failed MFA attempts",
                      userId, config.FailedAttempts);
                }

                await _context.SaveChangesAsync();
                await RecordMfaAttempt(userId, method, false, failureReason);

                return MfaVerificationResult.Failure(failureReason ?? "MFA verification failed");
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to verify MFA for user {UserId}", userId);
            return MfaVerificationResult.Failure("MFA verification failed");
        }
    }

    public async Task<bool> DisableMfaAsync(Guid userId, string confirmationCode) {
        try {
            var verificationResult = await VerifyMfaAsync(userId, confirmationCode);
            if (!verificationResult.IsSuccess) {
                return false;
            }

            var config = await _context.Set<UserMfaConfiguration>()
              .FirstOrDefaultAsync(x => x.UserId == userId);

            if (config != null) {
                config.IsEnabled = false;
                config.TotpSecretKey = null;
                config.BackupCodes = null;
                config.EnabledAt = null;
                config.FailedAttempts = 0;
                config.LockedOutUntil = null;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("MFA disabled for user {UserId}", userId);
            return true;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to disable MFA for user {UserId}", userId);
            return false;
        }
    }

    public async Task<string[]> GenerateBackupCodesAsync(Guid userId) {
        try {
            var config = await _context.Set<UserMfaConfiguration>()
              .FirstOrDefaultAsync(x => x.UserId == userId);

            if (config == null || !config.IsEnabled) {
                throw new InvalidOperationException("MFA is not enabled for this user");
            }

            var backupCodes = GenerateBackupCodes();
            var encryptedBackupCodes = await _encryptionService.EncryptAsync(JsonSerializer.Serialize(backupCodes));

            config.BackupCodes = encryptedBackupCodes;
            await _context.SaveChangesAsync();

            _logger.LogInformation("New backup codes generated for user {UserId}", userId);
            return backupCodes.ToArray();
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to generate backup codes for user {UserId}", userId);
            throw;
        }
    }

    public async Task<byte[]> GenerateQrCodeAsync(string qrCodeData) {
        try {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeInfo = qrGenerator.CreateQrCode(qrCodeData, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeInfo);
            return qrCode.GetGraphic(20);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to generate QR code");
            throw;
        }
    }

    public async Task<bool> IsMfaEnabledAsync(Guid userId) {
        var config = await _context.Set<UserMfaConfiguration>()
          .FirstOrDefaultAsync(x => x.UserId == userId);
        return config?.IsEnabled == true;
    }

    public async Task<bool> IsMfaRequiredAsync(Guid userId) {
        // This could be enhanced to check user roles, policies, etc.
        return await IsMfaEnabledAsync(userId);
    }

    public async Task ResetMfaFailedAttemptsAsync(Guid userId) {
        var config = await _context.Set<UserMfaConfiguration>()
          .FirstOrDefaultAsync(x => x.UserId == userId);

        if (config != null) {
            config.FailedAttempts = 0;
            config.LockedOutUntil = null;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsUserLockedOutAsync(Guid userId) {
        var config = await _context.Set<UserMfaConfiguration>()
          .FirstOrDefaultAsync(x => x.UserId == userId);

        return config?.LockedOutUntil.HasValue == true &&
               config.LockedOutUntil.Value > DateTime.UtcNow;
    }

    private bool VerifyTotpCode(string secretKey, string code) {
        try {
            var secretBytes = Base32Encoding.ToBytes(secretKey);
            var totp = new Totp(secretBytes);

            // Allow some time drift (previous, current, next window)
            var currentTime = DateTime.UtcNow;

            return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to verify TOTP code");
            return false;
        }
    }

    private List<string> GenerateBackupCodes() {
        var codes = new List<string>();

        for (int i = 0; i < _options.BackupCodeCount; i++) {
            codes.Add(GenerateBackupCode());
        }

        return codes;
    }

    private string GenerateBackupCode() {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private async Task RecordMfaAttempt(Guid userId, MfaMethod method, bool isSuccessful, string? failureReason) {
        try {
            var attempt = new MfaAttempt {
                UserId = userId,
                Method = method,
                IsSuccessful = isSuccessful,
                IpAddress = "unknown", // Would need HttpContext to get real IP
                UserAgent = "unknown", // Would need HttpContext to get real user agent
                FailureReason = failureReason
            };

            _context.Set<MfaAttempt>().Add(attempt);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to record MFA attempt for user {UserId}", userId);
        }
    }
}

/// <summary>
/// Configuration options for MFA
/// </summary>
public class MfaOptions {
    public string Issuer { get; set; } = "GameGuild";
    public int MaxFailedAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 15;
    public int BackupCodeCount { get; set; } = 10;
    public int TotpWindowSeconds { get; set; } = 30;
}

/// <summary>
/// Result of MFA setup operation
/// </summary>
public class MfaSetupResult {
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? SecretKey { get; private set; }
    public string? QrCodeData { get; private set; }

    private MfaSetupResult(bool isSuccess, string? errorMessage = null, string? secretKey = null, string? qrCodeData = null) {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        SecretKey = secretKey;
        QrCodeData = qrCodeData;
    }

    public static MfaSetupResult Success(string secretKey, string qrCodeData) =>
      new(true, null, secretKey, qrCodeData);

    public static MfaSetupResult Failure(string errorMessage) =>
      new(false, errorMessage);
}

/// <summary>
/// Result of MFA verification operation
/// </summary>
public class MfaVerificationResult {
    public bool IsSuccess { get; private set; }
    public string? Message { get; private set; }
    public string[]? BackupCodes { get; private set; }

    private MfaVerificationResult(bool isSuccess, string? message = null, string[]? backupCodes = null) {
        IsSuccess = isSuccess;
        Message = message;
        BackupCodes = backupCodes;
    }

    public static MfaVerificationResult Success(string message, string[]? backupCodes = null) =>
      new(true, message, backupCodes);

    public static MfaVerificationResult Failure(string message) =>
      new(false, message);
}
