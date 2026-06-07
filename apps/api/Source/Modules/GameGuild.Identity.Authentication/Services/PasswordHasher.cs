using System.Text.RegularExpressions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Password hashing service using BCrypt or Argon2.
///     Provides password hashing, verification, strength validation, and rehashing detection.
/// </summary>
public sealed class PasswordHasher(ILogger<PasswordHasher> logger, IConfiguration configuration) : IPasswordHasher
{
    // BCrypt work factor (cost parameter) - higher is more secure but slower
    // Recommended: 12-14 for production (2^12 to 2^14 iterations)
    private const int BCryptWorkFactor = 12;

    // Password policy — loaded from configuration section "PasswordPolicy", with secure defaults
    private int MinPasswordLength => configuration.GetValue("PasswordPolicy:MinPasswordLength", 8);

    private int MaxPasswordLength => configuration.GetValue("PasswordPolicy:MaxPasswordLength", 128);

    private bool RequireUppercase => configuration.GetValue("PasswordPolicy:RequireUppercase", true);

    private bool RequireLowercase => configuration.GetValue("PasswordPolicy:RequireLowercase", true);

    private bool RequireDigit => configuration.GetValue("PasswordPolicy:RequireDigit", true);

    private bool RequireSpecialChar => configuration.GetValue("PasswordPolicy:RequireSpecialChar", true);

