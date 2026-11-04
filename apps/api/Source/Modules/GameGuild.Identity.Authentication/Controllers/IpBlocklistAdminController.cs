using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Admin management for the proactive IP blocklist of known-attacker addresses
///     (issue #1089). Entries are stored in the distributed cache and evaluated on local
///     password sign-in according to <c>Authentication:IpBlocklist:Mode</c>. Mutations run
///     as commands; the listing reads the shared store directly.
/// </summary>
[ApiVersion("1.0")]
[Microsoft.AspNetCore.Http.Tags("auth/ip-blocklist")]
// IP blocklisting is platform security-configuration management (mirrors signing-key
// rotation): SystemAdmin role or a delegated system:manage-configuration grant.
[Authorize(Policy = Policies.SystemConfigurationManage)]
public sealed class IpBlocklistAdminController(
    IIpBlocklistService blocklist,
    ISender sender,
    ILogger<IpBlocklistAdminController> logger) : BaseApiController
{
    /// <summary>
    ///     Lists every active blocklist entry, expired entries filtered out.
    /// </summary>
    [HttpGet("v{version:apiVersion}/auth/ip-blocklist")]
    [EndpointSummary("List IP blocklist entries")]
    [EndpointDescription("Lists the active known-attacker IP blocklist entries stored in the distributed cache.")]
    [ProducesResponseType<List<IpBlocklistEntry>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<IpBlocklistEntry>>> ListEntries(CancellationToken cancellationToken)
    {
        var entries = await blocklist.ListEntriesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(entries.ToList());
    }

    /// <summary>
    ///     Adds or replaces a blocklist entry.
    /// </summary>
    [HttpPut("v{version:apiVersion}/auth/ip-blocklist")]
    [EndpointSummary("Add or replace an IP blocklist entry")]
    [EndpointDescription("Stores a known-attacker IP address in the shared blocklist. Omit expiresAt to apply the configured default entry lifetime.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddEntry([FromBody] IpBlocklistEntryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await sender.Send(new AddIpBlocklistEntryCommand
        {
            IpAddress = request.IpAddress,
            Reason = request.Reason,
            ExpiresAt = request.ExpiresAt,
            ActingAdmin = User.Identity?.Name
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        }, cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Blocklist entry rejected",
                Detail = result.Message
            });
        }

        logger.LogInformation("IP blocklist entry stored by an administrator");
        return NoContent();
    }

    /// <summary>
    ///     Removes a blocklist entry.
    /// </summary>
    [HttpDelete("v{version:apiVersion}/auth/ip-blocklist")]
    [EndpointSummary("Remove an IP blocklist entry")]
    [EndpointDescription("Removes the blocklist entry for the given IP address. Returns 404 when no entry exists.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveEntry([FromQuery] string ipAddress, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveIpBlocklistEntryCommand { IpAddress = ipAddress }, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return result.Message.Contains("No blocklist entry", StringComparison.Ordinal)
                ? NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Entry not found",
                    Detail = result.Message
                })
                : BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Blocklist entry rejected",
                    Detail = result.Message
                });
        }

        return NoContent();
    }
}

/// <summary>Admin request body for adding or replacing a blocklist entry.</summary>
public sealed class IpBlocklistEntryRequest
{
    /// <summary>The exact client IP address to blocklist.</summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>Operator note recorded with the entry (source feed, incident, ...).</summary>
    public string? Reason { get; set; }

    /// <summary>When the entry stops matching (UTC); omit for the configured default lifetime.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
