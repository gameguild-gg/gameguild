using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Resources;

/// <summary>
///     Tenant Resource Metadata API Controller - RESTful API for managing tenant-level resource metadata
/// </summary>
/// <remarks>
///     All endpoints require authentication. Tenant membership validation is enforced.
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Tags("tenants/resources/metadata")]
[Authorize]
public sealed class TenantResourceMetadataController(IResourceMetadataRepository metadataRepository) : ControllerBase
{
    /// <summary>
    ///     Get all metadata entries for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant unique identifier</param>
    /// <param name="category">Optional filter by category</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of metadata entries</returns>
    [HttpGet("v{version:apiVersion}/tenants/{tenantId:guid}/resources/metadata")]
    [EndpointSummary("Get all metadata entries for a tenant")]
    [EndpointDescription("Retrieves all resource metadata entries for a specific tenant, optionally filtered by category.")]
    [ProducesResponseType<IEnumerable<ResourceMetadata>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantMetadata(Guid tenantId, [FromQuery] string? category, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(category)) { return Ok(await metadataRepository.GetByCategoryAsync(tenantId, category, ct).ConfigureAwait(false)); }

        return Ok(await metadataRepository.GetByTenantAsync(tenantId, ct).ConfigureAwait(false));
    }

    /// <summary>
    ///     Get a specific metadata entry by key
    /// </summary>
    /// <param name="tenantId">Tenant unique identifier</param>
    /// <param name="key">Metadata key</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Metadata entry</returns>
    [HttpGet("v{version:apiVersion}/tenants/{tenantId:guid}/resources/metadata/{key}")]
    [EndpointSummary("Get a specific metadata entry by key")]
    [EndpointDescription("Retrieves a specific resource metadata entry by its key for a tenant.")]
    [ProducesResponseType<ResourceMetadata>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantMetadataByKey(Guid tenantId, string key, CancellationToken ct)
    {
        var metadata = await metadataRepository.GetByKeyAsync(tenantId, key, ct).ConfigureAwait(false);

        if (metadata == null) return NotFound($"Metadata not found for key: {key}");

        return Ok(metadata);
    }

    /// <summary>
    ///     Create or update a metadata entry
    /// </summary>
    /// <param name="tenantId">Tenant unique identifier</param>
    /// <param name="key">Metadata key</param>
    /// <param name="body">Metadata entry data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created or updated metadata entry</returns>
    [HttpPut("v{version:apiVersion}/tenants/{tenantId:guid}/resources/metadata/{key}")]
    [EndpointSummary("Create or update a metadata entry")]
    [EndpointDescription("Creates a new metadata entry or updates an existing one for a tenant.")]
    [ProducesResponseType<ResourceMetadata>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetTenantMetadata(Guid tenantId, string key, [FromBody] SetResourceMetadataRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        var existing = await metadataRepository.GetByKeyAsync(tenantId, key, ct).ConfigureAwait(false);

        if (existing != null)
        {
            existing.Value = body.Value;
            existing.DataType = body.DataType ?? existing.DataType;
            existing.Description = body.Description ?? existing.Description;
            existing.Category = body.Category ?? existing.Category;
            existing.DisplayOrder = body.DisplayOrder ?? existing.DisplayOrder;
            existing.UpdatedAt = DateTime.UtcNow;

            await metadataRepository.UpdateAsync(existing, ct).ConfigureAwait(false);

            return Ok(existing);
        }

        var metadata = new ResourceMetadata
        {
            Key = key,
            Value = body.Value,
            DataType = body.DataType ?? "String",
            Description = body.Description,
            Category = body.Category,
            DisplayOrder = body.DisplayOrder ?? 0,
            IsActive = true
        };

        // Set TenantId using reflection since the setter is protected
        var tenantIdProperty = typeof(ResourceMetadata).GetProperty("TenantId");
        tenantIdProperty?.GetSetMethod(nonPublic: true)?.Invoke(metadata, new object[] { tenantId });

        await metadataRepository.CreateAsync(metadata, ct).ConfigureAwait(false);

        return Ok(metadata);
    }

    /// <summary>
    ///     Delete a metadata entry
    /// </summary>
    /// <param name="tenantId">Tenant unique identifier</param>
    /// <param name="key">Metadata key</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("v{version:apiVersion}/tenants/{tenantId:guid}/resources/metadata/{key}")]
    [EndpointSummary("Delete a metadata entry")]
    [EndpointDescription("Removes a resource metadata entry for a tenant.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTenantMetadata(Guid tenantId, string key, CancellationToken ct)
    {
        var deleted = await metadataRepository.DeleteByKeyAsync(tenantId, key, ct).ConfigureAwait(false);

        if (!deleted) return NotFound($"Metadata not found for key: {key}");

        return NoContent();
    }
}
