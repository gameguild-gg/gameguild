using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// REST API controller for authentication operations using CQRS pattern.
/// Provides clean separation between API layer and business logic with comprehensive error handling.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Authentication")]
public class AuthController : ControllerBase {
  private readonly IMediator _mediator;
  private readonly ILogger<AuthController> _logger;

  public AuthController(IMediator mediator, ILogger<AuthController> logger) {
    _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <summary>
  /// Register a new user with email and password.
  /// </summary>
  /// <param name="request">User registration details</param>
  /// <returns>Authentication response with tokens</returns>
  [HttpPost("signup")]
  [ProducesResponseType(typeof(SignInResponseDto), StatusCodes.Status201Created)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
  public async Task<ActionResult<SignInResponseDto>> LocalSignUp([FromBody] LocalSignUpRequestDto request) {
    try {
      _logger.LogInformation("Local sign-up attempt for email: {Email}", request.Email);
      _logger.LogInformation("DEBUG: Received username: '{Username}', email: '{Email}'", request.Username, request.Email);

      var command = new LocalSignUpCommand { Email = request.Email, Password = request.Password, Username = request.Username ?? request.Email, TenantId = request.TenantId };

      _logger.LogInformation("DEBUG: Command username: '{Username}', email: '{Email}'", command.Username, command.Email);

      var result = await _mediator.Send(command);

      _logger.LogInformation("Local sign-up successful for email: {Email}", request.Email);

      return CreatedAtAction(nameof(LocalSignUp), result);
    }
    catch (ValidationException ex) {
      _logger.LogWarning("Validation failed for sign-up: {ValidationErrors}", string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)));

      return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)), Status = StatusCodes.Status400BadRequest });
    }
    catch (InvalidOperationException ex) {
      _logger.LogWarning("Sign-up operation failed: {Message}", ex.Message);

      return Conflict(new ProblemDetails { Title = "Operation Error", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Unexpected error during sign-up for email: {Email}", request.Email);

      return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Internal Server Error", Detail = "An unexpected error occurred", Status = StatusCodes.Status500InternalServerError });
    }
  }

  /// <summary>
  /// Authenticate a user with email and password.
  /// </summary>
  /// <param name="request">User login credentials</param>
  /// <returns>Authentication response with tokens</returns>
  [HttpPost("signin")]
  [ProducesResponseType(typeof(SignInResponseDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
  public async Task<ActionResult<SignInResponseDto>> LocalSignIn([FromBody] LocalSignInRequestDto request) {
    try {
      _logger.LogInformation("Local sign-in attempt for email: {Email}", request.Email);

      var command = new LocalSignInCommand { Email = request.Email, Password = request.Password, TenantId = request.TenantId };

      var result = await _mediator.Send(command);

      _logger.LogInformation("Local sign-in successful for email: {Email}", request.Email);

      return Ok(result);
    }
    catch (ValidationException ex) {
      _logger.LogWarning("Validation failed for sign-in: {ValidationErrors}", string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)));

      return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)), Status = StatusCodes.Status400BadRequest });
    }
    catch (UnauthorizedAccessException ex) {
      _logger.LogWarning("Sign-in failed for email: {Email} - {Message}", request.Email, ex.Message);

      return Unauthorized(new ProblemDetails { Title = "Authentication Failed", Detail = "Invalid credentials", Status = StatusCodes.Status401Unauthorized });
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Unexpected error during sign-in for email: {Email}", request.Email);

      return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Internal Server Error", Detail = "An unexpected error occurred", Status = StatusCodes.Status500InternalServerError });
    }
  }

  /// <summary>
  /// Authenticate a user using Google ID Token (for NextAuth.js integration).
  /// </summary>
  /// <param name="request">Google ID Token validation request</param>
  /// <returns>Authentication response with tokens</returns>
  [HttpPost("google/id-token")]
  [ProducesResponseType(typeof(SignInResponseDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
  public async Task<ActionResult<SignInResponseDto>> GoogleIdTokenSignIn([FromBody] GoogleIdTokenRequestDto request) {
    try {
      _logger.LogInformation("Google ID token sign-in attempt");

      var command = new GoogleIdTokenSignInCommand { IdToken = request.IdToken, TenantId = request.TenantId };

      var result = await _mediator.Send(command);

      _logger.LogInformation("Google ID token sign-in successful");

      return Ok(result);
    }
    catch (ValidationException ex) {
      _logger.LogWarning("Validation failed for Google ID token sign-in: {ValidationErrors}", string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)));

      return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)), Status = StatusCodes.Status400BadRequest });
    }
    catch (UnauthorizedAccessException ex) {
      _logger.LogWarning("Google ID token sign-in failed: {Message}", ex.Message);

      return Unauthorized(new ProblemDetails { Title = "Authentication Failed", Detail = "Invalid Google ID token", Status = StatusCodes.Status401Unauthorized });
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Unexpected error during Google ID token sign-in");

      return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Internal Server Error", Detail = "An unexpected error occurred", Status = StatusCodes.Status500InternalServerError });
    }
  }

  /// <summary>
  /// Refresh access token using a valid refresh token.
  /// </summary>
  /// <param name="request">Token refresh details</param>
  /// <returns>New authentication response with refreshed tokens</returns>
  [HttpPost("refresh")]
  [ProducesResponseType(typeof(SignInResponseDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
  public async Task<ActionResult<SignInResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto request) {
    try {
      _logger.LogInformation("🔥 [CONTROLLER] Token refresh attempt started. RefreshToken: {RefreshToken}, TenantId: {TenantId}", 
        request?.RefreshToken?.Substring(0, Math.Min(request?.RefreshToken?.Length ?? 0, 10)) + "...", 
        request?.TenantId);

      if (request?.RefreshToken == null) {
        _logger.LogWarning("🔥 [CONTROLLER] Refresh token is null or empty");
        return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Refresh token is required", Status = StatusCodes.Status400BadRequest });
      }

      var command = new RefreshTokenCommand { RefreshToken = request.RefreshToken, TenantId = request.TenantId };

      var result = await _mediator.Send(command);

      _logger.LogInformation("Token refresh successful");

      return Ok(result);
    }
    catch (ValidationException ex) {
      _logger.LogWarning("Validation failed for token refresh: {ValidationErrors}", string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)));

      return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)), Status = StatusCodes.Status400BadRequest });
    }
    catch (UnauthorizedAccessException ex) {
      _logger.LogWarning("Token refresh failed: {Message}", ex.Message);

      return Unauthorized(new ProblemDetails { Title = "Token Refresh Failed", Detail = "Invalid or expired refresh token", Status = StatusCodes.Status401Unauthorized });
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Unexpected error during token refresh");

      return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Internal Server Error", Detail = "An unexpected error occurred", Status = StatusCodes.Status500InternalServerError });
    }
  }

  /// <summary>
  /// Revoke a refresh token to invalidate it.
  /// </summary>
  /// <param name="request">Token revocation details</param>
  /// <returns>Success response</returns>
  [HttpPost("revoke")]
  [Authorize]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto request) {
    try {
      _logger.LogInformation("Token revocation attempt");

      var command = new RevokeTokenCommand { RefreshToken = request.RefreshToken, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() };

      await _mediator.Send(command);

      _logger.LogInformation("Token revocation successful");

      return NoContent();
    }
    catch (ValidationException ex) {
      _logger.LogWarning("Validation failed for token revocation: {ValidationErrors}", string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)));

      return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)), Status = StatusCodes.Status400BadRequest });
    }
    catch (UnauthorizedAccessException ex) {
      _logger.LogWarning("Token revocation failed: {Message}", ex.Message);

      return Unauthorized(new ProblemDetails { Title = "Token Revocation Failed", Detail = ex.Message, Status = StatusCodes.Status401Unauthorized });
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Unexpected error during token revocation");

      return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Internal Server Error", Detail = "An unexpected error occurred", Status = StatusCodes.Status500InternalServerError });
    }
  }

  /// <summary>
  /// Get current user profile information.
  /// </summary>
  /// <returns>User profile information</returns>
  [HttpGet("profile")]
  [Authorize]
  [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
  public async Task<ActionResult<UserProfileDto>> GetProfile() {
    try {
      _logger.LogInformation("User profile request");

      // Extract user ID from JWT claims
      var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;

      if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) { throw new UnauthorizedAccessException("Invalid user claims"); }

      var query = new GetUserProfileQuery { UserId = userId };
      var result = await _mediator.Send(query);

      _logger.LogInformation("User profile retrieved successfully");

      return Ok(result);
    }
    catch (UnauthorizedAccessException ex) {
      _logger.LogWarning("Unauthorized profile access: {Message}", ex.Message);

      return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = ex.Message, Status = StatusCodes.Status401Unauthorized });
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Unexpected error during profile retrieval");

      return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = "Internal Server Error", Detail = "An unexpected error occurred", Status = StatusCodes.Status500InternalServerError });
    }
  }
}
