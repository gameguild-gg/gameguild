using MediatR;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// Handler for MarkCredentialAsUsedCommand using CQRS pattern
/// </summary>
public class MarkCredentialAsUsedCommandHandler : IRequestHandler<MarkCredentialAsUsedCommand, bool>
{
    private readonly ICredentialService _credentialService;

    public MarkCredentialAsUsedCommandHandler(ICredentialService credentialService)
    {
        _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
    }

    public async Task<bool> Handle(MarkCredentialAsUsedCommand request, CancellationToken cancellationToken)
    {
        return await _credentialService.MarkCredentialAsUsedAsync(request.Id);
    }
}
