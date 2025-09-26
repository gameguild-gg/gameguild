using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// Handler for getting all credentials query using CQRS pattern
/// </summary>
public class GetAllCredentialsQueryHandler(ICredentialService credentialService, ILogger<GetAllCredentialsQueryHandler> logger) : IRequestHandler<GetAllCredentialsQuery, IEnumerable<Credential>>
{
    private readonly ICredentialService _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));

    private readonly ILogger<GetAllCredentialsQueryHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<IEnumerable<Credential>> Handle(GetAllCredentialsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all credentials");

        try
        {
            var credentials = await _credentialService.GetAllCredentialsAsync();

            _logger.LogInformation("Retrieved {Count} credentials", credentials.Count());

            return credentials;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve credentials");

            throw;
        }
    }
}
