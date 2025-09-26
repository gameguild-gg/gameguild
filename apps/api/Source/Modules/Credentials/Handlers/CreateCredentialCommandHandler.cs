using GameGuild.CQRS;

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
            // Verify that the user exists
            var getUserQuery = new GetUserByIdQuery { UserId = request.UserId };
            var user = await _mediator.Send(getUserQuery, cancellationToken);

            if (user == null) { throw new ArgumentException($"User with ID {request.UserId} not found"); }

            var credential = new Credential
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Type = request.Type,
                Value = request.Value,
                Metadata = request.Metadata,
                ExpiresAt = request.ExpiresAt,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var createdCredential = await _credentialService.CreateCredentialAsync(credential);

            _logger.LogInformation("Created credential {CredentialId} for user {UserId}", createdCredential.Id, request.UserId);

            // Publish notification for domain events
            var notification = new CredentialCreatedNotification { CredentialId = createdCredential.Id, UserId = createdCredential.UserId, Type = createdCredential.Type };

            await _mediator.Publish(notification, cancellationToken);

            return createdCredential;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create credential for user {UserId}", request.UserId);

            throw;
        }
    }
}
