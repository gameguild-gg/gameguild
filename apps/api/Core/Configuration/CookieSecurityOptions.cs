namespace GameGuild.Core.Configuration;

/// <summary>
/// Configuration options for cookie security
/// </summary>
public class CookieSecurityOptions
{
    /// <summary>
    /// Require HTTPS for all cookies
    /// </summary>
    public bool RequireHttps { get; set; } = true;

    /// <summary>
    /// Default SameSite mode for cookies
    /// </summary>
    public SameSiteMode SameSiteMode { get; set; } = SameSiteMode.Strict;

    /// <summary>
    /// Set HttpOnly as default for new cookies
    /// </summary>
    public bool HttpOnlyDefault { get; set; } = true;

    /// <summary>
    /// Check if user consent is needed for non-essential cookies
    /// </summary>
    public bool CheckConsentNeeded { get; set; } = false;

    /// <summary>
    /// Authentication cookie name
    /// </summary>
    public string AuthCookieName { get; set; } = "GameGuild.Auth";

    /// <summary>
    /// Authentication cookie expiration in minutes
    /// </summary>
    public int AuthCookieExpirationMinutes { get; set; } = 120; // 2 hours

    /// <summary>
    /// Enable sliding expiration for authentication cookies
    /// </summary>
    public bool EnableSlidingExpiration { get; set; } = true;

    /// <summary>
    /// Enable session cookies
    /// </summary>
    public bool EnableSessionCookies { get; set; } = false;

    /// <summary>
    /// Session cookie name
    /// </summary>
    public string SessionCookieName { get; set; } = "GameGuild.Session";

    /// <summary>
    /// Session timeout in minutes
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Anti-forgery token cookie name
    /// </summary>
    public string AntiForgeryTokenName { get; set; } = "GameGuild.CSRF";

    /// <summary>
    /// Default expiration for non-session cookies in days
    /// </summary>
    public int DefaultCookieExpirationDays { get; set; } = 30;

    /// <summary>
    /// Validates the configuration options
    /// </summary>
    public void Validate()
    {
        if (AuthCookieExpirationMinutes <= 0) { throw new ArgumentException("AuthCookieExpirationMinutes must be positive", nameof(AuthCookieExpirationMinutes)); }

        if (SessionTimeoutMinutes <= 0) { throw new ArgumentException("SessionTimeoutMinutes must be positive", nameof(SessionTimeoutMinutes)); }

        if (DefaultCookieExpirationDays < 0) { throw new ArgumentException("DefaultCookieExpirationDays must be non-negative", nameof(DefaultCookieExpirationDays)); }

        if (string.IsNullOrWhiteSpace(AuthCookieName)) { throw new ArgumentException("AuthCookieName cannot be null or empty", nameof(AuthCookieName)); }

        if (string.IsNullOrWhiteSpace(SessionCookieName)) { throw new ArgumentException("SessionCookieName cannot be null or empty", nameof(SessionCookieName)); }

        if (string.IsNullOrWhiteSpace(AntiForgeryTokenName)) { throw new ArgumentException("AntiForgeryTokenName cannot be null or empty", nameof(AntiForgeryTokenName)); }
    }
}
