using Asp.Versioning;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Controller for managing tenant metadata.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/tenants/{tenantId:guid}/metadata")]
[Microsoft.AspNetCore.Http.Tags("tenants/metadata")]
[Authorize]
public sealed class TenantMetadataController(ISender sender) : BaseApiController
{
    [HttpGet]
    [EndpointSummary("Get tenant metadata by tenant ID")]
    [EndpointDescription("Retrieves comprehensive tenant metadata including custom fields, tags, external references, and business information.")]
    [ProducesResponseType<TenantMetadataDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMetadata(Guid tenantId, CancellationToken ct)
        => Ok(await sender.Send(new GetTenantMetadataQuery(tenantId), ct).ConfigureAwait(false));

    [HttpPatch]
    [EndpointSummary("Partially update tenant metadata by tenant ID")]
    [EndpointDescription("Updates specific tenant metadata fields without affecting other metadata. Only the provided metadata keys are modified.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateMetadata(Guid tenantId, [FromBody] UpdateTenantMetadataRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new UpdateTenantMetadataCommand(tenantId, body), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPut]
    [EndpointSummary("Replace all tenant metadata by tenant ID")]
    [EndpointDescription("Replaces all tenant metadata with new values. All existing metadata is replaced with the provided data.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReplaceMetadata(Guid tenantId, [FromBody] ReplaceTenantMetadataRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new ReplaceTenantMetadataCommand(tenantId, body), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("custom-fields")]
    [EndpointSummary("Get tenant custom fields")]
    [EndpointDescription("Retrieves all custom fields configured for the tenant as a key-value dictionary for storing tenant-specific data.")]
    [ProducesResponseType(typeof(Dictionary<string, object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCustomFields(Guid tenantId, CancellationToken ct)
        => Ok(await sender.Send(new GetTenantCustomFieldsQuery(tenantId), ct).ConfigureAwait(false));

    [HttpPatch("custom-fields")]
    [EndpointSummary("Update tenant custom fields")]
    [EndpointDescription("Updates specific custom fields for the tenant. Existing fields not specified are preserved.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCustomFields(Guid tenantId, [FromBody] Dictionary<string, object?> customFields, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(customFields);
        await sender.Send(new UpdateTenantCustomFieldsCommand(tenantId, new UpdateTenantCustomFieldsRequest(customFields)), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("tags")]
    [EndpointSummary("Get tenant tags")]
    [EndpointDescription("Retrieves all tags configured for the tenant for categorization and filtering purposes.")]
    [ProducesResponseType<List<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTags(Guid tenantId, CancellationToken ct)
        => Ok(await sender.Send(new GetTenantTagsQuery(tenantId), ct).ConfigureAwait(false));

    [HttpPatch("tags")]
    [EndpointSummary("Update tenant tags")]
    [EndpointDescription("Updates the tags for the tenant. Existing tags are merged with the new tags.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateTags(Guid tenantId, [FromBody] UpdateTenantTagsRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new UpdateTenantTagsCommand(tenantId, body), ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPut("tags")]
    [EndpointSummary("Replace all tenant tags")]
    [EndpointDescription("Replaces all existing tags with the provided list of tags.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReplaceTags(Guid tenantId, [FromBody] List<string> tags, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tags);
        await sender.Send(new ReplaceTenantTagsCommand(tenantId, new ReplaceTenantTagsRequest(tags)), ct).ConfigureAwait(false);
        return NoContent();
    }
}
