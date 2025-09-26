using GameGuild.CQRS;
using GameGuild.Modules.Credentials;

/// <summary> Handler for GetDeletedCredentialsQuery using CQRS pattern </summary>
public class GetDeletedCredentialsQueryHandler(ICredentialService credentialService) : IRequestHandler<GetDeletedCredentialsQuery, IEnumerable<Credential>>
{
    private readonly ICredentialService _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));

    public async Task<IEnumerable<Credential>> Handle(GetDeletedCredentialsQuery request, CancellationToken cancellationToken) { return await _credentialService.GetDeletedCredentialsAsync(); }
}
