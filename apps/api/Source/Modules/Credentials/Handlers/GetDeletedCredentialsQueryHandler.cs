using MediatR;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// Handler for GetDeletedCredentialsQuery using CQRS pattern
/// </summary>
public class GetDeletedCredentialsQueryHandler : IRequestHandler<GetDeletedCredentialsQuery, IEnumerable<Credential>>
{
    private readonly ICredentialService _credentialService;

    public GetDeletedCredentialsQueryHandler(ICredentialService credentialService)
    {
        _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
    }

    public async Task<IEnumerable<Credential>> Handle(GetDeletedCredentialsQuery request, CancellationToken cancellationToken)
    {
        return await _credentialService.GetDeletedCredentialsAsync();
    }
}
