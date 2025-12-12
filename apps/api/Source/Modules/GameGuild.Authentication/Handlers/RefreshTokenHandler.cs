using GameGuild.Authentication.Abstractions;
using GameGuild.Authentication.Commands;
using GameGuild.Authentication.DTOs;
using GameGuild.Authentication.Mappings;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using DomainRequests = GameGuild.Authentication.Models.Requests;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace GameGuild.Authentication.Handlers;

/// <summary>
///     Handler for refresh token command
/// </summary>
public class RefreshTokenHandler(IAuthService authService, IAuthUserRepository authUserRepository, ILogger<RefreshTokenHandler> logger, FluentValidation.IValidator<RefreshTokenCommand> validator)
    : IRequestHandler<RefreshTokenCommand, SignInResponse>
{
    private readonly IAuthService _authService = authService ?? throw new ArgumentNullException(nameof(authService));

    private readonly IAuthUserRepository _authUserRepository = authUserRepository ?? throw new ArgumentNullException(nameof(authUserRepository));

    private readonly ILogger<RefreshTokenHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<SignInResponse> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        // Validate command
        var validationResult = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => new CQRS.ValidationError(e.PropertyName, e.ErrorMessage));

            throw new ValidationException(errors);
        }

        _logger.LogInformation("Processing refresh token request with token: {RefreshToken}", command.RefreshToken);

        var refreshRequest = new DomainRequests.RefreshTokenRequest { RefreshToken = command.RefreshToken };

        try
        {
            var domainResponse = await _authService.RefreshTokenAsync(refreshRequest, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Refresh token processed successfully");

            // Map from Domain response to Application DTO
            return await domainResponse.ToDto(_authUserRepository, cancellationToken).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("RefreshTokenHandler caught UnauthorizedAccessException: {Message}", ex.Message);

            throw; // Re-throw to propagate to controller
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RefreshTokenHandler");

            throw;
        }
    }
}
