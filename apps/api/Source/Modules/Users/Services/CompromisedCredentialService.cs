using System.Security.Cryptography;
using System.Text;
using GameGuild;
using GameGuild.Modules.Users;
using GameGuild.Modules.Users.Entities;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Users.Services;

/// <summary>
/// Service interface for checking compromised credentials using HIBP-style integration.
/// </summary>
public interface ICompromisedCredentialService
{
    /// <summary>
    /// Checks if a credential has been compromised in known data breaches.
    /// </summary>
    Task<Result<CredentialCheckResult>> CheckCredentialAsync(string credential, string credentialType = "Password");

    /// <summary>
    /// Checks a user's credential and records the result.
    /// </summary>
    Task<Result<CredentialCheckResult>> CheckUserCredentialAsync(Guid userId, string credential, string? ipAddress = null);

    /// <summary>
    /// Gets all compromised credentials for a user.
    /// </summary>
    Task<List<CompromisedCredential>> GetUserCompromisedCredentialsAsync(Guid userId, bool activeOnly = true);

    /// <summary>
    /// Acknowledges a compromised credential.
    /// </summary>
    Task<Result> AcknowledgeCompromiseAsync(Guid compromiseId);

    /// <summary>
    /// Resolves a compromised credential (marks as fixed).
    /// </summary>
    Task<Result> ResolveCompromiseAsync(Guid compromiseId, string resolutionAction);

    /// <summary>
    /// Ignores a compromised credential.
    /// </summary>
    Task<Result> IgnoreCompromiseAsync(Guid compromiseId);

    /// <summary>
    /// Scans all users for compromised credentials (background job).
    /// </summary>
    Task<Result<int>> ScanAllUsersAsync();

    /// <summary>
    /// Gets compromise statistics for a user.
    /// </summary>
    Task<CompromiseStatistics> GetUserStatisticsAsync(Guid userId);
}

