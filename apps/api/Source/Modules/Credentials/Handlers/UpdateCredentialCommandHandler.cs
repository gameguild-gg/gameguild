using System.Collections.Generic;
using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Handler for UpdateCredentialCommand using CQRS pattern </summary>
public class UpdateCredentialCommandHandler(ICredentialService credentialService, ILogger<UpdateCredentialCommandHandler> logger, IMediator mediator) : IRequestHandler<UpdateCredentialCommand, Credential>
{
    private readonly ICredentialService _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));

    private readonly ILogger<UpdateCredentialCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    public async Task<Credential> Handle(UpdateCredentialCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating credential {CredentialId}", request.Id);

        try
        {
            Credential existingCredential = await _credentialService.GetCredentialByIdAsync(request.Id) ?? throw new ArgumentException($"Credential with ID {request.Id} not found");

            Dictionary<string, object> changes = [];

            if (!string.Equals(existingCredential.Type, request.Type, StringComparison.Ordinal))
            {
                changes["Type"] = new { From = existingCredential.Type, To = request.Type };
                existingCredential.Type = request.Type;
            }

            if (!string.Equals(existingCredential.Value, request.Value, StringComparison.Ordinal))
            {
                changes["Value"] = new { From = existingCredential.Value, To = request.Value };
                existingCredential.Value = request.Value;
            }

            if (!string.Equals(existingCredential.Metadata, request.Metadata, StringComparison.Ordinal))
            {
                changes["Metadata"] = new { From = existingCredential.Metadata, To = request.Metadata };
                existingCredential.Metadata = request.Metadata;
            }

            if (existingCredential.ExpiresAt != request.ExpiresAt)
            {
                changes["ExpiresAt"] = new { From = existingCredential.ExpiresAt, To = request.ExpiresAt };
                existingCredential.ExpiresAt = request.ExpiresAt;
            }

            if (existingCredential.IsActive != request.IsActive)
            {
                changes["IsActive"] = new { From = existingCredential.IsActive, To = request.IsActive };
                existingCredential.IsActive = request.IsActive;
            }

            if (changes.Count == 0)
            {
                _logger.LogInformation("No changes detected for credential {CredentialId}", request.Id);
                return existingCredential;
            }

            existingCredential.UpdatedAt = DateTime.UtcNow;

            Credential updatedCredential = await _credentialService.UpdateCredentialAsync(existingCredential);

            _logger.LogInformation("Updated credential {CredentialId}", updatedCredential.Id);

            await _mediator.Publish(new CredentialUpdatedEvent(updatedCredential.Id, changes), cancellationToken);

            return updatedCredential;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update credential {CredentialId}", request.Id);

            throw;
        }
    }
}
