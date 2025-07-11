using GameGuild.Modules.Credentials.Commands;
using MediatR;


namespace GameGuild.Modules.Credentials.Handlers;

/// <summary>
/// Handler for UpdateCredentialCommand using CQRS pattern
/// </summary>
public class UpdateCredentialCommandHandler : IRequestHandler<UpdateCredentialCommand, Credential>
{
    private readonly ICredentialService _credentialService;
    private readonly ILogger<UpdateCredentialCommandHandler> _logger;

    public UpdateCredentialCommandHandler(ICredentialService credentialService, ILogger<UpdateCredentialCommandHandler> logger)
    {
        _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Credential> Handle(UpdateCredentialCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating credential {CredentialId}", request.Id);

        try
        {
            var existingCredential = await _credentialService.GetCredentialByIdAsync(request.Id);
            if (existingCredential == null)
            {
                throw new ArgumentException($"Credential with ID {request.Id} not found");
            }

            // Update the credential properties
            existingCredential.Type = request.Type;
            existingCredential.Value = request.Value;
            existingCredential.Metadata = request.Metadata;
            existingCredential.ExpiresAt = request.ExpiresAt;
            existingCredential.IsActive = request.IsActive;
            existingCredential.UpdatedAt = DateTime.UtcNow;

            var updatedCredential = await _credentialService.UpdateCredentialAsync(existingCredential);

            _logger.LogInformation("Updated credential {CredentialId}", updatedCredential.Id);

            return updatedCredential;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update credential {CredentialId}", request.Id);
            throw;
        }
    }
}
