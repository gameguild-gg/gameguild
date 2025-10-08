namespace GameGuild.Modules.Authentication;

/// <summary>
/// Service for generating and validating username slugs
/// </summary>
public interface IUsernameSlugificationService
{
    /// <summary>
    /// Generates a URL-safe slug from a username
    /// </summary>
    /// <param name="username">The original username</param>
    /// <returns>A slugified version of the username</returns>
    string Slugify(string username);

    /// <summary>
    /// Validates if a slug meets the requirements
    /// </summary>
    /// <param name="slug">The slug to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValidSlug(string slug);

    /// <summary>
    /// Generates a unique slug by appending numbers if needed
    /// </summary>
    /// <param name="username">The original username</param>
    /// <param name="checkAvailability">Function to check if slug is available</param>
    /// <returns>A unique slug</returns>
    Task<string> GenerateUniqueSlugAsync(string username, Func<string, Task<bool>> checkAvailability);

    /// <summary>
    /// Normalizes a username for comparison
    /// </summary>
    /// <param name="username">The username to normalize</param>
    /// <returns>Normalized username</returns>
    string Normalize(string username);
}
