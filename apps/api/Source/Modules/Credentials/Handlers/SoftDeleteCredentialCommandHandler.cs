using MediatR;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// Handler for SoftDeleteCredentialCommand using CQRS pattern
/// </summary>
public class SoftDeleteCredentialCommandHandler : IRequestHandler<SoftDeleteCredentialCommand, bool>
{
    private readonly ICredentialService _credentialService;

    public SoftDeleteCredentialCommandHandler(ICredentialService credentialService)
    {
        _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
    }

    public async Task<bool> Handle(SoftDeleteCredentialCommand request, CancellationToken cancellationToken)
    {
        return await _credentialService.SoftDeleteCredentialAsync(request.Id);
    }
}
