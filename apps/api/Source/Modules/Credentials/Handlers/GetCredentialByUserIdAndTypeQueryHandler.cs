using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Handler for GetCredentialByUserIdAndTypeQuery using CQRS pattern </summary>
public class GetCredentialByUserIdAndTypeQueryHandler(ICredentialService credentialService) : IRequestHandler<GetCredentialByUserIdAndTypeQuery, Credential?>
{
    private readonly ICredentialService _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));

    public async Task<Credential?> Handle(GetCredentialByUserIdAndTypeQuery request, CancellationToken cancellationToken)
    {
        return await _credentialService.GetCredentialByUserIdAndTypeAsync(request.UserId, request.Type);
    }
}
