using Asp.Versioning;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Tax rules controller.
///     Provides endpoints for retrieving tax rules.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/tax-rules")]
[Tags("tax-rules")]
public sealed class TaxRulesController(ISender sender) : ControllerBase
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
}
