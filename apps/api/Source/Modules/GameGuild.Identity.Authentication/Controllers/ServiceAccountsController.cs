using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using GameGuild.Configuration.PresentationLayer.RateLimiting;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Controller for managing service accounts (machine-to-machine authentication).
///     Provides OAuth2 client_credentials grant and service account CRUD operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/auth/service-accounts")]
[Produces("application/json")]
public class ServiceAccountsController : AuthControllerBase
{
    private readonly IServiceAccountService _serviceAccountService;
    private readonly IJwtTokenService _jwtTokenService;

    public ServiceAccountsController(
        IServiceAccountService serviceAccountService,
        IJwtTokenService jwtTokenService)
    {
        _serviceAccountService = serviceAccountService;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    ///     OAuth2 client_credentials grant - authenticates a service account and returns a JWT token.
    /// </summary>
    /// <remarks>
    ///     This endpoint implements the OAuth2 client_credentials flow for machine-to-machine authentication.
    ///     The returned access token can be used to authenticate API requests.
    /// </remarks>
    /// <param name="request">The client credentials request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An OAuth2 token response with access token.</returns>
    [HttpPost("/v{version:apiVersion}/oauth/token")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [ProducesResponseType(typeof(ClientCredentialsTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OAuth2ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(OAuth2ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Token([FromForm] ClientCredentialsRequest request, CancellationToken cancellationToken)
    {
        // Validate grant type
        if (request.GrantType != "client_credentials")
        {
            return BadRequest(new OAuth2ErrorResponse
            {
                Error = "unsupported_grant_type",
                ErrorDescription = "Only 'client_credentials' grant type is supported"
            });
        }

        if (string.IsNullOrEmpty(request.ClientId) || string.IsNullOrEmpty(request.ClientSecret))
        {
            return BadRequest(new OAuth2ErrorResponse
            {
                Error = "invalid_request",
                ErrorDescription = "client_id and client_secret are required"
            });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var serviceAccount = await _serviceAccountService.AuthenticateAsync(
            request.ClientId,
            request.ClientSecret,
            ipAddress,
            cancellationToken);

        if (serviceAccount == null)
        {
            return Unauthorized(new OAuth2ErrorResponse
            {
                Error = "invalid_client",
                ErrorDescription = "Invalid client credentials"
            });
        }

        // Generate JWT token for the service account
        var (accessToken, expiresAt) = await _jwtTokenService.GenerateServiceAccountTokenAsync(
            serviceAccount.Id.ToString(),
            serviceAccount.ClientId,
            serviceAccount.Name,
            serviceAccount.GetScopesSet(),
            serviceAccount.TenantId,
            cancellationToken);

        return Ok(new ClientCredentialsTokenResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = (int)(expiresAt - DateTime.UtcNow).TotalSeconds,
            Scope = serviceAccount.Scopes
        });
    }

    /// <summary>
    ///     Creates a new service account.
    /// </summary>
    /// <remarks>
    ///     The client secret is only returned once during creation. Store it securely.
    /// </remarks>
    /// <param name="request">The service account creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created service account with client credentials.</returns>
    [HttpPost]
    [Authorize(Policy = "RequireAdminRole")]
    [EnableRateLimiting(RateLimitPolicies.Authorization)]
    [ProducesResponseType(typeof(ServiceAccountCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateServiceAccount(
        [FromBody] CreateServiceAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Name is required" });
        }

        var createdBy = GetCurrentUserId().ToString();

        var (account, clientSecret) = await _serviceAccountService.CreateServiceAccountAsync(
            request.Name,
            request.Description,
            request.TenantId,
            request.Scopes ?? string.Empty,
            createdBy,
            request.AllowedIpAddresses,
            request.ExpiresAt,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetServiceAccount),
            new { id = account.Id },
            new ServiceAccountCreatedResponse
            {
                Id = account.Id,
                ClientId = account.ClientId,
                ClientSecret = clientSecret, // Only returned once!
                Name = account.Name,
                Description = account.Description,
                TenantId = account.TenantId,
                Scopes = account.Scopes,
                CreatedAt = account.CreatedAt,
                ExpiresAt = account.ExpiresAt,
                Warning = "Store the client_secret securely. It will not be shown again."
            });
    }

    /// <summary>
    ///     Gets a service account by ID.
    /// </summary>
    [HttpGet("{serviceAccountId:guid}")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(typeof(ServiceAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServiceAccount(Guid serviceAccountId, CancellationToken cancellationToken)
    {
        var account = await _serviceAccountService.GetByIdAsync(serviceAccountId, cancellationToken);
        if (account == null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(account));
    }

    /// <summary>
    ///     Checks if a service account exists by ID.
    /// </summary>
    /// <param name="serviceAccountId">Service account ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>200 OK if exists, 404 Not Found if not</returns>
    [HttpHead("{serviceAccountId:guid}")]
    [Authorize(Policy = "RequireAdminRole")]
    [EndpointSummary("Check if service account exists")]
    [EndpointDescription("Checks if a service account exists without returning the body.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckServiceAccountExists(Guid serviceAccountId, CancellationToken cancellationToken)
    {
        var account = await _serviceAccountService.GetByIdAsync(serviceAccountId, cancellationToken);
        return account == null ? NotFound() : Ok();
    }

    /// <summary>
    ///     Partially updates a service account.
    /// </summary>
    /// <param name="serviceAccountId">Service account ID</param>
    /// <param name="request">Update request with optional fields</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPatch("{serviceAccountId:guid}")]
    [Authorize(Policy = "RequireAdminRole")]
    [EndpointSummary("Partially update service account")]
    [EndpointDescription("Updates specific fields of a service account. Only provided fields are updated.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PatchServiceAccount(
        Guid serviceAccountId,
        [FromBody] PatchServiceAccountRequest request,
        CancellationToken cancellationToken)
    {
        var account = await _serviceAccountService.GetByIdAsync(serviceAccountId, cancellationToken);
        if (account == null)
        {
            return NotFound();
        }

        // Update only provided fields
        if (!string.IsNullOrEmpty(request.Name))
        {
            account.Name = request.Name;
        }
        if (request.Description != null)
        {
            account.Description = request.Description;
        }
        if (!string.IsNullOrEmpty(request.Scopes))
        {
            await _serviceAccountService.UpdateScopesAsync(serviceAccountId, request.Scopes, cancellationToken);
        }
        if (request.ExpiresAt.HasValue)
        {
            account.ExpiresAt = request.ExpiresAt.Value;
        }

        return NoContent();
    }

    /// <summary>
    ///     Gets all service accounts with optional tenant filtering.
    /// </summary>
    /// <param name="tenantId">Optional tenant ID to filter service accounts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of service accounts</returns>
    [HttpGet]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(typeof(IEnumerable<ServiceAccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServiceAccounts([FromQuery] Guid? tenantId, CancellationToken cancellationToken)
    {
        if (tenantId.HasValue)
        {
            var accounts = await _serviceAccountService.GetByTenantAsync(tenantId.Value, cancellationToken);
            return Ok(accounts.Select(MapToResponse));
        }

        // TODO: Implement GetAllAsync in service if needed for admin use case
        var allAccounts = await _serviceAccountService.GetByTenantAsync(Guid.Empty, cancellationToken);
        return Ok(allAccounts.Select(MapToResponse));
    }

    /// <summary>
    ///     Rotates the client secret for a service account.
    /// </summary>
    /// <remarks>
    ///     The new client secret is only returned once. Store it securely.
    ///     The old secret is immediately invalidated.
    /// </remarks>
    [HttpPost("{serviceAccountId:guid}:rotate-secret")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(typeof(SecretRotationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RotateSecret(Guid serviceAccountId, CancellationToken cancellationToken)
    {
        try
        {
            var newSecret = await _serviceAccountService.RotateSecretAsync(serviceAccountId, cancellationToken);
            return Ok(new SecretRotationResponse
            {
                ClientSecret = newSecret,
                Warning = "Store the new client_secret securely. It will not be shown again."
            });
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    ///     Unlocks a locked service account.
    /// </summary>
    [HttpPost("{serviceAccountId:guid}:unlock")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unlock(Guid serviceAccountId, CancellationToken cancellationToken)
    {
        try
        {
            await _serviceAccountService.UnlockAsync(serviceAccountId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    ///     Locks a service account to prevent authentication.
    /// </summary>
    /// <param name="serviceAccountId">Service account ID</param>
    /// <param name="request">Lock request with reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("{serviceAccountId:guid}:lock")]
    [Authorize(Policy = "RequireAdminRole")]
    [EndpointSummary("Lock service account")]
    [EndpointDescription("Locks a service account to prevent it from authenticating.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Lock(Guid serviceAccountId, [FromBody] LockServiceAccountRequest request, CancellationToken cancellationToken)
    {
        var account = await _serviceAccountService.GetByIdAsync(serviceAccountId, cancellationToken);
        if (account == null)
        {
            return NotFound();
        }

        await _serviceAccountService.LockAsync(serviceAccountId, request.Reason, cancellationToken);
        return NoContent();
    }

    /// <summary>
    ///     Gets the audit log for a service account.
    /// </summary>
    /// <param name="serviceAccountId">Service account ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated audit log entries</returns>
    [HttpGet("{serviceAccountId:guid}/audit-log")]
    [Authorize(Policy = "RequireAdminRole")]
    [EndpointSummary("Get service account audit log")]
    [EndpointDescription("Retrieves the audit log of actions performed on or by a service account.")]
    [ProducesResponseType(typeof(ServiceAccountAuditLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditLog(
        Guid serviceAccountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var account = await _serviceAccountService.GetByIdAsync(serviceAccountId, cancellationToken);
        if (account == null)
        {
            return NotFound();
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var auditLog = await _serviceAccountService.GetAuditLogAsync(
            serviceAccountId,
            (page - 1) * pageSize,
            pageSize,
            cancellationToken);

        return Ok(new ServiceAccountAuditLogResponse
        {
            ServiceAccountId = serviceAccountId,
            Entries = auditLog.Items,
            TotalCount = auditLog.TotalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    ///     Deactivates a service account.
    /// </summary>
    [HttpPost("{serviceAccountId:guid}:deactivate")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid serviceAccountId, CancellationToken cancellationToken)
    {
        try
        {
            await _serviceAccountService.DeactivateAsync(serviceAccountId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    ///     Reactivates a deactivated service account.
    /// </summary>
    [HttpPost("{serviceAccountId:guid}:reactivate")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(Guid serviceAccountId, CancellationToken cancellationToken)
    {
        try
        {
            await _serviceAccountService.ReactivateAsync(serviceAccountId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    ///     Updates the scopes for a service account.
    /// </summary>
    [HttpPatch("{serviceAccountId:guid}/scopes")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateScopes(Guid serviceAccountId, [FromBody] UpdateScopesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _serviceAccountService.UpdateScopesAsync(serviceAccountId, request.Scopes, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    ///     Deletes a service account.
    /// </summary>
    [HttpDelete("{serviceAccountId:guid}")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteServiceAccount(Guid serviceAccountId, CancellationToken cancellationToken)
    {
        var account = await _serviceAccountService.GetByIdAsync(serviceAccountId, cancellationToken);
        if (account == null)
        {
            return NotFound();
        }

        await _serviceAccountService.DeactivateAsync(serviceAccountId, cancellationToken);
        return NoContent();
    }

    private static ServiceAccountResponse MapToResponse(ServiceAccount account) => new()
    {
        Id = account.Id,
        ClientId = account.ClientId,
        Name = account.Name,
        Description = account.Description,
        TenantId = account.TenantId,
        Scopes = account.Scopes,
        IsActive = account.IsActive,
        IsLocked = account.IsLocked,
        ExpiresAt = account.ExpiresAt,
        CreatedAt = account.CreatedAt,
        CreatedBy = account.CreatedBy,
        LastAuthenticatedAt = account.LastAuthenticatedAt,
        AuthenticationCount = account.AuthenticationCount,
        SecretRotationCount = account.SecretRotationCount
    };
}

#region DTOs

/// <summary>
///     OAuth2 client_credentials request.
/// </summary>
public class ClientCredentialsRequest
{
    [FromForm(Name = "grant_type")]
    public string GrantType { get; set; } = string.Empty;

    [FromForm(Name = "client_id")]
    public string ClientId { get; set; } = string.Empty;

    [FromForm(Name = "client_secret")]
    public string ClientSecret { get; set; } = string.Empty;

    [FromForm(Name = "scope")]
    public string? Scope { get; set; }
}

/// <summary>
///     OAuth2 token response.
/// </summary>
public class ClientCredentialsTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public string? Scope { get; set; }
}

/// <summary>
///     OAuth2 error response.
/// </summary>
public class OAuth2ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? ErrorDescription { get; set; }
}

/// <summary>
///     Request to create a service account.
/// </summary>
public class CreateServiceAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? TenantId { get; set; }
    public string? Scopes { get; set; }
    public string? AllowedIpAddresses { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
///     Response when a service account is created (includes secret).
/// </summary>
public class ServiceAccountCreatedResponse
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? TenantId { get; set; }
    public string Scopes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Warning { get; set; } = string.Empty;
}

/// <summary>
///     Response for service account (excludes secret).
/// </summary>
public class ServiceAccountResponse
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? TenantId { get; set; }
    public string Scopes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? LastAuthenticatedAt { get; set; }
    public long AuthenticationCount { get; set; }
    public int SecretRotationCount { get; set; }
}

/// <summary>
///     Response when secret is rotated.
/// </summary>
public class SecretRotationResponse
{
    public string ClientSecret { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
}

/// <summary>
///     Request to update scopes.
/// </summary>
public class UpdateScopesRequest
{
    public string Scopes { get; set; } = string.Empty;
}

/// <summary>
///     Request to partially update a service account.
/// </summary>
public class PatchServiceAccountRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Scopes { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
///     Request to lock a service account.
/// </summary>
public class LockServiceAccountRequest
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
///     Response for service account audit log.
/// </summary>
public class ServiceAccountAuditLogResponse
{
    public Guid ServiceAccountId { get; set; }
    public IEnumerable<ServiceAccountAuditEntry> Entries { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
///     Single entry in service account audit log.
/// </summary>
public class ServiceAccountAuditEntry
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? PerformedBy { get; set; }
    public string? IpAddress { get; set; }
    public string? Details { get; set; }
}

#endregion
