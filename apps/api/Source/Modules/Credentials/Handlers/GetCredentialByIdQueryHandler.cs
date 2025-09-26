using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Handler for getting credential by ID query using CQRS pattern </summary>
public class GetCredentialByIdQueryHandler(ICredentialService credentialService, ILogger<GetCredentialByIdQueryHandler> logger) : IRequestHandler<GetCredentialByIdQuery, Credential?>
{
    private readonly ICredentialService _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));

    private readonly ILogger<GetCredentialByIdQueryHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Credential?> Handle(GetCredentialByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving credential {CredentialId}", request.Id);

        try
        {
            var credential = await _credentialService.GetCredentialByIdAsync(request.Id);

            if (credential != null) { _logger.LogInformation("Found credential {CredentialId}", request.Id); }
            else { _logger.LogWarning("Credential {CredentialId} not found", request.Id); }

            return credential;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve credential {CredentialId}", request.Id);

            throw;
        }
    }
}
