namespace GameGuild.Modules.Authentication;

/// <summary>
/// Service interface for user enumeration protection
/// </summary>
public interface IUserEnumerationProtectionService
{
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
