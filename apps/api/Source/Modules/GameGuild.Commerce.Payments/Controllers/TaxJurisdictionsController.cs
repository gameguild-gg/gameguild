using Asp.Versioning;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Tax jurisdictions controller.
///     Provides endpoints for managing tax jurisdictions.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tax-jurisdictions")]
[Tags("tax-jurisdictions")]
[Authorize]
public sealed class TaxJurisdictionsController(ISender sender) : BaseApiController
{
    /// <summary>
    ///     Get all tax jurisdictions
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of tax jurisdictions</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaxRate>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJurisdictions(CancellationToken ct)
    {
        var query = new GetTaxJurisdictionsQuery();
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Get tax jurisdiction by ID
    /// </summary>
    /// <param name="jurisdictionId">Jurisdiction ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tax jurisdiction details</returns>
    [HttpGet("{jurisdictionId:guid}")]
    [EndpointSummary("Get tax jurisdiction by ID")]
    [EndpointDescription("Retrieves detailed information for a specific tax jurisdiction.")]
    [ProducesResponseType(typeof(TaxJurisdictionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJurisdictionById(Guid jurisdictionId, CancellationToken ct)
    {
        var result = await sender.Send(new GetTaxJurisdictionByIdQuery(jurisdictionId), ct).ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    ///     Create a new tax jurisdiction
    /// </summary>
    /// <param name="body">Jurisdiction creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created jurisdiction</returns>
    [HttpPost]
    [EndpointSummary("Create tax jurisdiction")]
    [EndpointDescription("Creates a new tax jurisdiction with the provided information.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateJurisdiction([FromBody] CreateTaxJurisdictionRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await sender.Send(new CreateTaxJurisdictionCommand(
            body.Code,
            body.Name,
            body.Country,
            body.State,
            body.TaxType,
            body.DefaultRate), ct);

        return CreatedAtAction(nameof(GetJurisdictionById), new { jurisdictionId = id }, new { id });
    }

    /// <summary>
    ///     Partially update a tax jurisdiction
    /// </summary>
    /// <param name="jurisdictionId">Jurisdiction ID</param>
    /// <param name="body">Partial update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPatch("{jurisdictionId:guid}")]
    [EndpointSummary("Partially update tax jurisdiction")]
    [EndpointDescription("Updates specific fields of a tax jurisdiction.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PatchJurisdiction(Guid jurisdictionId, [FromBody] PatchTaxJurisdictionRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new PatchTaxJurisdictionCommand(
            jurisdictionId,
            body.Name,
            body.TaxType,
            body.DefaultRate,
            body.IsActive), ct).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    ///     Delete a tax jurisdiction
    /// </summary>
    /// <param name="jurisdictionId">Jurisdiction ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{jurisdictionId:guid}")]
    [EndpointSummary("Delete tax jurisdiction")]
    [EndpointDescription("Deletes a tax jurisdiction by ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteJurisdiction(Guid jurisdictionId, CancellationToken ct)
    {
        await sender.Send(new DeleteTaxJurisdictionCommand(jurisdictionId), ct).ConfigureAwait(false);
        return NoContent();
    }
}

/// <summary>Request to create a tax jurisdiction</summary>
public sealed record CreateTaxJurisdictionRequest(
    string Code,
    string Name,
    string Country,
    string? State,
    string TaxType,
    decimal DefaultRate);

/// <summary>Request to partially update a tax jurisdiction</summary>
public sealed record PatchTaxJurisdictionRequest(
    string? Name = null,
    string? TaxType = null,
    decimal? DefaultRate = null,
    bool? IsActive = null);

/// <summary>DTO for tax jurisdiction</summary>
public sealed record TaxJurisdictionDto(
    Guid Id,
    string Code,
    string Name,
    string Country,
    string? State,
    string TaxType,
    decimal DefaultRate,
    bool IsActive);

