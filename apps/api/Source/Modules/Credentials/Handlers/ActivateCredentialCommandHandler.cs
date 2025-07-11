using GameGuild.Modules.Credentials.Commands;
using MediatR;


namespace GameGuild.Modules.Credentials.Handlers;

/// <summary>
/// Handler for ActivateCredentialCommand using CQRS pattern
/// </summary>
public class ActivateCredentialCommandHandler : IRequestHandler<ActivateCredentialCommand, bool>
{
    private readonly ICredentialService _credentialService;

    public ActivateCredentialCommandHandler(ICredentialService credentialService)
    {
        _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
    }

    public async Task<bool> Handle(ActivateCredentialCommand request, CancellationToken cancellationToken)
    {
        return await _credentialService.ActivateCredentialAsync(request.Id);
    }
}
