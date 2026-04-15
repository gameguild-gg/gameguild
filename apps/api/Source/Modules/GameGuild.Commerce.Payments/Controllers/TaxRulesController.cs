using Asp.Versioning;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Tax rules controller.
///     Provides endpoints for managing tax rules.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tax-rules")]
[Tags("tax-rules")]
[Authorize]
public sealed class TaxRulesController(ISender sender) : BaseApiController
{
    /// <summary>
    ///     Get tax rules for a jurisdiction
    /// </summary>
    /// <param name="jurisdictionCode">The jurisdiction code to get rules for</param>
    /// <param name="customerType">Customer type (Individual, Business, etc.)</param>
    /// <param name="effectiveDate">Effective date for the rules</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of applicable tax rules</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaxRate>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRules(
        [FromQuery] string jurisdictionCode,
        [FromQuery] string customerType = "Individual",
        [FromQuery] DateTime? effectiveDate = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jurisdictionCode))
        {
            return BadRequest("Jurisdiction code is required");
        }

        // Parse customerType string to enum with fallback to B2C
        if (!Enum.TryParse(customerType, true, out CustomerType customerTypeEnum))
        {
            customerTypeEnum = CustomerType.B2C;
        }

        var query = new GetApplicableTaxRulesQuery(jurisdictionCode, customerTypeEnum, effectiveDate);
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Get tax rule by ID
    /// </summary>
    /// <param name="ruleId">Rule ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tax rule details</returns>
    [HttpGet("{ruleId:guid}")]
    [EndpointSummary("Get tax rule by ID")]
    [EndpointDescription("Retrieves detailed information for a specific tax rule.")]
    [ProducesResponseType(typeof(TaxRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRuleById(Guid ruleId, CancellationToken ct)
    {
        var result = await sender.Send(new GetTaxRuleByIdQuery(ruleId), ct).ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    ///     Create a new tax rule
    /// </summary>
    /// <param name="body">Rule creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created rule</returns>
    [HttpPost]
    [EndpointSummary("Create tax rule")]
    [EndpointDescription("Creates a new tax rule with the provided information.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRule([FromBody] CreateTaxRuleRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await sender.Send(new CreateTaxRuleCommand(
            body.JurisdictionCode,
            body.ProductCategory,
            body.CustomerType,
            body.Rate,
            body.EffectiveFrom,
            body.EffectiveTo,
            body.Description), ct);

        return CreatedAtAction(nameof(GetRuleById), new { ruleId = id }, new { id });
    }

    /// <summary>
    ///     Partially update a tax rule
    /// </summary>
    /// <param name="ruleId">Rule ID</param>
    /// <param name="body">Partial update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPatch("{ruleId:guid}")]
    [EndpointSummary("Partially update tax rule")]
    [EndpointDescription("Updates specific fields of a tax rule.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PatchRule(Guid ruleId, [FromBody] PatchTaxRuleRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new PatchTaxRuleCommand(
            ruleId,
            body.Rate,
            body.EffectiveFrom,
            body.EffectiveTo,
            body.Description,
            body.IsActive), ct);
        return NoContent();
    }

    /// <summary>
    ///     Delete a tax rule
    /// </summary>
    /// <param name="ruleId">Rule ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{ruleId:guid}")]
    [EndpointSummary("Delete tax rule")]
    [EndpointDescription("Deletes a tax rule by ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRule(Guid ruleId, CancellationToken ct)
    {
        await sender.Send(new DeleteTaxRuleCommand(ruleId), ct).ConfigureAwait(false);
        return NoContent();
    }
}

/// <summary>Request to create a tax rule</summary>
public sealed record CreateTaxRuleRequest(
    string JurisdictionCode,
    string? ProductCategory,
    string CustomerType,
    decimal Rate,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string? Description);

/// <summary>Request to partially update a tax rule</summary>
public sealed record PatchTaxRuleRequest(
    decimal? Rate = null,
    DateTime? EffectiveFrom = null,
    DateTime? EffectiveTo = null,
    string? Description = null,
    bool? IsActive = null);

/// <summary>DTO for tax rule</summary>
public sealed record TaxRuleDto(
    Guid Id,
    string JurisdictionCode,
    string? ProductCategory,
    string CustomerType,
    decimal Rate,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string? Description,
    bool IsActive);

