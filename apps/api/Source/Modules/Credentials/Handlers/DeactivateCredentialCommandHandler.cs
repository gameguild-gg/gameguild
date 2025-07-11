using GameGuild.Modules.Credentials.Commands;
using MediatR;


namespace GameGuild.Modules.Credentials.Handlers;

/// <summary>
/// Handler for DeactivateCredentialCommand using CQRS pattern
/// </summary>
public class DeactivateCredentialCommandHandler : IRequestHandler<DeactivateCredentialCommand, bool>
{
    private readonly ICredentialService _credentialService;

    public DeactivateCredentialCommandHandler(ICredentialService credentialService)
    {
        _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
    }

    public async Task<bool> Handle(DeactivateCredentialCommand request, CancellationToken cancellationToken)
    {
        return await _credentialService.DeactivateCredentialAsync(request.Id);
    }
}
