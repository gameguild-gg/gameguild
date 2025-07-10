using MediatR;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// Handler for RestoreCredentialCommand using CQRS pattern
/// </summary>
public class RestoreCredentialCommandHandler : IRequestHandler<RestoreCredentialCommand, bool>
{
    private readonly ICredentialService _credentialService;

    public RestoreCredentialCommandHandler(ICredentialService credentialService)
    {
        _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
    }

    public async Task<bool> Handle(RestoreCredentialCommand request, CancellationToken cancellationToken)
    {
        return await _credentialService.RestoreCredentialAsync(request.Id);
    }
}
