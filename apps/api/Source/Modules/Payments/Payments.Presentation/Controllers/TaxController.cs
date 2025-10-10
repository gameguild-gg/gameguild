using GameGuild.CQRS;
using GameGuild.Modules.Payments.Commands;
using GameGuild.Modules.Payments.Entities;
using GameGuild.Modules.Payments.Queries;
using GameGuild.Modules.Payments.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Payments.Controllers;

/// <summary>
///     Tax calculation and management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class TaxController : ControllerBase
{
    private readonly ISender _sender;

    public TaxController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    ///     Calculate tax for a transaction
    /// </summary>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(TaxCalculationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculateTax([FromBody] CalculateTaxRequest request, CancellationToken ct)
    {
        var command = new CalculateTaxCommand
        {
            JurisdictionCode = request.JurisdictionCode,
            Amount = request.Amount,
            Currency = request.Currency,
            CustomerType = request.CustomerType,
            ProductCategory = request.ProductCategory,
            CustomerVatNumber = request.CustomerVatNumber,
            IsTaxInclusive = request.IsTaxInclusive,
            TransactionDate = request.TransactionDate
        };

        var result = await _sender.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    ///     Get all tax jurisdictions
    /// </summary>
    [HttpGet("jurisdictions")]
    [ProducesResponseType(typeof(IEnumerable<TaxJurisdiction>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJurisdictions(CancellationToken ct)
    {
        var query = new GetTaxJurisdictionsQuery();
        var result = await _sender.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    ///     Get tax rules for a jurisdiction
    /// </summary>
    [HttpGet("rules")]
    [ProducesResponseType(typeof(IEnumerable<TaxRule>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRules(
        [FromQuery] string jurisdictionCode,
        [FromQuery] string? customerType = null,
        [FromQuery] DateTime? effectiveDate = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jurisdictionCode))
            return BadRequest("Jurisdiction code is required");

        var query = new GetTaxRulesQuery
        {
            JurisdictionCode = jurisdictionCode,
            CustomerType = customerType,
            EffectiveDate = effectiveDate
        };

        var result = await _sender.Send(query, ct);
        return Ok(result);
    }
}

/// <summary>
///     Tax calculation request DTO
/// </summary>
public class CalculateTaxRequest
{
    public required string JurisdictionCode { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string CustomerType { get; init; }
    public string? ProductCategory { get; init; }
    public string? CustomerVatNumber { get; init; }
    public bool IsTaxInclusive { get; init; }
    public DateTime? TransactionDate { get; init; }
}
