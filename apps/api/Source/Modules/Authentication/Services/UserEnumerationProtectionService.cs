using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace GameGuild.Modules.Authentication.Services;

/// <summary>
/// Service interface for user enumeration protection
/// </summary>
public interface IUserEnumerationProtectionService {
    /// <summary>
    /// Simulates authentication processing time to prevent timing attacks
    /// </summary>
    Task SimulateAuthenticationDelayAsync(string email, bool userExists);

    /// <summary>
    /// Gets a consistent error message that doesn't reveal whether a user exists
    /// </summary>
    string GetConsistentErrorMessage();

    /// <summary>
    /// Performs a dummy password hash operation to maintain consistent timing
    /// </summary>
    Task PerformDummyPasswordHashAsync(string password);

    /// <summary>
    /// Gets the base authentication processing time for timing consistency
    /// </summary>
    TimeSpan GetBaseProcessingTime();
}

/// <summary>
/// Service to protect against user enumeration attacks by ensuring consistent timing and responses
/// </summary>
public class UserEnumerationProtectionService : IUserEnumerationProtectionService {
    private readonly ILogger<UserEnumerationProtectionService> _logger;
    private readonly IConfiguration _configuration;

    // Timing constants
    private static readonly TimeSpan MinProcessingTime = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan MaxProcessingTime = TimeSpan.FromMilliseconds(800);
    private static readonly TimeSpan TargetProcessingTime = TimeSpan.FromMilliseconds(400);

    // Consistent error message to prevent user enumeration
    private const string ConsistentErrorMessage = "Invalid credentials. Please check your email and password.";

    public UserEnumerationProtectionService(
        ILogger<UserEnumerationProtectionService> logger,
        IConfiguration configuration) {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SimulateAuthenticationDelayAsync(string email, bool userExists) {
        var stopwatch = Stopwatch.StartNew();

        try {
            // Calculate a consistent delay based on email hash
            var targetDelay = CalculateConsistentDelay(email);

            // If user doesn't exist, we need to simulate the full authentication process
            if (!userExists) {
                // Simulate database lookup time
                await Task.Delay(Random.Shared.Next(50, 150));

                // Perform dummy password hashing to simulate verification
                await PerformDummyPasswordHashAsync("dummy_password_for_timing");

                // Simulate additional processing
                await Task.Delay(Random.Shared.Next(30, 100));
            }

            stopwatch.Stop();

            // Calculate remaining delay needed to reach target
            var elapsed = stopwatch.Elapsed;
            var remainingDelay = targetDelay - elapsed;

            if (remainingDelay > TimeSpan.Zero) {
                await Task.Delay(remainingDelay);
            }

            var totalTime = stopwatch.Elapsed + (remainingDelay > TimeSpan.Zero ? remainingDelay : TimeSpan.Zero);

            // Log timing analysis for security monitoring
            if (_logger.IsEnabled(LogLevel.Debug)) {
                _logger.LogDebug(
                    "Authentication timing: Email={EmailHash}, UserExists={UserExists}, ProcessingTime={ProcessingTimeMs}ms, TargetTime={TargetTimeMs}ms",
                    HashEmail(email), userExists, totalTime.TotalMilliseconds, targetDelay.TotalMilliseconds);
            }

        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error in authentication delay simulation");
        }
    }

    public string GetConsistentErrorMessage() {
        return ConsistentErrorMessage;
    }

    public async Task PerformDummyPasswordHashAsync(string password) {
        // Perform actual BCrypt hashing to maintain realistic timing
        // Use a fixed salt to ensure consistent timing
        var dummySalt = "$2a$12$abcdefghijklmnopqrstuu"; // Fixed salt for dummy operations

        await Task.Run(() => {
            try {
                // Simulate BCrypt.HashPassword operation
                BCrypt.Net.BCrypt.HashPassword(password, dummySalt);
            }
            catch {
                // Ignore errors in dummy operation, just maintain timing
                Thread.Sleep(Random.Shared.Next(100, 300));
            }
        });
    }

    public TimeSpan GetBaseProcessingTime() {
        return TargetProcessingTime;
    }

    /// <summary>
    /// Calculates a consistent delay based on email hash to prevent timing analysis
    /// </summary>
    private TimeSpan CalculateConsistentDelay(string email) {
        // Use deterministic hash to ensure same email always gets same delay
        var emailHash = HashEmail(email);
        var hashBytes = Convert.FromHexString(emailHash);

        // Use first 4 bytes of hash to determine delay within acceptable range
        var delayMs = BitConverter.ToUInt32(hashBytes, 0) %
                     (uint)(MaxProcessingTime.TotalMilliseconds - MinProcessingTime.TotalMilliseconds) +
                     (uint)MinProcessingTime.TotalMilliseconds;

        return TimeSpan.FromMilliseconds(delayMs);
    }

    /// <summary>
    /// Creates a hash of the email for logging without exposing the actual email
    /// </summary>
    private string HashEmail(string email) {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(email.ToLowerInvariant()));
        return Convert.ToHexString(hash)[..16]; // First 16 chars for brevity
    }
}

/// <summary>
/// Configuration options for user enumeration protection
/// </summary>
public class UserEnumerationProtectionOptions {
    public const string SectionName = "Authentication:UserEnumerationProtection";

    /// <summary>
    /// Whether user enumeration protection is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Minimum processing time in milliseconds
    /// </summary>
    public int MinProcessingTimeMs { get; set; } = 200;

    /// <summary>
    /// Maximum processing time in milliseconds
    /// </summary>
    public int MaxProcessingTimeMs { get; set; } = 800;

    /// <summary>
    /// Target processing time in milliseconds
    /// </summary>
    public int TargetProcessingTimeMs { get; set; } = 400;

    /// <summary>
    /// Whether to log timing information for analysis
    /// </summary>
    public bool LogTimingAnalysis { get; set; } = false;

    /// <summary>
    /// Custom error message to use (if not set, uses default)
    /// </summary>
    public string? CustomErrorMessage { get; set; }
}

/// <summary>
/// Result of timing analysis for monitoring
/// </summary>
public class AuthenticationTimingAnalysis {
    public string EmailHash { get; set; } = string.Empty;
    public bool UserExists { get; set; }
    public TimeSpan ActualProcessingTime { get; set; }
    public TimeSpan TargetProcessingTime { get; set; }
    public TimeSpan TimingDeviation { get; set; }
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = string.Empty;
}