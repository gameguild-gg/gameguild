using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Handler for DeactivateCredentialCommand using CQRS pattern </summary>
public class DeactivateCredentialCommandHandler(ICredentialService credentialService, IMediator mediator, ILogger<DeactivateCredentialCommandHandler> logger) : IRequestHandler<DeactivateCredentialCommand, bool>
{
    private readonly ICredentialService _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    private readonly ILogger<DeactivateCredentialCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<bool> Handle(DeactivateCredentialCommand request, CancellationToken cancellationToken)
    {
        bool deactivated = await _credentialService.DeactivateCredentialAsync(request.Id);

        if (!deactivated)
        {
            _logger.LogWarning("Credential {CredentialId} deactivation failed or credential already inactive", request.Id);

            return false;
        }

        _logger.LogInformation("Credential {CredentialId} deactivated", request.Id);

        await _mediator.Publish(new CredentialDeactivatedEvent(request.Id), cancellationToken);

        return true;
    }
}
