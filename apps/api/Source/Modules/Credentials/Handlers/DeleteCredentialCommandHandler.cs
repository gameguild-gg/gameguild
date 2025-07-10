using MediatR;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// Handler for DeleteCredentialCommand using CQRS pattern
/// </summary>
public class DeleteCredentialCommandHandler : IRequestHandler<DeleteCredentialCommand, bool>
{
    private readonly ICredentialService _credentialService;
    private readonly ILogger<DeleteCredentialCommandHandler> _logger;

    public DeleteCredentialCommandHandler(ICredentialService credentialService, ILogger<DeleteCredentialCommandHandler> logger)
    {
        _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> Handle(DeleteCredentialCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting credential {CredentialId}", request.Id);

        try
        {
            var result = await _credentialService.SoftDeleteCredentialAsync(request.Id);
            
            if (result)
            {
                _logger.LogInformation("Successfully deleted credential {CredentialId}", request.Id);
            }
            else
            {
                _logger.LogWarning("Credential {CredentialId} not found for deletion", request.Id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete credential {CredentialId}", request.Id);
            throw;
        }
    }
}
