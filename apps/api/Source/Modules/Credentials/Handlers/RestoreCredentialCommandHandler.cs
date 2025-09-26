using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Handler for RestoreCredentialCommand using CQRS pattern </summary>
public class RestoreCredentialCommandHandler(ICredentialService credentialService, IMediator mediator, ILogger<RestoreCredentialCommandHandler> logger) : IRequestHandler<RestoreCredentialCommand, bool>
{
    private readonly ICredentialService _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    private readonly ILogger<RestoreCredentialCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<bool> Handle(RestoreCredentialCommand request, CancellationToken cancellationToken)
    {
        bool restored = await _credentialService.RestoreCredentialAsync(request.Id);

        if (!restored)
        {
            _logger.LogWarning("Credential {CredentialId} restore failed or credential is not soft deleted", request.Id);
            return false;
        }

        _logger.LogInformation("Credential {CredentialId} restored", request.Id);

        await _mediator.Publish(new CredentialRestoredEvent(request.Id), cancellationToken);

        return true;
    }
}
