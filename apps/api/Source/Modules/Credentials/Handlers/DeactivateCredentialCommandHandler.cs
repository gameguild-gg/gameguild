using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Handler for DeactivateCredentialCommand using CQRS pattern </summary>
public class DeactivateCredentialCommandHandler(ICredentialService credentialService) : IRequestHandler<DeactivateCredentialCommand, bool>
{
    private readonly ICredentialService _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));

    public async Task<bool> Handle(DeactivateCredentialCommand request, CancellationToken cancellationToken) { return await _credentialService.DeactivateCredentialAsync(request.Id); }
}
