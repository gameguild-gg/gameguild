using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Authenticates local email/username and password credentials using HTTP Basic.
///     The scheme is opt-in and rejects every non-HTTPS request.
/// </summary>
public sealed class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationSchemeOptions>
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserMfaConfigurationRepository _mfaConfigurationRepository;

    public BasicAuthenticationHandler(
        IOptionsMonitor<BasicAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUserMfaConfigurationRepository mfaConfigurationRepository)
        : base(options, logger, encoder)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _mfaConfigurationRepository = mfaConfigurationRepository;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorizationHeaders = Request.Headers.Authorization;
        if (authorizationHeaders.Count == 0)
        {
            return AuthenticateResult.NoResult();
        }

        if (authorizationHeaders.Count != 1)
        {
            return AuthenticateResult.Fail("Invalid authorization header.");
        }

        var authorization = authorizationHeaders[0];
        if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Basic", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        if (authorization.Length <= 6 || authorization[5] != ' ')
        {
            return AuthenticateResult.Fail("Invalid Basic authorization header.");
        }

        if (!Request.IsHttps)
        {
            return AuthenticateResult.Fail("Basic authentication requires HTTPS.");
        }

        var encodedCredentials = authorization[6..];
        if (encodedCredentials.Length == 0 || encodedCredentials.Any(char.IsWhiteSpace))
        {
            return AuthenticateResult.Fail("Invalid Basic credentials.");
        }

        string decodedCredentials;
        try
        {
            decodedCredentials = StrictUtf8.GetString(Convert.FromBase64String(encodedCredentials));
        }
        catch (FormatException)
        {
            return AuthenticateResult.Fail("Invalid Basic credentials.");
        }
        catch (DecoderFallbackException)
        {
            return AuthenticateResult.Fail("Invalid Basic credentials.");
        }

        var separatorIndex = decodedCredentials.IndexOf(':');
        if (separatorIndex <= 0)
        {
            return AuthenticateResult.Fail("Invalid Basic credentials.");
        }

        var username = decodedCredentials[..separatorIndex];
        var password = decodedCredentials[(separatorIndex + 1)..];

        try
        {
            var cancellationToken = Context.RequestAborted;
            var user = await _userRepository.GetByEmailAsync(username.ToLowerInvariant(), cancellationToken).ConfigureAwait(false);
            user ??= await _userRepository.GetByUsernameAsync(username, cancellationToken).ConfigureAwait(false);

            if (user is null || !user.IsActive || user.IsSuspended || !user.HasPassword ||
                !_passwordHasher.VerifyPassword(user.PasswordHash!, password))
            {
                return AuthenticateResult.Fail("Invalid username or password.");
            }

            // Basic auth has no challenge flow to complete a second factor. Never let it bypass MFA.
            if (await _mfaConfigurationRepository.IsMfaEnabledAsync(user.Id, cancellationToken).ConfigureAwait(false))
            {
                return AuthenticateResult.Fail("Invalid username or password.");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new("sub", user.Id.ToString()),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Email, user.Email),
                new("auth_method", "basic")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (OperationCanceledException) when (Context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "An unexpected error occurred while processing Basic authentication.");
            return AuthenticateResult.Fail("Basic authentication is unavailable.");
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.Remove("WWW-Authenticate");

        if (Request.IsHttps)
        {
            Response.Headers.WWWAuthenticate = $"Basic realm=\"{Options.Realm}\", charset=\"UTF-8\"";
        }

        return Task.CompletedTask;
    }
}

/// <summary>
///     Options for the named HTTP Basic authentication scheme.
/// </summary>
public sealed class BasicAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public string Realm { get; set; } = "GameGuild API";

    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(Realm) ||
            Realm.Any(character => char.IsControl(character) || character is '"' or '\\'))
        {
            throw new InvalidOperationException("Basic authentication realm must be non-empty and must not contain control characters, quotes, or backslashes.");
        }
    }
}

/// <summary>
///     Registers the opt-in HTTP Basic authentication scheme.
/// </summary>
public static class BasicAuthenticationExtensions
{
    public static AuthenticationBuilder AddBasicAuthentication(
        this AuthenticationBuilder builder,
        string schemeName) => AddBasicAuthentication(builder, schemeName, static _ => { });

    public static AuthenticationBuilder AddBasicAuthentication(
        this AuthenticationBuilder builder,
        string schemeName,
        Action<BasicAuthenticationSchemeOptions> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemeName);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.AddScheme<BasicAuthenticationSchemeOptions, BasicAuthenticationHandler>(schemeName, options =>
        {
            configure(options);
            options.Validate();
        });
    }
}
