using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Handler for ActivateCredentialCommand using CQRS pattern </summary>
public class ActivateCredentialCommandHandler(ICredentialService credentialService, IMediator mediator, ILogger<ActivateCredentialCommandHandler> logger) : IRequestHandler<ActivateCredentialCommand, bool>
{
    private readonly ICredentialService _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    private readonly ILogger<ActivateCredentialCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<bool> Handle(ActivateCredentialCommand request, CancellationToken cancellationToken)
    {
        bool activated = await _credentialService.ActivateCredentialAsync(request.Id);

        if (!activated)
        {
            _logger.LogWarning("Credential {CredentialId} activation failed or credential already active", request.Id);
            return false;
        }

        _logger.LogInformation("Credential {CredentialId} activated", request.Id);

        await _mediator.Publish(new CredentialActivatedEvent(request.Id), cancellationToken);

        return true;
    }
}
