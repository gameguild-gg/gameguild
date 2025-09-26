using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Handler for SoftDeleteCredentialCommand using CQRS pattern </summary>
public class SoftDeleteCredentialCommandHandler(ICredentialService credentialService, IMediator mediator, ILogger<SoftDeleteCredentialCommandHandler> logger) : IRequestHandler<SoftDeleteCredentialCommand, bool>
{
    private readonly ICredentialService _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    private readonly ILogger<SoftDeleteCredentialCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<bool> Handle(SoftDeleteCredentialCommand request, CancellationToken cancellationToken)
    {
        bool softDeleted = await _credentialService.SoftDeleteCredentialAsync(request.Id);

        if (!softDeleted)
        {
            _logger.LogWarning("Credential {CredentialId} soft delete failed or credential already soft deleted", request.Id);

            return false;
        }

        _logger.LogInformation("Credential {CredentialId} soft deleted", request.Id);

        await _mediator.Publish(new CredentialDeletedEvent(request.Id, true), cancellationToken);

        return true;
    }
}
