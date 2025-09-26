using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// Handler for getting credentials by user ID query using CQRS pattern
/// </summary>
public class GetCredentialsByUserIdQueryHandler(ICredentialService credentialService, ILogger<GetCredentialsByUserIdQueryHandler> logger) : IRequestHandler<GetCredentialsByUserIdQuery, IEnumerable<Credential>>
{
    private readonly ICredentialService _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));

    private readonly ILogger<GetCredentialsByUserIdQueryHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<IEnumerable<Credential>> Handle(GetCredentialsByUserIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving credentials for user {UserId}", request.UserId);

        try
        {
            var credentials = await _credentialService.GetCredentialsByUserIdAsync(request.UserId);

            _logger.LogInformation("Retrieved {Count} credentials for user {UserId}", credentials.Count(), request.UserId);

            return credentials;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve credentials for user {UserId}", request.UserId);

            throw;
        }
    }
}
