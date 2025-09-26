using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Handler for SoftDeleteCredentialCommand using CQRS pattern </summary>
public class SoftDeleteCredentialCommandHandler(ICredentialService credentialService) : IRequestHandler<SoftDeleteCredentialCommand, bool>
{
    private readonly ICredentialService _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));

    public async Task<bool> Handle(SoftDeleteCredentialCommand request, CancellationToken cancellationToken) { return await _credentialService.SoftDeleteCredentialAsync(request.Id); }
}
