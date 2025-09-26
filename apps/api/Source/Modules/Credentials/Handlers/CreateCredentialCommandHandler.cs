using GameGuild.CQRS;
using GameGuild.Modules.Users;

namespace GameGuild.Modules.Credentials;

/// <summary> Handler for creating credential command using CQRS pattern </summary>
public class CreateCredentialCommandHandler(ICredentialService credentialService, ILogger<CreateCredentialCommandHandler> logger, IMediator mediator) : IRequestHandler<CreateCredentialCommand, Credential>
{
    private readonly ICredentialService _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));

    private readonly ILogger<CreateCredentialCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    public async Task<Credential> Handle(CreateCredentialCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating credential for user {UserId} with type {Type}", request.UserId, request.Type);

        try
        {
            GetUserByIdQuery getUserQuery = new() { UserId = request.UserId };
            User user = await _mediator.Send(getUserQuery, cancellationToken) ?? throw new ArgumentException($"User with ID {request.UserId} not found");

            Credential credential = new()
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Type = request.Type,
                Value = request.Value,
                Metadata = request.Metadata,
                ExpiresAt = request.ExpiresAt,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            Credential createdCredential = await _credentialService.CreateCredentialAsync(credential);

            _logger.LogInformation("Created credential {CredentialId} for user {UserId}", createdCredential.Id, request.UserId);

            await _mediator.Publish(new CredentialCreatedEvent(createdCredential.Id, createdCredential.UserId, createdCredential.Type, createdCredential.CreatedAt), cancellationToken);

            return createdCredential;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create credential for user {UserId}", request.UserId);

            throw;
        }
    }
}
