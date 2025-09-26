using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// Handler for ActivateCredentialCommand using CQRS pattern
/// </summary>
public class ActivateCredentialCommandHandler(ICredentialService credentialService) : IRequestHandler<ActivateCredentialCommand, bool>
{
    private readonly ICredentialService _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));

    public async Task<bool> Handle(ActivateCredentialCommand request, CancellationToken cancellationToken) { return await _credentialService.ActivateCredentialAsync(request.Id); }
}