/// <summary>
/// Result of a credential check.
/// </summary>
public class CredentialCheckResult
{
    public bool IsCompromised { get; set; }
    public int BreachCount { get; set; }
    public BreachSeverity Severity { get; set; }
    public string? BreachName { get; set; }
    public DateTime? BreachDate { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Statistics about a user's compromised credentials.
/// </summary>
public class CompromiseStatistics
{
    public int TotalCompromises { get; set; }
    public int ActiveCompromises { get; set; }
    public int ResolvedCompromises { get; set; }
    public int IgnoredCompromises { get; set; }
    public BreachSeverity HighestSeverity { get; set; }
    public DateTime? LastCheckDate { get; set; }
    public DateTime? LastCompromiseDate { get; set; }
}

/// <summary>
/// Service implementation for compromised credential detection.
/// </summary>
public class CompromisedCredentialService : ICompromisedCredentialService
{
    private readonly IRepository<CompromisedCredential> _compromiseRepository;
    private readonly IRepository<CredentialCheckLog> _checkLogRepository;
    private readonly IRepository<User> _userRepository;
    private readonly ILogger<CompromisedCredentialService> _logger;

    public CompromisedCredentialService(
        IRepository<CompromisedCredential> compromiseRepository,
        IRepository<CredentialCheckLog> checkLogRepository,
        IRepository<User> userRepository,
        ILogger<CompromisedCredentialService> logger)
    {
        _compromiseRepository = compromiseRepository;
        _checkLogRepository = checkLogRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<CredentialCheckResult>> CheckCredentialAsync(
        string credential,
        string credentialType = "Password")
    {
        // Hash the credential (SHA-256)
        var hash = ComputeSha256Hash(credential);

        // In a real implementation, this would call HIBP API or internal breach database
        // For now, we simulate the check with a basic pattern check
        var result = await SimulateBreachCheckAsync(hash);

        _logger.LogInformation(
            "Credential check completed. Hash: {HashPrefix}..., Compromised: {IsCompromised}",
            hash.Substring(0, 10), result.IsCompromised);

        return Result<CredentialCheckResult>.Success(result);
    }

    public async Task<Result<CredentialCheckResult>> CheckUserCredentialAsync(
        Guid userId,
        string credential,
        string? ipAddress = null)
    {
        // Validate user exists
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return Result<CredentialCheckResult>.Failure("User not found");
        }

        var hash = ComputeSha256Hash(credential);

        // Check if already logged
        var existingCheck = await _checkLogRepository.FindAsync(
            c => c.UserId == userId && c.CredentialHash == hash);

        if (existingCheck.Any() && existingCheck.First().CheckedAt > DateTime.UtcNow.AddHours(-24))
        {
            // Use cached result if checked within last 24 hours
            var cached = existingCheck.First();
            return Result<CredentialCheckResult>.Success(new CredentialCheckResult
            {
                IsCompromised = cached.IsCompromised,
                BreachCount = cached.BreachCount,
                Severity = cached.IsCompromised ? BreachSeverity.Medium : BreachSeverity.Low,
                Message = cached.IsCompromised ? "Credential found in data breach (cached result)" : "Credential is secure"
            });
        }

        // Perform actual check
        var checkResult = await CheckCredentialAsync(credential);
        if (!checkResult.IsSuccess)
        {
            return checkResult;
        }

        var result = checkResult.Data!;

        // Log the check
        var checkLog = new CredentialCheckLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CredentialHash = hash,
            CheckedAt = DateTime.UtcNow,
            CheckService = "HIBP",
            IsCompromised = result.IsCompromised,
            BreachCount = result.BreachCount,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _checkLogRepository.AddAsync(checkLog);

        // If compromised, create or update compromise record
        if (result.IsCompromised)
        {
            var existingCompromise = await _compromiseRepository.FindAsync(
                c => c.UserId == userId && c.CredentialHash == hash && c.Status != CompromiseStatus.Resolved);

            if (!existingCompromise.Any())
            {
                var compromise = new CompromisedCredential
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CredentialHash = hash,
                    CredentialType = "Password",
                    DetectedAt = DateTime.UtcNow,
                    Source = "HIBP",
                    BreachName = result.BreachName,
                    BreachDate = result.BreachDate,
                    Severity = result.Severity,
                    BreachCount = result.BreachCount,
                    Status = CompromiseStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _compromiseRepository.AddAsync(compromise);

                _logger.LogWarning(
                    "Compromised credential detected for user {UserId}. Severity: {Severity}, Breach count: {Count}",
                    userId, result.Severity, result.BreachCount);
            }
        }

        return Result<CredentialCheckResult>.Success(result);
    }

    public async Task<List<CompromisedCredential>> GetUserCompromisedCredentialsAsync(
        Guid userId,
        bool activeOnly = true)
    {
        if (activeOnly)
        {
            return await _compromiseRepository.FindAsync(
                c => c.UserId == userId && (c.Status == CompromiseStatus.Active || c.Status == CompromiseStatus.Acknowledged));
        }

        return await _compromiseRepository.FindAsync(c => c.UserId == userId);
    }

    public async Task<Result> AcknowledgeCompromiseAsync(Guid compromiseId)
    {
        var compromise = await _compromiseRepository.GetByIdAsync(compromiseId);
        if (compromise == null)
        {
            return Result.Failure("Compromise not found");
        }

        compromise.Acknowledge();
        await _compromiseRepository.UpdateAsync(compromise);

        _logger.LogInformation("Compromise {Id} acknowledged by user {UserId}", compromiseId, compromise.UserId);

        return Result.Success();
    }

    public async Task<Result> ResolveCompromiseAsync(Guid compromiseId, string resolutionAction)
    {
        var compromise = await _compromiseRepository.GetByIdAsync(compromiseId);
        if (compromise == null)
        {
            return Result.Failure("Compromise not found");
        }

        compromise.Resolve(resolutionAction);
        await _compromiseRepository.UpdateAsync(compromise);

        _logger.LogInformation(
            "Compromise {Id} resolved by user {UserId}. Action: {Action}",
            compromiseId, compromise.UserId, resolutionAction);

        return Result.Success();
    }

    public async Task<Result> IgnoreCompromiseAsync(Guid compromiseId)
    {
        var compromise = await _compromiseRepository.GetByIdAsync(compromiseId);
        if (compromise == null)
        {
            return Result.Failure("Compromise not found");
        }

        compromise.Ignore();
        await _compromiseRepository.UpdateAsync(compromise);

        _logger.LogInformation("Compromise {Id} ignored by user {UserId}", compromiseId, compromise.UserId);

        return Result.Success();
    }

    public async Task<Result<int>> ScanAllUsersAsync()
    {
        // This would be implemented as a background job in production
        // For now, return a placeholder
        _logger.LogInformation("Scanning all users for compromised credentials");

        var scannedCount = 0;

        // TODO: Implement actual scanning logic
        // This would involve:
        // 1. Get all active users
        // 2. Check their password hashes against breach database
        // 3. Create compromise records for any matches

        return Result<int>.Success(scannedCount);
    }

    public async Task<CompromiseStatistics> GetUserStatisticsAsync(Guid userId)
    {
        var compromises = await _compromiseRepository.FindAsync(c => c.UserId == userId);
        var checkLogs = await _checkLogRepository.FindAsync(c => c.UserId == userId);

        return new CompromiseStatistics
        {
            TotalCompromises = compromises.Count,
            ActiveCompromises = compromises.Count(c => c.Status == CompromiseStatus.Active),
            ResolvedCompromises = compromises.Count(c => c.Status == CompromiseStatus.Resolved),
            IgnoredCompromises = compromises.Count(c => c.Status == CompromiseStatus.Ignored),
            HighestSeverity = compromises.Any() ? compromises.Max(c => c.Severity) : BreachSeverity.Low,
            LastCheckDate = checkLogs.Any() ? checkLogs.Max(c => c.CheckedAt) : null,
            LastCompromiseDate = compromises.Any() ? compromises.Max(c => c.DetectedAt) : null
        };
    }

    private string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes);
    }

    private Task<CredentialCheckResult> SimulateBreachCheckAsync(string hash)
    {
        // Simulate HIBP API response
        // In production, this would call actual HIBP API or internal database

        // For simulation: check if hash starts with certain patterns (demo purposes)
        var isCompromised = hash.StartsWith("A") || hash.StartsWith("B") || hash.StartsWith("1");

        if (isCompromised)
        {
            return Task.FromResult(new CredentialCheckResult
            {
                IsCompromised = true,
                BreachCount = Random.Shared.Next(1, 100),
                Severity = Random.Shared.Next(0, 4) switch
                {
                    0 => BreachSeverity.Low,
                    1 => BreachSeverity.Medium,
                    2 => BreachSeverity.High,
                    _ => BreachSeverity.Critical
                },
                BreachName = "Collection #1",
                BreachDate = DateTime.UtcNow.AddMonths(-Random.Shared.Next(1, 36)),
                Message = "This password has been seen in data breaches. Please change it immediately."
            });
        }

        return Task.FromResult(new CredentialCheckResult
        {
            IsCompromised = false,
            BreachCount = 0,
            Severity = BreachSeverity.Low,
            Message = "This credential has not been found in any known data breaches."
        });
    }
}