    /// <summary>
    ///     Hashes a password using BCrypt algorithm.
    /// </summary>
    public Task<string> HashPasswordAsync(string password, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HashPassword(password));
    }

    /// <summary>
    ///     Verifies a password against its hash.
    /// </summary>
    public Task<bool> VerifyPasswordAsync(string passwordHash, string providedPassword, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(VerifyPassword(passwordHash, providedPassword));
    }

    /// <summary>
    ///     Validates password strength against policy requirements.
    /// </summary>
    public Task<PasswordStrengthResult> ValidatePasswordStrengthAsync(string password, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ValidatePasswordStrength(password));
    }

    /// <summary>
    ///     Checks if a password hash needs to be rehashed (e.g., due to increased work factor).
    /// </summary>
    public Task<bool> NeedsRehashAsync(string passwordHash, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(NeedsUpgrade(passwordHash));
    }

    #region Private Helper Methods

    /// <summary>
    ///     Calculates password strength score (0-100).
    /// </summary>
    private int CalculatePasswordStrength(string password)
    {
        var score = 0;

        // Length score (max 25 points)
        score += Math.Min(password.Length * 2, 25);

        // Character variety score (max 40 points)
        if (Regex.IsMatch(password, @"[a-z]")) score += 10;
        if (Regex.IsMatch(password, @"[A-Z]")) score += 10;
        if (Regex.IsMatch(password, @"[0-9]")) score += 10;
        if (Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]")) score += 10;

        // Complexity bonus (max 35 points)
        var uniqueChars = password.Distinct().Count();
        score += Math.Min(uniqueChars * 2, 20);

        // Entropy bonus
        if (password.Length >= 12 && uniqueChars >= 10) { score += 15; }

        // Penalty for repeated characters
        if (Regex.IsMatch(password, @"(.)\1{2,}")) { score -= 10; }

        // Penalty for sequential characters
        if (ContainsSequentialCharacters(password)) { score -= 10; }

        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    ///     Checks if password contains sequential characters (abc, 123, etc.).
    /// </summary>
    private bool ContainsSequentialCharacters(string password)
    {
        for (var i = 0; i < password.Length - 2; i++)
        {
            var char1 = password[i];
            var char2 = password[i + 1];
            var char3 = password[i + 2];

            // Check ascending sequence
            if (char2 == char1 + 1 && char3 == char2 + 1) { return true; }

            // Check descending sequence
            if (char2 == char1 - 1 && char3 == char2 - 1) { return true; }
        }

        return false;
    }

    #endregion

    #region Interface Implementation (Synchronous Methods)

    /// <summary>
    ///     Hashes a password using BCrypt algorithm.
    /// </summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) { throw new ArgumentException("Password cannot be empty", nameof(password)); }

        logger.LogDebug("Hashing password with BCrypt (work factor: {WorkFactor})", BCryptWorkFactor);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, BCryptWorkFactor);
        logger.LogDebug("Password hashed successfully");
        return passwordHash;
    }

    /// <summary>
    ///     Verifies a password against its hash.
    /// </summary>
    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(providedPassword)) { return false; }

        try
        {
            logger.LogDebug("Verifying password");
            var isValid = BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
            logger.LogDebug("Password verification result: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying password");
            return false;
        }
    }

    /// <summary>
    ///     Checks if a password hash needs rehashing (e.g., due to increased work factor).
    /// </summary>
    public bool NeedsUpgrade(string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword)) { return false; }

        var parts = hashedPassword.Split('$');

        if (parts.Length < 3)
        {
            logger.LogWarning("Invalid BCrypt hash format");
            return true;
        }

        if (!int.TryParse(parts[2], out var currentWorkFactor))
        {
            logger.LogWarning("Cannot parse BCrypt work factor");
            return true;
        }

        var needsRehash = currentWorkFactor < BCryptWorkFactor;

        if (needsRehash) { logger.LogInformation("Password hash needs rehashing: Current work factor {Current}, Required {Required}", currentWorkFactor, BCryptWorkFactor); }

        return needsRehash;
    }

    /// <summary>
    ///     Validates password strength against policy requirements.
    /// </summary>
    public PasswordStrengthResult ValidatePasswordStrength(string password)
    {
        var result = new PasswordStrengthResult { IsValid = true, ValidationFailures = new List<string>() };

        if (string.IsNullOrWhiteSpace(password))
        {
            result.IsValid = false;
            result.ValidationFailures.Add("Password is required");
            return result;
        }

        if (password.Length < MinPasswordLength)
        {
            result.IsValid = false;
            result.ValidationFailures.Add($"Password must be at least {MinPasswordLength} characters long");
        }

        if (password.Length > MaxPasswordLength)
        {
            result.IsValid = false;
            result.ValidationFailures.Add($"Password must not exceed {MaxPasswordLength} characters");
        }

        if (RequireUppercase && !Regex.IsMatch(password, @"[A-Z]"))
        {
            result.IsValid = false;
            result.ValidationFailures.Add("Password must contain at least one uppercase letter");
        }

        if (RequireLowercase && !Regex.IsMatch(password, @"[a-z]"))
        {
            result.IsValid = false;
            result.ValidationFailures.Add("Password must contain at least one lowercase letter");
        }

        if (RequireDigit && !Regex.IsMatch(password, @"[0-9]"))
        {
            result.IsValid = false;
            result.ValidationFailures.Add("Password must contain at least one digit");
        }

        if (RequireSpecialChar && !Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]"))
        {
            result.IsValid = false;
            result.ValidationFailures.Add("Password must contain at least one special character");
        }

        var commonPasswords = new[] { "password", "12345678", "qwerty", "abc123", "password1", "Password1", "Password123", "Welcome1", "Admin123" };

        if (commonPasswords.Contains(password, StringComparer.OrdinalIgnoreCase))
        {
            result.IsValid = false;
            result.ValidationFailures.Add("Password is too common and easily guessable");
        }

        result.StrengthScore = CalculatePasswordStrength(password);

        result.StrengthLevel = result.StrengthScore switch
        {
            >= 80 => "Strong",
            >= 60 => "Good",
            >= 40 => "Fair",
            >= 20 => "Weak",
            _ => "Very Weak"
        };

        logger.LogDebug("Password strength validation: {IsValid}, Score: {Score}, Level: {Level}", result.IsValid, result.StrengthScore, result.StrengthLevel);

        return result;
    }

    #endregion
}
