using Asp.Versioning;
using GameGuild.CQRS;


using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Tax calculation controller.
///     Provides endpoints for calculating taxes.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/taxes")]
[Tags("taxes")]
public sealed class TaxesController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Calculate tax for a transaction
    /// </summary>
    [HttpPost(":calculate")]
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
}
