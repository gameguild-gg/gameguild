using System.Globalization;
using GameGuild.CQRS;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
/// OAuth authentication: GitHub OAuth, Google OAuth, Google ID token
/// </summary>
public class OAuthAuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenService jwtTokenService,
    IOAuthService oauthService,
    IGoogleIdTokenVerifier googleIdTokenVerifier,
    IExternalLoginRepository externalLoginRepository,
    IConfiguration configuration,
    IAuthAttemptService authAttemptService,
    IHttpContextAccessor httpContextAccessor,
    ISender sender,
    ILogger<OAuthAuthService> logger
) : IOAuthAuthService
{
    public async Task<SignInResponse> GitHubSignInAsync(OAuthSignInRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing GitHub OAuth sign-in");

        var githubUser = await oauthService.GetUserProfileAsync("github", request.AccessToken).ConfigureAwait(false);

        var userId = Guid.NewGuid();
        var email = githubUser.Email ?? throw new UnauthorizedAccessException("Email not available from GitHub profile");
        var roles = new[] { "User" };

        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = authAttemptService.GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "OAuth Device", DeviceType = "Web" };

        var jwtToken = jwtTokenService.GenerateAccessToken(userId, email, roles);
        var refreshTokenValue = await jwtTokenService.GenerateRefreshTokenAsync(userId, deviceInfo, cancellationToken).ConfigureAwait(false);
        var refreshExpiresInDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7", CultureInfo.InvariantCulture);
        var refreshTokenExpiresAt = SystemClock.UtcNow.AddDays(refreshExpiresInDays);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = refreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt,
            IsRevoked = false,
            CreatedByIp = ipAddress
        };
        await refreshTokenRepository.CreateAsync(refreshToken).ConfigureAwait(false);

        logger.LogInformation("GitHub OAuth sign-in successful for {Email}", email);

        var accessTokenExpirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60", CultureInfo.InvariantCulture);

        return new SignInResponse
        {
            Success = true,
            Message = "GitHub sign-in successful",
            AccessToken = jwtToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt,
            ExpiresIn = accessTokenExpirationMinutes * 60,
            AccessTokenExpiresAt = SystemClock.UtcNow.AddMinutes(accessTokenExpirationMinutes),
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            UserId = userId,
            Email = email,
            SessionId = refreshToken.Id
        };
    }

    public async Task<SignInResponse> GoogleSignInAsync(OAuthSignInRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Google OAuth sign-in");

        var googleUser = await oauthService.GetUserProfileAsync("google", request.AccessToken).ConfigureAwait(false);

        var userId = Guid.NewGuid();
        var email = googleUser.Email ?? throw new UnauthorizedAccessException("Email not available from Google profile");
        var roles = new[] { "User" };

        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = authAttemptService.GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "OAuth Device", DeviceType = "Web" };

        var jwtToken = jwtTokenService.GenerateAccessToken(userId, email, roles);
        var refreshTokenValue = await jwtTokenService.GenerateRefreshTokenAsync(userId, deviceInfo, cancellationToken).ConfigureAwait(false);
        var refreshExpiresInDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7", CultureInfo.InvariantCulture);
        var refreshTokenExpiresAt = SystemClock.UtcNow.AddDays(refreshExpiresInDays);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = refreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt,
            IsRevoked = false,
            CreatedByIp = ipAddress
        };
        await refreshTokenRepository.CreateAsync(refreshToken).ConfigureAwait(false);

        logger.LogInformation("Google OAuth sign-in successful for {Email}", email);

        var accessTokenExpirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60", CultureInfo.InvariantCulture);

        return new SignInResponse
        {
            Success = true,
            Message = "Google sign-in successful",
            AccessToken = jwtToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt,
            ExpiresIn = accessTokenExpirationMinutes * 60,
            AccessTokenExpiresAt = SystemClock.UtcNow.AddMinutes(accessTokenExpirationMinutes),
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            UserId = userId,
            Email = email,
            SessionId = refreshToken.Id
        };
    }

    public async Task<SignInResponse> GoogleIdTokenSignInAsync(GoogleIdTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.IdToken)) { throw new UnauthorizedAccessException("ID token is required"); }

        // Cryptographically verify the Google ID token (signature, iss, aud, exp).
        // Verifier throws UnauthorizedAccessException on any failure → caller surfaces 401.
        var googleUser = await googleIdTokenVerifier.VerifyAsync(request.IdToken, cancellationToken).ConfigureAwait(false);

        var email = googleUser.Email;
        var providerKey = googleUser.Sub;

        var user = await ResolveGoogleUserAsync(email, providerKey, googleUser, cancellationToken).ConfigureAwait(false);
        var userId = user.Id;

        var tenantAccessContext = await ResolveTenantAccessContextAsync(userId, request.TenantId, cancellationToken).ConfigureAwait(false);

        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = authAttemptService.GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();
        var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "OAuth Device", DeviceType = "Web" };

        // GenerateAccessTokenAsync: async overload that emits tenant_id + token_version claims.
        // GenerateRefreshTokenAsync: persists exactly ONE hashed refresh row (no second plaintext insert).
        var accessToken = await jwtTokenService.GenerateAccessTokenAsync(
            userId,
            user.Email,
            tenantAccessContext.Roles.ToArray(),
            tenantAccessContext.TenantId,
            user.TokenVersion,
            cancellationToken).ConfigureAwait(false);
        var refreshToken = await jwtTokenService.GenerateRefreshTokenAsync(userId, deviceInfo, cancellationToken).ConfigureAwait(false);

        var refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7", CultureInfo.InvariantCulture);
        var refreshTokenExpiresAt = SystemClock.UtcNow.AddDays(refreshTokenExpiryDays);

        logger.LogInformation("Google ID token sign-in successful for {Email}", email);

        var accessTokenExpirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60", CultureInfo.InvariantCulture);

        return new SignInResponse
        {
            Success = true,
            Message = "Google ID token sign-in successful",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = refreshTokenExpiresAt,
            ExpiresIn = accessTokenExpirationMinutes * 60,
            AccessTokenExpiresAt = SystemClock.UtcNow.AddMinutes(accessTokenExpirationMinutes),
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            UserId = userId,
            Email = user.Email,
            SessionId = Guid.NewGuid(),
            TenantId = tenantAccessContext.TenantId,
            AvailableTenants = tenantAccessContext.AvailableTenants
        };
    }

    /// <summary>
    ///     Resolves the GameGuild <see cref="User" /> for a verified Google identity using the
    ///     auto-link policy: existing ExternalLogin wins; else verified-email match links to
    ///     the existing user; else a brand-new OAuth user is created. Concurrent sign-ins for
    ///     the same identity race the unique (Provider, ProviderKey) index — on collision the
    ///     losing insert is caught and the winning rows are refetched (idempotent resume).
    /// </summary>
    private async Task<User> ResolveGoogleUserAsync(string email, string providerKey, VerifiedGoogleUser googleUser, CancellationToken cancellationToken)
    {
        const string provider = "google";

        var existingLink = await externalLoginRepository
            .GetByProviderKeyAsync(provider, providerKey, cancellationToken)
            .ConfigureAwait(false);

        if (existingLink != null)
        {
            return await userRepository.GetByIdAsync(existingLink.UserId, cancellationToken).ConfigureAwait(false)
                ?? throw new UnauthorizedAccessException("Linked user not found");
        }

        var existingByEmail = await userRepository.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);

        if (existingByEmail != null && !googleUser.EmailVerified)
        {
            // Refuse to merge an unverified-email collision — would let a Google-unverified
            // identity hijack a pre-existing account.
            throw new UnauthorizedAccessException("Email is not verified by Google");
        }

        var user = existingByEmail;
        var createdNewUser = false;
        if (user == null)
        {
            user = User.CreateOAuthUser(email, googleUser.Name ?? email.Split('@')[0]);
            createdNewUser = true;
        }

        try
        {
            if (createdNewUser)
            {
                await userRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);
                await userRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            await externalLoginRepository.UpsertAsync(
                new ExternalLogin { UserId = user.Id, Provider = provider, ProviderKey = providerKey },
                cancellationToken).ConfigureAwait(false);

            return user;
        }
        catch (DbUpdateException)
        {
            // Race lost — unique index on (Provider, ProviderKey) or email rejected our insert.
            // Re-fetch the winning rows and resume; the index is the last-line defense.
            existingLink = await externalLoginRepository
                .GetByProviderKeyAsync(provider, providerKey, cancellationToken)
                .ConfigureAwait(false);

            if (existingLink != null)
            {
                return await userRepository.GetByIdAsync(existingLink.UserId, cancellationToken).ConfigureAwait(false)
                    ?? throw new UnauthorizedAccessException("Linked user not found after race");
            }

            // ExternalLogin row was rolled back but a User collision (email uniqueness) won.
            user = await userRepository.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false)
                ?? throw new UnauthorizedAccessException("User not found after race");

            await externalLoginRepository.UpsertAsync(
                new ExternalLogin { UserId = user.Id, Provider = provider, ProviderKey = providerKey },
                cancellationToken).ConfigureAwait(false);

            return user;
        }
    }

    // Verbatim duplicate of LocalAuthService.ResolveTenantAccessContextAsync (pure — uses
    // ISender + GetUserMembershipsQuery). Not extracted to a shared service to keep the
    // blast radius of this fix inside OAuthAuthService.
    private async Task<TenantAccessContext> ResolveTenantAccessContextAsync(Guid userId, Guid? requestedTenantId, CancellationToken cancellationToken)
    {
        var memberships = await sender.Send(new global::GameGuild.Identity.Tenants.GetUserMembershipsQuery(userId), cancellationToken).ConfigureAwait(false);

        if (memberships.TotalCount == 0)
        {
            return new TenantAccessContext(null, null, ["User"]);
        }

        var activeMemberships = memberships.Memberships
            .Where(membership => membership.IsActive)
            .ToList();

        if (activeMemberships.Count == 0)
        {
            return new TenantAccessContext(null, null, ["User"]);
        }

        var availableTenants = activeMemberships
            .GroupBy(membership => membership.TenantId)
            .Select(group => group.First())
            .Select(membership => new global::GameGuild.TenantInfo(
                membership.TenantId,
                membership.TenantName,
                membership.TenantSlug,
                membership.TenantIsActive))
            .ToList();

        var selectedTenantId = requestedTenantId.HasValue
            ? availableTenants.FirstOrDefault(tenant => tenant.Id == requestedTenantId.Value && tenant.IsActive)?.Id
            : null;

        selectedTenantId ??= availableTenants.FirstOrDefault(tenant => tenant.IsActive)?.Id;
        selectedTenantId ??= availableTenants[0].Id;

        var roles = activeMemberships
            .Where(membership => membership.TenantId == selectedTenantId)
            .Select(membership => membership.Role)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Append("User")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new TenantAccessContext(selectedTenantId, availableTenants, roles);
    }

    private sealed record TenantAccessContext(
        Guid? TenantId,
        IReadOnlyList<global::GameGuild.TenantInfo>? AvailableTenants,
        IReadOnlyList<string> Roles);

    public Task<string> GetGitHubAuthUrlAsync(string redirectUri)
    {
        var clientId = configuration["OAuth:GitHub:ClientId"];
        var scopes = "user:email";
        var state = Guid.NewGuid().ToString();

        var url = $"https://github.com/login/oauth/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={scopes}&state={state}";

        return Task.FromResult(url);
    }

    public Task<string> GetGoogleAuthUrlAsync(string redirectUri)
    {
        var clientId = configuration["OAuth:Google:ClientId"];
        var scopes = "openid email profile";
        var state = Guid.NewGuid().ToString();

        var url = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Uri.EscapeDataString(scopes)}&response_type=code&state={state}";

        return Task.FromResult(url);
    }
}
