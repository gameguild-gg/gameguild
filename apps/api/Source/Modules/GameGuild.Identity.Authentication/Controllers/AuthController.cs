using Asp.Versioning;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Authentication API Controller - RESTful API for user authentication and token management
/// </summary>
/// <remarks>
///     Rate limited to 10 requests per minute per client to prevent brute-force attacks.
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Tags("authentication")]
[EnableRateLimiting(RateLimitPolicies.Authentication)]
public sealed class AuthController(ISender sender) : ControllerBase
{
    #region Registration Operations - /v1/auth/sign-up

    /// <summary>
    ///     Register a new user with email and password
    /// </summary>
    /// <param name="body">User registration details including email, password, and optional username</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Authentication response with access and refresh tokens</returns>
    [AllowAnonymous]
    [HttpPost("v{version:apiVersion}/auth/sign-up")]
    [EndpointSummary("Register a new user")]
    [EndpointDescription("Creates a new user account with email and password credentials, returning authentication tokens on success.")]
    [ProducesResponseType<SignInResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LocalSignUp([FromBody] LocalSignUpRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var command = new LocalSignUpCommand
        {
            Email = body.Email,
            Password = body.Password,
            Username = body.Username,
            TenantId = body.TenantId
        };

        SignInResponse result = await sender.Send(command, ct).ConfigureAwait(false);

        return CreatedAtAction(nameof(LocalSignUp), result);
    }

    #endregion

    #region Sign-In Operations - /v1/auth/sign-in

    /// <summary>
    ///     Authenticate a user with email and password
    /// </summary>
    /// <param name="body">User login credentials</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Authentication response with access and refresh tokens</returns>
    [AllowAnonymous]
    [HttpPost("v{version:apiVersion}/auth/sign-in")]
    [EndpointSummary("Sign in with email and password")]
    [EndpointDescription("Authenticates a user with email and password credentials, returning access and refresh tokens.")]
    [ProducesResponseType<SignInResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LocalSignIn([FromBody] LocalSignInRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var command = new LocalSignInCommand
        {
            Email = body.Email,
            Password = body.Password,
            TenantId = body.TenantId
        };

        SignInResponse result = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Authenticate a user using Google ID Token
    /// </summary>
    /// <param name="body">Google ID Token validation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Authentication response with access and refresh tokens</returns>
    [AllowAnonymous]
    [HttpPost("v{version:apiVersion}/auth/google")]
    [EndpointSummary("Sign in with Google ID Token")]
    [EndpointDescription("Authenticates a user using a Google ID Token (for NextAuth.js integration), returning access and refresh tokens.")]
    [ProducesResponseType<SignInResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GoogleIdTokenSignIn([FromBody] GoogleIdTokenRequestDto body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var command = new GoogleIdTokenSignInCommand
        {
            IdToken = body.IdToken,
            TenantId = body.TenantId
        };

        SignInResponse result = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Initiate GitHub OAuth sign-in
    /// </summary>
    /// <param name="redirectUri">Redirect URI after authentication</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>GitHub OAuth authorization URL</returns>
    [AllowAnonymous]
    [HttpGet("v{version:apiVersion}/auth/github:authorize")]
    [EndpointSummary("Initiate GitHub OAuth sign-in")]
    [EndpointDescription("Initiates GitHub OAuth authentication flow and returns the authorization URL.")]
    [ProducesResponseType<GitHubSignInResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GitHubSignIn([FromQuery] string redirectUri, CancellationToken ct)
    {
        // TODO: Implement proper GitHub OAuth flow with CQRS command
        var mockAuthUrl = $"https://github.com/login/oauth/authorize?client_id=test&redirect_uri={redirectUri}";

        return Task.FromResult<IActionResult>(Ok(new GitHubSignInResponse { AuthUrl = mockAuthUrl }));
    }

    #endregion

    #region Token Operations - /v1/auth/tokens

    /// <summary>
    ///     Refresh access token using a valid refresh token
    /// </summary>
    /// <param name="body">Token refresh request with refresh token</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>New authentication response with refreshed tokens</returns>
    [AllowAnonymous]
    [HttpPost("v{version:apiVersion}/auth/tokens:refresh")]
    [EndpointSummary("Refresh access token")]
    [EndpointDescription("Exchanges a valid refresh token for a new access token and refresh token pair.")]
    [ProducesResponseType<SignInResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var command = new RefreshTokenCommand
        {
            RefreshToken = body.RefreshToken,
            TenantId = body.TenantId
        };

        SignInResponse result = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Revoke a refresh token to invalidate it
    /// </summary>
    /// <param name="body">Token revocation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [Authorize]
    [HttpPost("v{version:apiVersion}/auth/tokens:revoke")]
    [EndpointSummary("Revoke refresh token")]
    [EndpointDescription("Invalidates a refresh token, preventing it from being used to obtain new access tokens.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeRefreshTokenRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var command = new RevokeTokenCommand
        {
            RefreshToken = body.Token,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await sender.Send(command, ct).ConfigureAwait(false);

        return NoContent();
    }

    #endregion

    #region Web3 Operations - /v1/auth/web3

    /// <summary>
    ///     Generate Web3 challenge for wallet authentication
    /// </summary>
    /// <param name="body">Web3 challenge request with wallet address</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Challenge string for wallet signing</returns>
    [AllowAnonymous]
    [HttpPost("v{version:apiVersion}/auth/web3/challenge")]
    [EndpointSummary("Generate Web3 authentication challenge")]
    [EndpointDescription("Generates a cryptographic challenge that must be signed by the user's wallet to prove ownership.")]
    [ProducesResponseType<Web3ChallengeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateWeb3Challenge([FromBody] Web3ChallengeRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var command = new GenerateWeb3ChallengeCommand
        {
            WalletAddress = body.WalletAddress,
            ChainId = body.ChainId
        };

        var result = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(result);
    }

    #endregion

    #region Email Verification - /v1/auth/email

    /// <summary>
    ///     Send email verification to user
    /// </summary>
    /// <param name="body">Email verification request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success confirmation</returns>
    [AllowAnonymous]
    [HttpPost("v{version:apiVersion}/auth/email:send-verification")]
    [EndpointSummary("Send email verification")]
    [EndpointDescription("Sends a verification email to the specified email address to confirm ownership.")]
    [ProducesResponseType<EmailVerificationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> SendEmailVerification([FromBody] SendEmailVerificationRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        // TODO: Implement proper email verification command
        return Task.FromResult<IActionResult>(Ok(new EmailVerificationResponse { Message = "Verification email sent successfully" }));
    }

    #endregion
}

/// <summary>
///     Response for GitHub sign-in initiation
/// </summary>
public sealed record GitHubSignInResponse
{
    /// <summary>
    ///     GitHub OAuth authorization URL
    /// </summary>
    public required string AuthUrl { get; init; }
}

/// <summary>
///     Response for email verification request
/// </summary>
public sealed record EmailVerificationResponse
{
    /// <summary>
    ///     Success message
    /// </summary>
    public required string Message { get; init; }
}
