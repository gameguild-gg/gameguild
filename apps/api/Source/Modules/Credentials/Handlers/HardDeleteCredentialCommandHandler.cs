using GameGuild.Modules.Credentials.Commands;


namespace GameGuild.Modules.Credentials.Handlers;

/// <summary>
/// Handler for HardDeleteCredentialCommand using CQRS pattern
/// </summary>
public class HardDeleteCredentialCommandHandler : IRequestHandler<HardDeleteCredentialCommand, bool> {
  private readonly ICredentialService _credentialService;

  public HardDeleteCredentialCommandHandler(ICredentialService credentialService) { _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService)); }

  public async Task<bool> Handle(HardDeleteCredentialCommand request, CancellationToken cancellationToken) { return await _credentialService.HardDeleteCredentialAsync(request.Id); }
}
