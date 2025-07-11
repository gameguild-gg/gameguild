using GameGuild.Modules.Credentials.Commands;
using GameGuild.Modules.Credentials.Queries;
using GameGuild.Modules.Users;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// REST API controller for managing user credentials using CQRS pattern
/// </summary>
[ApiController]
[Route("[controller]")]
public class CredentialsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CredentialsController> _logger;

    public CredentialsController(IMediator mediator, ILogger<CredentialsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all credentials using CQRS pattern
    /// </summary>
    /// <returns>List of credentials</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CredentialResponseDto>>> GetCredentials()
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

  /// <summary>
  /// Get credentials by user ID using CQRS pattern
  /// </summary>
  /// <param name="userId">User ID</param>
  /// <returns>List of user credentials</returns>
  [HttpGet("user/{userId:guid}")]
  public async Task<ActionResult<IEnumerable<CredentialResponseDto>>> GetCredentialsByUserId(Guid userId)
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

  /// <summary>
  /// Get a specific credential by ID using CQRS pattern
  /// </summary>
  /// <param name="id">Credential ID</param>
  /// <returns>Credential details</returns>
  [HttpGet("{id:guid}")]
  public async Task<ActionResult<CredentialResponseDto>> GetCredential(Guid id)
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

  /// <summary>
  /// Get a credential by user ID and type using CQRS pattern
  /// </summary>
  /// <param name="userId">User ID</param>
  /// <param name="type">Credential type</param>
  /// <returns>Credential details</returns>
  [HttpGet("user/{userId}/type/{type}")]
  public async Task<ActionResult<CredentialResponseDto>> GetCredentialByUserIdAndType(Guid userId, string type)
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
  }  /// <summary>
  /// Create a new credential using CQRS pattern
  /// </summary>
  /// <param name="createDto">Credential data</param>
  /// <returns>Created credential</returns>
  [HttpPost]
  public async Task<ActionResult<CredentialResponseDto>> CreateCredential([FromBody] CreateCredentialDto createDto)
  {
    try
    {
      if (!ModelState.IsValid) return BadRequest(ModelState);

      _logger.LogInformation("Creating credential for user {UserId}", createDto.UserId);

      var command = new CreateCredentialCommand
      {
        UserId = createDto.UserId,
        Type = createDto.Type,
        Value = createDto.Value,
        Metadata = createDto.Metadata,
        ExpiresAt = createDto.ExpiresAt,
        IsActive = createDto.IsActive
      };

      var createdCredential = await _mediator.Send(command);
      var response = MapToResponseDto(createdCredential);

      return CreatedAtAction(nameof(GetCredential), new { id = createdCredential.Id }, response);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to create credential for user {UserId}", createDto.UserId);
      return StatusCode(500, "Internal server error");
    }
  }

  /// <summary>
  /// Update an existing credential using CQRS pattern
  /// </summary>
  /// <param name="id">Credential ID</param>
  /// <param name="updateDto">Updated credential data</param>
  /// <returns>Updated credential</returns>
  [HttpPut("{id}")]
  public async Task<ActionResult<CredentialResponseDto>> UpdateCredential(
    Guid id,
    [FromBody] UpdateCredentialDto updateDto
  )
  {
    try
    {
      if (!ModelState.IsValid) return BadRequest(ModelState);

      _logger.LogInformation("Updating credential {CredentialId}", id);

      var command = new UpdateCredentialCommand
      {
        Id = id,
        Type = updateDto.Type,
        Value = updateDto.Value,
        Metadata = updateDto.Metadata,
        ExpiresAt = updateDto.ExpiresAt,
        IsActive = updateDto.IsActive
      };

      var updatedCredential = await _mediator.Send(command);
      var response = MapToResponseDto(updatedCredential);

      return Ok(response);
    }
    catch (ArgumentException ex)
    {
      return NotFound(ex.Message);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to update credential {CredentialId}", id);
      return StatusCode(500, "Internal server error");
    }
  }

  /// <summary>
  /// Soft delete a credential using CQRS pattern
  /// </summary>
  /// <param name="id">Credential ID to delete</param>
  /// <returns>No content if successful</returns>
  [HttpDelete("{id}")]
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

  /// <summary>
  /// Restore a soft-deleted credential using CQRS pattern
  /// </summary>
  /// <param name="id">Credential ID to restore</param>
  /// <returns>No content if successful</returns>
  [HttpPost("{id}/restore")]
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

  /// <summary>
  /// Permanently delete a credential using CQRS pattern
  /// </summary>
  /// <param name="id">Credential ID to delete</param>
  /// <returns>No content if successful</returns>
  [HttpDelete("{id}/hard")]
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

  /// <summary>
  /// Mark a credential as used using CQRS pattern
  /// </summary>
  /// <param name="id">Credential ID</param>
  /// <returns>No content if successful</returns>
  [HttpPost("{id}/mark-used")]
  public async Task<IActionResult> MarkCredentialAsUsed(Guid id)
  {
    try
    {
      _logger.LogInformation("Marking credential {CredentialId} as used", id);

      var command = new MarkCredentialAsUsedCommand(id);
      var result = await _mediator.Send(command);

      if (!result) return NotFound($"Credential with ID {id} not found");

      return NoContent();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to mark credential {CredentialId} as used", id);
      return StatusCode(500, "Internal server error");
    }
  }

  /// <summary>
  /// Deactivate a credential using CQRS pattern
  /// </summary>
  /// <param name="id">Credential ID</param>
  /// <returns>No content if successful</returns>
  [HttpPost("{id}/deactivate")]
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

  /// <summary>
  /// Activate a credential using CQRS pattern
  /// </summary>
  /// <param name="id">Credential ID</param>
  /// <returns>No content if successful</returns>
  [HttpPost("{id}/activate")]
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

  /// <summary>
  /// Get soft-deleted credentials using CQRS pattern
  /// </summary>
  /// <returns>List of soft-deleted credentials</returns>
  [HttpGet("deleted")]
  public async Task<ActionResult<IEnumerable<CredentialResponseDto>>> GetDeletedCredentials()
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

  /// <summary>
  /// Map Credential entity to response DTO
  /// </summary>
  /// <param name="credential">Credential entity</param>
  /// <returns>Credential response DTO</returns>
  private static CredentialResponseDto MapToResponseDto(Credential credential) {
    return new CredentialResponseDto {
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
      User = credential.User != null
               ? new UserResponseDto {
                 Id = credential.User.Id,
                 Name = credential.User.Name,
                 Email = credential.User.Email,
                 IsActive = credential.User.IsActive,
                 Version = credential.User.Version,
                 CreatedAt = credential.User.CreatedAt,
                 UpdatedAt = credential.User.UpdatedAt,
                 DeletedAt = credential.User.DeletedAt,
               }
               : null,
    };
  }
}
