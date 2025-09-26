using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Handler for HardDeleteCredentialCommand using CQRS pattern </summary>
public class HardDeleteCredentialCommandHandler(ICredentialService credentialService, IMediator mediator, ILogger<HardDeleteCredentialCommandHandler> logger) : IRequestHandler<HardDeleteCredentialCommand, bool>
{
    private readonly ICredentialService _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    private readonly ILogger<HardDeleteCredentialCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<bool> Handle(HardDeleteCredentialCommand request, CancellationToken cancellationToken)
    {
        bool deleted = await _credentialService.HardDeleteCredentialAsync(request.Id);

        if (!deleted)
        {
            _logger.LogWarning("Credential {CredentialId} hard delete failed", request.Id);
            return false;
        }

        _logger.LogInformation("Credential {CredentialId} hard deleted", request.Id);

        await _mediator.Publish(new CredentialDeletedEvent(request.Id, false), cancellationToken);

        return true;
    }
}
