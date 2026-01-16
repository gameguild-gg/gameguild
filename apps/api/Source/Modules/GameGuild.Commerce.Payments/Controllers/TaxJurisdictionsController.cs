using Asp.Versioning;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Tax jurisdictions controller.
///     Provides endpoints for retrieving tax jurisdictions.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/tax-jurisdictions")]
[Tags("tax-jurisdictions")]
public sealed class TaxJurisdictionsController(ISender sender) : ControllerBase
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
}
