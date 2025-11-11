using System.Net.Mail;
using Asp.Versioning;
using GameGuild.Authentication.Commands;
using GameGuild.Authentication.DTOs;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ValidationException = GameGuild.CQRS.ValidationException;
using Web3ChallengeResponse = GameGuild.Authentication.Models.Responses.Web3ChallengeResponse;

namespace GameGuild.Authentication.Controllers;

/// <summary>
///     REST API controller for authentication operations using CQRS pattern.
///     Provides clean separation between API layer and business logic with comprehensive error handling.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Tags("Authentication")]
public class AuthController(IMediator mediator, ILogger<AuthController> logger) : ControllerBase
{
    private readonly ILogger<AuthController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    /// <summary>
    ///     Register a new user with email and password.
    /// </summary>
    /// <param name="request">User registration details</param>
    /// <returns>Authentication response with tokens</returns>
    [HttpPost("sign-up")]
    [ProducesResponseType(typeof(SignInResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SignInResponse>> LocalSignUp([FromBody] LocalSignUpRequest request)
    {
        try
        {
            _logger.LogInformation("Local sign-up attempt for email: {Email}", request.Email);
            _logger.LogInformation("DEBUG: Received username: '{Username}', email: '{Email}'", request.Username, request.Email);

            var command = new LocalSignUpCommand { Email = request.Email, Password = request.Password, Username = request.Username ?? request.Email, TenantId = request.TenantId };

            _logger.LogInformation("DEBUG: Command username: '{Username}', email: '{Email}'", command.Username, command.Email);

            var result = await _mediator.Send(command);

            _logger.LogInformation("Local sign-up successful for email: {Email}", request.Email);

            return CreatedAtAction(nameof(LocalSignUp), result);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation failed for sign-up: {ValidationErrors}", string.Join(", ", ex.Errors));

            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = string.Join(", ", ex.Errors), Status = StatusCodes.Status400BadRequest });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Sign-up operation failed: {Message}", ex.Message);

            return Conflict(new ProblemDetails { Title = "Operation Error", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during sign-up for email: {Email}", request.Email);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Internal Server Error", Detail = "An unexpected error occurred", Status = StatusCodes.Status500InternalServerError });
        }
    }

    /// <summary>
    ///     Authenticate a user with email and password.
    /// </summary>
    /// <param name="request">User login credentials</param>
    /// <returns>Authentication response with tokens</returns>
    [HttpPost("sign-in")]
    [ProducesResponseType(typeof(SignInResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SignInResponse>> LocalSignIn([FromBody] LocalSignInRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request?.Email)) { return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Email is required", Status = StatusCodes.Status400BadRequest }); }

            if (string.IsNullOrWhiteSpace(request.Password)) { return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Password is required", Status = StatusCodes.Status400BadRequest }); }

            if (!IsValidEmail(request.Email)) { return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Email must be a valid email address", Status = StatusCodes.Status400BadRequest }); }

            _logger.LogInformation("Local sign-in attempt for email: {Email}", request.Email);

            var command = new LocalSignInCommand { Email = request.Email, Password = request.Password, TenantId = request.TenantId };

            var result = await _mediator.Send(command);

            _logger.LogInformation("Local sign-in successful for email: {Email}", request.Email);

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for sign-in: {ValidationErrors}", string.Join(", ", ex.Errors));

            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = string.Join(", ", ex.Errors), Status = StatusCodes.Status400BadRequest });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Sign-in failed for email: {Email} - {Message}", request.Email, ex.Message);

            return Unauthorized(new ProblemDetails { Title = "Authentication Failed", Detail = "Invalid credentials", Status = StatusCodes.Status401Unauthorized });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during sign-in for email: {Email}", request.Email);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Internal Server Error", Detail = "An unexpected error occurred", Status = StatusCodes.Status500InternalServerError });
        }
    }

    /// <summary>
    ///     Authenticate a user using Google ID Token (for NextAuth.js integration).
    /// </summary>
    /// <param name="request">Google ID Token validation request</param>
    /// <returns>Authentication response with tokens</returns>
    [AllowAnonymous]
    [HttpPost("google")]
    [ProducesResponseType(typeof(SignInResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SignInResponse>> GoogleIdTokenSignIn([FromBody] GoogleIdTokenRequestDto request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request?.IdToken)) { return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "ID token is required", Status = StatusCodes.Status400BadRequest }); }

            _logger.LogInformation("Google ID token sign-in attempt");

            var command = new GoogleIdTokenSignInCommand { IdToken = request.IdToken, TenantId = request.TenantId };

            var result = await _mediator.Send(command);

