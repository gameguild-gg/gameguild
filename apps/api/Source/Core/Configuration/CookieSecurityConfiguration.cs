using Microsoft.AspNetCore.CookiePolicy;

namespace GameGuild.Core.Configuration;

/// <summary>
/// Configuration for secure cookie settings
/// </summary>
public static class CookieSecurityConfiguration {
    /// <summary>
    /// Adds secure cookie configuration to the service collection
    /// </summary>
    public static IServiceCollection AddCookieSecurity(this IServiceCollection services, IConfiguration configuration, CookieSecurityOptions? options = null) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        options ??= new CookieSecurityOptions();
        options.Validate();

        // Configure cookie policy options
        services.Configure<CookiePolicyOptions>(cookieOptions => {
            // Require HTTPS for cookies in production
            cookieOptions.Secure = options.RequireHttps ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;

            // Set SameSite policy
            cookieOptions.MinimumSameSitePolicy = options.SameSiteMode;

            // HttpOnly by default for security
            cookieOptions.HttpOnly = options.HttpOnlyDefault ? HttpOnlyPolicy.Always : HttpOnlyPolicy.None;

            // Custom cookie consent check if needed
            if (options.CheckConsentNeeded) {
                cookieOptions.CheckConsentNeeded = context => true;
            }

            // Configure secure cookie creation
            cookieOptions.OnAppendCookie = cookieContext => {
                SecureCookie(cookieContext.CookieOptions, options);
            };

            cookieOptions.OnDeleteCookie = cookieContext => {
                SecureCookie(cookieContext.CookieOptions, options);
            };
        });

        // Configure authentication cookies specifically
        services.ConfigureApplicationCookie(authOptions => {
            // Cookie configuration
            authOptions.Cookie.Name = options.AuthCookieName;
            authOptions.Cookie.HttpOnly = true;
            authOptions.Cookie.SecurePolicy = options.RequireHttps ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
            authOptions.Cookie.SameSite = options.SameSiteMode;
            authOptions.Cookie.IsEssential = true; // GDPR compliance - essential cookies

            // Expiration settings
            authOptions.ExpireTimeSpan = TimeSpan.FromMinutes(options.AuthCookieExpirationMinutes);
            authOptions.SlidingExpiration = options.EnableSlidingExpiration;

            // Security settings
            authOptions.LoginPath = "/auth/login";
            authOptions.LogoutPath = "/auth/logout";
            authOptions.AccessDeniedPath = "/auth/access-denied";

            // Configure for API scenarios
            authOptions.Events.OnRedirectToLogin = context => {
                if (context.Request.Path.StartsWithSegments("/api") ||
                    context.Request.Headers.Accept.Any(h => h.Contains("application/json"))) {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                }
                return Task.CompletedTask;
            };

            authOptions.Events.OnRedirectToAccessDenied = context => {
                if (context.Request.Path.StartsWithSegments("/api") ||
                    context.Request.Headers.Accept.Any(h => h.Contains("application/json"))) {
                    context.Response.StatusCode = 403;
                    return Task.CompletedTask;
                }
                return Task.CompletedTask;
            };
        });

        // Configure session cookies if sessions are enabled
        if (options.EnableSessionCookies) {
            services.AddSession(sessionOptions => {
                sessionOptions.Cookie.Name = options.SessionCookieName;
                sessionOptions.Cookie.HttpOnly = true;
                sessionOptions.Cookie.SecurePolicy = options.RequireHttps ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
                sessionOptions.Cookie.SameSite = options.SameSiteMode;
                sessionOptions.Cookie.IsEssential = true;
                sessionOptions.IdleTimeout = TimeSpan.FromMinutes(options.SessionTimeoutMinutes);
            });
        }

        // Configure anti-forgery token cookies
        services.AddAntiforgery(antiOptions => {
            antiOptions.Cookie.Name = options.AntiForgeryTokenName;
            antiOptions.Cookie.HttpOnly = true;
            antiOptions.Cookie.SecurePolicy = options.RequireHttps ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
            antiOptions.Cookie.SameSite = options.SameSiteMode;
            antiOptions.HeaderName = "X-CSRF-TOKEN";
        });

        return services;
    }

    /// <summary>
    /// Configures the cookie policy middleware in the application pipeline
    /// </summary>
    public static IApplicationBuilder UseCookieSecurity(this IApplicationBuilder app) {
        ArgumentNullException.ThrowIfNull(app);

        // Add cookie policy middleware early in pipeline
        app.UseCookiePolicy();

        return app;
    }

    /// <summary>
    /// Helper method to apply security settings to a cookie
    /// </summary>
    private static void SecureCookie(CookieOptions cookieOptions, CookieSecurityOptions options, string? cookieName = null) {
        // Ensure HttpOnly for security-sensitive cookies
        if (IsSecuritySensitiveCookie(cookieName)) {
            cookieOptions.HttpOnly = true;
        }

        // Apply secure policy
        if (options.RequireHttps) {
            cookieOptions.Secure = true;
        }

        // Apply SameSite policy
        cookieOptions.SameSite = options.SameSiteMode;

        // Set expiration if not already set and not session cookie
        if (!cookieOptions.Expires.HasValue && options.DefaultCookieExpirationDays > 0) {
            cookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(options.DefaultCookieExpirationDays);
        }
    }

    /// <summary>
    /// Determines if a cookie is security-sensitive and should have additional protections
    /// </summary>
    private static bool IsSecuritySensitiveCookie(string? cookieName) {
        if (string.IsNullOrEmpty(cookieName)) return false;

        var sensitiveNames = new[] {
            "auth",
            "session",
            "token",
            "csrf",
            "xsrf",
            "identity",
            "__requestverificationtoken"
        };

        return sensitiveNames.Any(name => cookieName.ToLowerInvariant().Contains(name));
    }
}

/// <summary>
/// Configuration options for cookie security
/// </summary>
public class CookieSecurityOptions {
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
    public void Validate() {
        if (AuthCookieExpirationMinutes <= 0) {
            throw new ArgumentException("AuthCookieExpirationMinutes must be positive", nameof(AuthCookieExpirationMinutes));
        }

        if (SessionTimeoutMinutes <= 0) {
            throw new ArgumentException("SessionTimeoutMinutes must be positive", nameof(SessionTimeoutMinutes));
        }

        if (DefaultCookieExpirationDays < 0) {
            throw new ArgumentException("DefaultCookieExpirationDays must be non-negative", nameof(DefaultCookieExpirationDays));
        }

        if (string.IsNullOrWhiteSpace(AuthCookieName)) {
            throw new ArgumentException("AuthCookieName cannot be null or empty", nameof(AuthCookieName));
        }

        if (string.IsNullOrWhiteSpace(SessionCookieName)) {
            throw new ArgumentException("SessionCookieName cannot be null or empty", nameof(SessionCookieName));
        }

        if (string.IsNullOrWhiteSpace(AntiForgeryTokenName)) {
            throw new ArgumentException("AntiForgeryTokenName cannot be null or empty", nameof(AntiForgeryTokenName));
        }
    }
}
