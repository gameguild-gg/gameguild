namespace GameGuild;

public class CorsOptions
{
    public string[] AllowedOrigins { get; set; } = [];

    public string[] AllowedMethods { get; set; } = [];

    public string[] AllowedHeaders { get; set; } = [];

    public void Validate()
    {
        // Validate CORS configuration
        if (AllowedOrigins.Contains("*") && AllowedOrigins.Length > 1)
        {
            throw new InvalidOperationException("When using wildcard '*' for AllowedOrigins, it must be the only origin specified.");
        }

        // Check for potential security issues
        if (AllowedOrigins.Contains("*") &&
            (AllowedMethods.Contains("*") || AllowedHeaders.Contains("*")))
        {
            // This is a very permissive CORS configuration - consider if this is intentional
        }
    }
}
