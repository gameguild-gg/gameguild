using GameGuild.Common;
using HotChocolate.Authorization;
using MediatR;
using ValidationException = FluentValidation.ValidationException;
using AuthorizeAttribute = HotChocolate.Authorization.AuthorizeAttribute;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// GraphQL mutations for Authentication module using CQRS pattern.
/// Provides clean separation between GraphQL layer and business logic with comprehensive error handling.
/// </summary>
[ExtendObjectType<Mutation>]
public class AuthMutations {
  /// <summary>
  /// Local sign-up mutation using CQRS pattern.
  /// </summary>
  /// <param name="input">Sign-up request data</param>
  /// <param name="mediator">MediatR mediator for CQRS</param>
  /// <returns>Authentication response with tokens</returns>
  [GraphQLDescription("Registers a new user with email and password")]
  [Error<ValidationException>]
  [Error<InvalidOperationException>]
  public async Task<SignInResponseDto> LocalSignUp(
    [GraphQLDescription("User registration details")]
    LocalSignUpRequestDto input,
    [Service] IMediator mediator
  ) {
    var command = new LocalSignUpCommand {
      Email = input.Email,
      Password = input.Password,
      Username = input.Username ?? input.Email, // Fallback to email if username not provided
      TenantId = input.TenantId,
    };

    return await mediator.Send(command);
  }

  /// <summary>
  /// Local sign-in mutation using CQRS pattern.
  /// </summary>
  /// <param name="input">Sign-in request data</param>
  /// <param name="mediator">MediatR mediator for CQRS</param>
  /// <returns>Authentication response with tokens</returns>
  [GraphQLDescription("Authenticates a user with email and password")]
  [Error<UnauthorizedAccessException>]
  [Error<ValidationException>]
  public async Task<SignInResponseDto> LocalSignIn(
    [GraphQLDescription("User login credentials")]
    LocalSignInRequestDto input,
    [Service] IMediator mediator
  ) {
    var command = new LocalSignInCommand { Email = input.Email, Password = input.Password, TenantId = input.TenantId };

    return await mediator.Send(command);
  }

  /// <summary>
  /// Revoke refresh token using CQRS pattern.
  /// </summary>
  /// <param name="refreshToken">The refresh token to revoke</param>
  /// <param name="mediator">MediatR mediator for CQRS</param>
  /// <param name="contextAccessor">HTTP context for IP address</param>
  /// <returns>Success confirmation</returns>
  [GraphQLDescription("Revokes a refresh token to prevent future use")]
  [Authorize] // Requires authentication
  [Error<UnauthorizedAccessException>]
  [Error<ValidationException>]
  public async Task<bool> RevokeToken(
    [GraphQLDescription("The refresh token to revoke")]
    string refreshToken,
    [Service] IMediator mediator,
    [Service] IHttpContextAccessor contextAccessor
  ) {
    var ipAddress = contextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "GraphQL";

    var command = new RevokeTokenCommand { RefreshToken = refreshToken, IpAddress = ipAddress };

    await mediator.Send(command);

    return true;
  }

  /// <summary>
  /// Refresh access token using CQRS pattern.
  /// </summary>
  /// <param name="refreshToken">The refresh token to use</param>
  /// <param name="tenantId">Optional tenant ID</param>
  /// <param name="mediator">MediatR mediator for CQRS</param>
  /// <returns>New authentication response with refreshed tokens</returns>
  [GraphQLDescription("Refreshes access token using a valid refresh token")]
  [Error<UnauthorizedAccessException>]
  [Error<ValidationException>]
  public async Task<SignInResponseDto> RefreshToken(
    [GraphQLDescription("Refresh token to use")]
    string refreshToken,
    [GraphQLDescription("Optional tenant ID")]
    Guid? tenantId,
    [Service] IMediator mediator
  ) {
    var command = new RefreshTokenCommand { RefreshToken = refreshToken, TenantId = tenantId };

    return await mediator.Send(command);
  }

  /// <summary>
  /// Generate Web3 challenge using CQRS pattern.
  /// </summary>
  /// <param name="input">Web3 challenge request</param>
  /// <param name="mediator">MediatR mediator for CQRS</param>
  /// <returns>Challenge data for wallet signing</returns>
  [GraphQLDescription("Generates a challenge for Web3 wallet authentication")]
  [Error<ValidationException>]
  public async Task<Web3ChallengeResponseDto> GenerateWeb3Challenge(
    [GraphQLDescription("Web3 challenge request data")]
    Web3ChallengeRequestDto input,
    [Service] IMediator mediator
  ) {
    var command = new GenerateWeb3ChallengeCommand {
      WalletAddress = input.WalletAddress, ChainId = input.ChainId ?? "1", // Default to Ethereum mainnet
    };

    return await mediator.Send(command);
  }

  /// <summary>
  /// Verify Web3 signature using CQRS pattern.
  /// </summary>
  /// <param name="input">Web3 signature verification request</param>
  /// <param name="mediator">MediatR mediator for CQRS</param>
  /// <returns>Authentication response with tokens</returns>
  [GraphQLDescription("Verifies a Web3 wallet signature and authenticates the user")]
  [Error<UnauthorizedAccessException>]
  [Error<ValidationException>]
  public async Task<SignInResponseDto> VerifyWeb3Signature(
    [GraphQLDescription("Web3 signature verification data")]
    Web3VerifyRequestDto input,
    [Service] IMediator mediator
  ) {
    var command = new VerifyWeb3SignatureCommand {
      WalletAddress = input.WalletAddress, Signature = input.Signature, Nonce = input.Nonce, ChainId = input.ChainId ?? "1", // Default to Ethereum mainnet
    };

    return await mediator.Send(command);
  }
}
