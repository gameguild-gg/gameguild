using GameGuild.CQRS;
using GameGuild.Modules.Users;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Credentials;

/// <summary> REST API controller for managing user credentials using CQRS pattern </summary>
[ApiController]
[Route("[controller]")]
public class CredentialsController(IMediator mediator, ILogger<CredentialsController> logger) : ControllerBase
{
    private readonly ILogger<CredentialsController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    /// <summary> Get all credentials using CQRS pattern </summary>
    /// <returns> List of credentials </returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CredentialResponse>>> GetCredentials()
    {
        try
        {
            _logger.LogInformation("Getting all credentials");

            var query = new GetAllCredentialsQuery();
            var credentials = await _mediator.Send(query);
            var response = credentials.Select(MapToResponseDto);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get credentials");

            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary> Get credentials by user ID using CQRS pattern </summary>
    /// <param name="userId"> User ID </param>
    /// <returns> List of user credentials </returns>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<CredentialResponse>>> GetCredentialsByUserId(Guid userId)
    {
        try
        {
            _logger.LogInformation("Getting credentials for user {UserId}", userId);

            var query = new GetCredentialsByUserIdQuery(userId);
            var credentials = await _mediator.Send(query);
            var response = credentials.Select(MapToResponseDto);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get credentials for user {UserId}", userId);

            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary> Get a specific credential by ID using CQRS pattern </summary>
    /// <param name="id"> Credential ID </param>
    /// <returns> Credential details </returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CredentialResponse>> GetCredential(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting credential {CredentialId}", id);

            var query = new GetCredentialByIdQuery(id);
            var credential = await _mediator.Send(query);

            if (credential == null) return NotFound($"Credential with ID {id} not found");

            return Ok(MapToResponseDto(credential));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get credential {CredentialId}", id);

            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary> Get a credential by user ID and type using CQRS pattern </summary>
    /// <param name="userId"> User ID </param>
    /// <param name="type"> Credential type </param>
    /// <returns> Credential details </returns>
    [HttpGet("user/{userId:guid}/type/{type}")]
    public async Task<ActionResult<CredentialResponse>> GetCredentialByUserIdAndType(Guid userId, string type)
    {
        try
        {
            _logger.LogInformation("Getting credential of type {Type} for user {UserId}", type, userId);

            var query = new GetCredentialByUserIdAndTypeQuery(userId, type);
            var credential = await _mediator.Send(query);

            if (credential == null) return NotFound($"Credential of type '{type}' for user {userId} not found");

            return Ok(MapToResponseDto(credential));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get credential of type {Type} for user {UserId}", type, userId);

            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary> Create a new credential using CQRS pattern </summary>
    /// <param name="createRequest"> Credential data </param>
    /// <returns> Created credential </returns>
    [HttpPost]
    public async Task<ActionResult<CredentialResponse>> CreateCredential([FromBody] CreateCredentialRequest createRequest)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _logger.LogInformation("Creating credential for user {UserId}", createRequest.UserId);

            var command = new CreateCredentialCommand
            {
                UserId = createRequest.UserId, Type = createRequest.Type, Value = createRequest.Value, Metadata = createRequest.Metadata, ExpiresAt = createRequest.ExpiresAt, IsActive = createRequest.IsActive
            };

            var createdCredential = await _mediator.Send(command);
            var response = MapToResponseDto(createdCredential);

            return CreatedAtAction(nameof(GetCredential), new { id = createdCredential.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create credential for user {UserId}", createRequest.UserId);

            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary> Update an existing credential using CQRS pattern </summary>
    /// <param name="id"> Credential ID </param>
    /// <param name="updateRequest"> Updated credential data </param>
    /// <returns> Updated credential </returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CredentialResponse>> UpdateCredential(Guid id, [FromBody] UpdateCredentialRequest updateRequest)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _logger.LogInformation("Updating credential {CredentialId}", id);

            var command = new UpdateCredentialCommand
            {
                Id = id, Type = updateRequest.Type, Value = updateRequest.Value, Metadata = updateRequest.Metadata, ExpiresAt = updateRequest.ExpiresAt, IsActive = updateRequest.IsActive
            };

            var updatedCredential = await _mediator.Send(command);
            var response = MapToResponseDto(updatedCredential);

            return Ok(response);
        }
        catch (ArgumentException ex) { return NotFound(ex.Message); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update credential {CredentialId}", id);

            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary> Soft delete a credential using CQRS pattern </summary>
    /// <param name="id"> Credential ID to delete </param>
    /// <returns> No content if successful </returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDeleteCredential(Guid id)
    {
        try
        {
            _logger.LogInformation("Soft deleting credential {CredentialId}", id);

            var command = new SoftDeleteCredentialCommand(id);
            var result = await _mediator.Send(command);

            if (!result) return NotFound($"Credential with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to soft delete credential {CredentialId}", id);

            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary> Restore a soft-deleted credential using CQRS pattern </summary>
    /// <param name="id"> Credential ID to restore </param>
    /// <returns> No content if successful </returns>
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> RestoreCredential(Guid id)
    {
        try
        {
            _logger.LogInformation("Restoring credential {CredentialId}", id);

            var command = new RestoreCredentialCommand(id);
            var result = await _mediator.Send(command);

            if (!result) return NotFound($"Deleted credential with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore credential {CredentialId}", id);

            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary> Permanently delete a credential using CQRS pattern </summary>
    /// <param name="id"> Credential ID to delete </param>
    /// <returns> No content if successful </returns>
    [HttpDelete("{id:guid}/hard")]
    public async Task<IActionResult> HardDeleteCredential(Guid id)
    {
        try
        {
            _logger.LogInformation("Hard deleting credential {CredentialId}", id);

            var command = new HardDeleteCredentialCommand(id);
            var result = await _mediator.Send(command);

            if (!result) return NotFound($"Credential with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hard delete credential {CredentialId}", id);

            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary> Deactivate a credential using CQRS pattern </summary>
    /// <param name="id"> Credential ID </param>
    /// <returns> No content if successful </returns>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateCredential(Guid id)
    {
        try
        {
            _logger.LogInformation("Deactivating credential {CredentialId}", id);

            var command = new DeactivateCredentialCommand(id);
            var result = await _mediator.Send(command);

            if (!result) return NotFound($"Credential with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate credential {CredentialId}", id);

            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary> Activate a credential using CQRS pattern </summary>
    /// <param name="id"> Credential ID </param>
    /// <returns> No content if successful </returns>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateCredential(Guid id)
    {
        try
        {
            _logger.LogInformation("Activating credential {CredentialId}", id);

            var command = new ActivateCredentialCommand(id);
            var result = await _mediator.Send(command);

            if (!result) return NotFound($"Credential with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate credential {CredentialId}", id);

            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary> Get soft-deleted credentials using CQRS pattern </summary>
    /// <returns> List of soft-deleted credentials </returns>
    [HttpGet("deleted")]
    public async Task<ActionResult<IEnumerable<CredentialResponse>>> GetDeletedCredentials()
    {
        try
        {
            _logger.LogInformation("Getting deleted credentials");

            var query = new GetDeletedCredentialsQuery();
            var credentials = await _mediator.Send(query);
            var response = credentials.Select(MapToResponseDto);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get deleted credentials");

            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary> Map Credential entity to response DTO </summary>
    /// <param name="credential"> Credential entity </param>
    /// <returns> Credential response DTO </returns>
    private static CredentialResponse MapToResponseDto(Credential credential)
    {
        return new CredentialResponse
        {
            Id = credential.Id,
            UserId = credential.UserId,
            Type = credential.Type,
            Value = "***REDACTED***", // Don't expose actual credential values
            Metadata = credential.Metadata,
            ExpiresAt = credential.ExpiresAt,
            IsActive = credential.IsActive,
            LastUsedAt = credential.LastUsedAt,
            Version = credential.Version,
            CreatedAt = credential.CreatedAt,
            UpdatedAt = credential.UpdatedAt,
            DeletedAt = credential.DeletedAt,
            User = new UserResponse
            {
                Id = credential.User.Id,
                Name = credential.User.Name,
                Email = credential.User.Email,
                IsActive = credential.User.IsActive,
                Version = credential.User.Version,
                CreatedAt = credential.User.CreatedAt,
                UpdatedAt = credential.User.UpdatedAt,
                DeletedAt = credential.User.DeletedAt,
            },
        };
    }
}
