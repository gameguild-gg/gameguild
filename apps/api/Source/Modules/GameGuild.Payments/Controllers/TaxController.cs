using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Payments.Commands;
using GameGuild.Payments.Entities;
using GameGuild.Payments.Queries;
using GameGuild.Payments.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Payments.Controllers;

/// <summary>
///     Tax calculation and management controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class TaxController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Calculate tax for a transaction
    /// </summary>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(TaxCalculationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculateTax([FromBody] CalculateTaxRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new CalculateTaxCommand(
            request.JurisdictionCode,
            request.Amount,
            request.Currency,
            request.CustomerType,
            request.ProductCategory,
            request.CustomerVatNumber,
            request.IsTaxInclusive,
            request.TransactionDate,
            request.ApplicableExemptions
        );

        var result = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Get all tax jurisdictions
    /// </summary>
    [HttpGet("jurisdictions")]
    [ProducesResponseType(typeof(IEnumerable<TaxRate>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJurisdictions(CancellationToken ct)
    {
        var query = new GetTaxJurisdictionsQuery();
        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Get tax rules for a jurisdiction
    /// </summary>
    [HttpGet("rules")]
    [ProducesResponseType(typeof(IEnumerable<TaxRate>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRules([FromQuery] string jurisdictionCode, [FromQuery] string customerType = "Individual", [FromQuery] DateTime? effectiveDate = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jurisdictionCode)) return BadRequest("Jurisdiction code is required");

        // Parse customerType string to enum with fallback to B2C
        if (!Enum.TryParse(customerType, true, out CustomerType customerTypeEnum)) { customerTypeEnum = CustomerType.B2C; }

        var query = new GetApplicableTaxRulesQuery(jurisdictionCode, customerTypeEnum, effectiveDate);

        var result = await sender.Send(query, ct).ConfigureAwait(false);

        return Ok(result);
    }
}
