using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace GameGuild.Modules.Authentication;

/// <summary>
/// Service to protect against user enumeration attacks by ensuring consistent timing and responses
/// </summary>
public class UserEnumerationProtectionService(ILogger<UserEnumerationProtectionService> logger, IConfiguration configuration) : IUserEnumerationProtectionService
{
    private readonly IConfiguration _configuration = configuration;

    // Timing constants
    private static readonly TimeSpan MinProcessingTime = TimeSpan.FromMilliseconds(200);

    private static readonly TimeSpan MaxProcessingTime = TimeSpan.FromMilliseconds(800);

    private static readonly TimeSpan TargetProcessingTime = TimeSpan.FromMilliseconds(400);

    // Consistent error message to prevent user enumeration
    private const string ConsistentErrorMessage = "Invalid credentials. Please check your email and password.";

    public async Task SimulateAuthenticationDelayAsync(string email, bool userExists)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Calculate a consistent delay based on email hash
            var targetDelay = CalculateConsistentDelay(email);

            // If user doesn't exist, we need to simulate the full authentication process
            if (!userExists)
            {
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

            if (remainingDelay > TimeSpan.Zero) { await Task.Delay(remainingDelay); }

            var totalTime = stopwatch.Elapsed + (remainingDelay > TimeSpan.Zero ? remainingDelay : TimeSpan.Zero);

            // Log timing analysis for security monitoring
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "Authentication timing: Email={EmailHash}, UserExists={UserExists}, ProcessingTime={ProcessingTimeMs}ms, TargetTime={TargetTimeMs}ms",
                    HashEmail(email),
                    userExists,
                    totalTime.TotalMilliseconds,
                    targetDelay.TotalMilliseconds
                );
            }
        }
        catch (Exception ex) { logger.LogError(ex, "Error in authentication delay simulation"); }
    }

    public string GetConsistentErrorMessage() { return ConsistentErrorMessage; }

    public async Task PerformDummyPasswordHashAsync(string password)
    {
        // Perform actual BCrypt hashing to maintain realistic timing
        // Use a fixed salt to ensure consistent timing
        var dummySalt = "$2a$12$abcdefghijklmnopqrstuu"; // Fixed salt for dummy operations

        await Task.Run(() =>
            {
                try
                {
                    // Simulate BCrypt.HashPassword operation
                    BCrypt.Net.BCrypt.HashPassword(password, dummySalt);
                }
                catch
                {
                    // Ignore errors in dummy operation, just maintain timing
                    Thread.Sleep(Random.Shared.Next(100, 300));
                }
            }
        );
    }

    public TimeSpan GetBaseProcessingTime() { return TargetProcessingTime; }

    /// <summary>
    /// Calculates a consistent delay based on email hash to prevent timing analysis
    /// </summary>
    private TimeSpan CalculateConsistentDelay(string email)
    {
        // Use deterministic hash to ensure same email always gets same delay
        var emailHash = HashEmail(email);
        var hashBytes = Convert.FromHexString(emailHash);

        // Use first 4 bytes of hash to determine delay within acceptable range
        var delayMs = BitConverter.ToUInt32(hashBytes, 0) % (uint) (MaxProcessingTime.TotalMilliseconds - MinProcessingTime.TotalMilliseconds) + (uint) MinProcessingTime.TotalMilliseconds;

        return TimeSpan.FromMilliseconds(delayMs);
    }

    /// <summary>
    /// Creates a hash of the email for logging without exposing the actual email
    /// </summary>
    private string HashEmail(string email)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(email.ToLowerInvariant()));

        return Convert.ToHexString(hash)[..16]; // First 16 chars for brevity
    }
}