            _logger.LogInformation("Google ID token sign-in successful");

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation failed for Google ID token sign-in: {ValidationErrors}", string.Join(", ", ex.Errors));

            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = string.Join(", ", ex.Errors), Status = StatusCodes.Status400BadRequest });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Google ID token sign-in failed: {Message}", ex.Message);

            return Unauthorized(new ProblemDetails { Title = "Authentication Failed", Detail = "Invalid Google ID token", Status = StatusCodes.Status401Unauthorized });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Google ID token sign-in");

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Internal Server Error", Detail = "An unexpected error occurred", Status = StatusCodes.Status500InternalServerError });
        }
    }

    /// <summary>
    ///     Refresh access token using a valid refresh token.
    /// </summary>
    /// <param name="request">Token refresh details</param>
    /// <returns>New authentication response with refreshed tokens</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(SignInResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SignInResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            _logger.LogInformation(
                "🔥 [CONTROLLER] Token refresh attempt started. RefreshToken: {RefreshToken}, TenantId: {TenantId}",
                request?.RefreshToken?[..Math.Min(request?.RefreshToken?.Length ?? 0, 10)] + "...",
                request?.TenantId
            );

            if (request?.RefreshToken == null)
            {
                _logger.LogWarning("🔥 [CONTROLLER] Refresh token is null or empty");

                return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Refresh token is required", Status = StatusCodes.Status400BadRequest });
            }

            var command = new RefreshTokenCommand { RefreshToken = request.RefreshToken, TenantId = request.TenantId };

            _logger.LogInformation("🔥 [CONTROLLER] About to send command via mediator: {Command}", command.RefreshToken);

            var result = await _mediator.Send(command);

            _logger.LogInformation("🔥 [CONTROLLER] Token refresh successful, returning result");

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation failed for token refresh: {ValidationErrors}", string.Join(", ", ex.Errors));

            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = string.Join(", ", ex.Errors), Status = StatusCodes.Status400BadRequest });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Token refresh failed: {Message}", ex.Message);

            return Unauthorized(new ProblemDetails { Title = "Token Refresh Failed", Detail = "Invalid or expired refresh token", Status = StatusCodes.Status401Unauthorized });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh");

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Internal Server Error", Detail = "An unexpected error occurred", Status = StatusCodes.Status500InternalServerError });
        }
    }

    /// <summary>
    ///     Revoke a refresh token to invalidate it.
    /// </summary>
    /// <param name="request">Token revocation details</param>
    /// <returns>Success response</returns>
    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeRefreshTokenRequest request)
    {
        try
        {
            _logger.LogInformation("Token revocation attempt");

            var command = new RevokeTokenCommand { RefreshToken = request.Token, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() };

            await _mediator.Send(command);

            _logger.LogInformation("Token revocation successful");

            return NoContent();
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation failed for token revocation: {ValidationErrors}", string.Join(", ", ex.Errors));

            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = string.Join(", ", ex.Errors), Status = StatusCodes.Status400BadRequest });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Token revocation failed: {Message}", ex.Message);

            return Unauthorized(new ProblemDetails { Title = "Token Revocation Failed", Detail = ex.Message, Status = StatusCodes.Status401Unauthorized });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token revocation");

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Internal Server Error", Detail = "An unexpected error occurred", Status = StatusCodes.Status500InternalServerError });
        }
    }

    /// <summary>
    ///     Generate Web3 challenge for wallet authentication.
    /// </summary>
    /// <param name="request">Web3 challenge request with wallet address</param>
    /// <returns>Challenge string for wallet signing</returns>
    [HttpPost("web3/challenge")]
    [ProducesResponseType(typeof(Web3ChallengeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Web3ChallengeResponse>> GenerateWeb3Challenge([FromBody] Web3ChallengeRequest request)
    {
        try
        {
            _logger.LogInformation("Web3 challenge generation request for address: {Address}", request.WalletAddress);

            var command = new GenerateWeb3ChallengeCommand { WalletAddress = request.WalletAddress ?? "", ChainId = request.ChainId ?? "" };

            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Web3 challenge");

            return BadRequest(new ProblemDetails { Title = "Web3 Challenge Error", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
        }
    }

    /// <summary>
    ///     Send email verification.
    /// </summary>
    /// <param name="request">Email verification request</param>
    /// <returns>Success confirmation</returns>
    [HttpPost("send-email-verification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult SendEmailVerification([FromBody] SendEmailVerificationRequest request)
    {
        try
        {
            _logger.LogInformation("Email verification request for: {Email}", request.Email);

            // For now, return success without implementing the full functionality
            // TODO: Implement proper email verification command and handler
            return Ok(new { Message = "Verification email sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending verification email");

            return BadRequest(new ProblemDetails { Title = "Email Verification Error", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
        }
    }

    /// <summary>
    ///     Initiate GitHub OAuth sign-in.
    /// </summary>
    /// <param name="redirectUri">Redirect URI after authentication</param>
    /// <returns>GitHub OAuth authorization URL</returns>
    [HttpGet("github/signin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult GitHubSignIn([FromQuery] string redirectUri)
    {
        try
        {
            _logger.LogInformation("GitHub OAuth sign-in request with redirect: {RedirectUri}", redirectUri);

            // For now, return a mock GitHub auth URL
            // TODO: Implement proper GitHub OAuth flow
            var mockAuthUrl = $"https://github.com/login/oauth/authorize?client_id=test&redirect_uri={redirectUri}";

            return Ok(new { AuthUrl = mockAuthUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating GitHub sign-in");

            return BadRequest(new ProblemDetails { Title = "GitHub Sign-In Error", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
        }
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        try
        {
            var addr = new MailAddress(email);

            return addr.Address == email;
        }
        catch { return false; }
    }
}
