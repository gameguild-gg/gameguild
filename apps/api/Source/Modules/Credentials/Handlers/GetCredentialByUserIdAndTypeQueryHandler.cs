using GameGuild.Modules.Credentials.Queries;
using MediatR;


namespace GameGuild.Modules.Credentials.Handlers;

/// <summary>
/// Handler for GetCredentialByUserIdAndTypeQuery using CQRS pattern
/// </summary>
public class GetCredentialByUserIdAndTypeQueryHandler : IRequestHandler<GetCredentialByUserIdAndTypeQuery, Credential?> {
  private readonly ICredentialService _credentialService;

  public GetCredentialByUserIdAndTypeQueryHandler(ICredentialService credentialService) { _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService)); }

  public async Task<Credential?> Handle(GetCredentialByUserIdAndTypeQuery request, CancellationToken cancellationToken) { return await _credentialService.GetCredentialByUserIdAndTypeAsync(request.UserId, request.Type); }
}
