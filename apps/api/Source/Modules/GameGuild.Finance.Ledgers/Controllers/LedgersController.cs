using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;
using GameGuild.Finance.Ledgers.Commands;
using GameGuild.Finance.Ledgers.Enums;
using GameGuild.Finance.Ledgers.Models;
using GameGuild.Finance.Ledgers.Queries;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Finance.Ledgers.Controllers;

/// <summary>
/// API controller for ledger management operations.
/// </summary>
/// <remarks>
///     <para>
///         <b>SECURITY:</b> every action requires authentication plus the
///         <c>Ledgers.Read</c> (reads) or <c>Ledgers.Write</c> (mutations) policy.
///     </para>
///     <para>
///         <b>TENANCY:</b> the effective tenant is the authenticated actor's tenant; a
///         route-supplied tenant is only honored for SystemAdmin. Reads are scoped to the
///         caller's tenant, and <c>CreatedByUserId</c> is always taken from the actor —
///         never from the request body.
///     </para>
/// </remarks>
[Microsoft.AspNetCore.Http.Tags("finance/ledgers")]
[ApiController]
[Route("api/v1/ledgers")]
[Produces("application/json")]
[Authorize]
public class LedgersController(ISender sender, IActorContextAccessor actorContextAccessor) : ControllerBase
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    // ========================================================================
    // Ledger CRUD
    // ========================================================================

    /// <summary>
    /// Gets a ledger by ID.
    /// </summary>
    [HttpGet("{ledgerId:guid}")]
    [Authorize(Policy = Policies.LedgersRead)]
    [ProducesResponseType(typeof(LedgerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LedgerDto>> GetById(Guid ledgerId, CancellationToken ct)
    {
        var result = await sender.Send(new GetLedgerByIdQuery(ledgerId), ct);
        if (result is null || !IsVisibleInCallerTenant(result.TenantId))
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets a ledger by code within tenant.
    /// </summary>
    [HttpGet("by-code/{tenantId:guid}/{code}")]
    [Authorize(Policy = Policies.LedgersRead)]
    [ProducesResponseType(typeof(LedgerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LedgerDto>> GetByCode(Guid tenantId, string code, CancellationToken ct)
    {
        if (!CanReadTenant(tenantId))
        {
            return Forbid();
        }

        var result = await sender.Send(new GetLedgerByCodeQuery(tenantId, code), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Gets all ledgers for a tenant.
    /// </summary>
    [HttpGet("tenant/{tenantId:guid}")]
    [Authorize(Policy = Policies.LedgersRead)]
    [ProducesResponseType(typeof(IReadOnlyList<LedgerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<LedgerDto>>> GetByTenant(
        Guid tenantId,
        [FromQuery] LedgerStatus? status = null,
        [FromQuery] LedgerType? type = null,
        CancellationToken ct = default)
    {
        if (!CanReadTenant(tenantId))
        {
            return Forbid();
        }

        var result = await sender.Send(new GetLedgersByTenantQuery(tenantId, status, type), ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets root ledgers for a tenant.
    /// </summary>
    [HttpGet("tenant/{tenantId:guid}/roots")]
    [Authorize(Policy = Policies.LedgersRead)]
    [ProducesResponseType(typeof(IReadOnlyList<LedgerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<LedgerDto>>> GetRootLedgers(Guid tenantId, CancellationToken ct)
    {
        if (!CanReadTenant(tenantId))
        {
            return Forbid();
        }

        var result = await sender.Send(new GetRootLedgersQuery(tenantId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets direct children of a ledger.
    /// </summary>
    [HttpGet("{ledgerId:guid}/children")]
    [Authorize(Policy = Policies.LedgersRead)]
    [ProducesResponseType(typeof(IReadOnlyList<LedgerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<LedgerDto>>> GetChildren(Guid ledgerId, CancellationToken ct)
    {
        if (!await CanReadLedgerAsync(ledgerId, ct))
        {
            return NotFound();
        }

        var result = await sender.Send(new GetLedgerChildrenQuery(ledgerId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets the hierarchy tree starting from a ledger.
    /// </summary>
    [HttpGet("{ledgerId:guid}/hierarchy")]
    [Authorize(Policy = Policies.LedgersRead)]
    [ProducesResponseType(typeof(LedgerTreeNode), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LedgerTreeNode>> GetHierarchy(
        Guid ledgerId,
        [FromQuery] int? maxDepth = null,
        CancellationToken ct = default)
    {
        if (!await CanReadLedgerAsync(ledgerId, ct))
        {
            return NotFound();
        }

        var result = await sender.Send(new GetLedgerHierarchyQuery(ledgerId, maxDepth), ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets the ancestor path from root to ledger.
    /// </summary>
    [HttpGet("{ledgerId:guid}/ancestors")]
    [Authorize(Policy = Policies.LedgersRead)]
    [ProducesResponseType(typeof(IReadOnlyList<LedgerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<LedgerDto>>> GetAncestors(Guid ledgerId, CancellationToken ct)
    {
        if (!await CanReadLedgerAsync(ledgerId, ct))
        {
            return NotFound();
        }

        var result = await sender.Send(new GetLedgerAncestorsQuery(ledgerId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets descendants of a ledger.
    /// </summary>
    [HttpGet("{ledgerId:guid}/descendants")]
    [Authorize(Policy = Policies.LedgersRead)]
    [ProducesResponseType(typeof(IReadOnlyList<LedgerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<LedgerDto>>> GetDescendants(
        Guid ledgerId,
        [FromQuery] int? maxDepth = null,
        CancellationToken ct = default)
    {
        if (!await CanReadLedgerAsync(ledgerId, ct))
        {
            return NotFound();
        }

        var result = await sender.Send(new GetLedgerDescendantsQuery(ledgerId, maxDepth), ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets virtual ledgers for a tenant.
    /// </summary>
    [HttpGet("tenant/{tenantId:guid}/virtual")]
    [Authorize(Policy = Policies.LedgersRead)]
    [ProducesResponseType(typeof(IReadOnlyList<LedgerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<LedgerDto>>> GetVirtualLedgers(Guid tenantId, CancellationToken ct)
    {
        if (!CanReadTenant(tenantId))
        {
            return Forbid();
        }

        var result = await sender.Send(new GetVirtualLedgersQuery(tenantId), ct);
        return Ok(result);
    }

    // ========================================================================
    // Create Operations
    // ========================================================================

    /// <summary>
    /// Creates a new root ledger.
    /// </summary>
    [HttpPost("root")]
    [Authorize(Policy = Policies.LedgersWrite)]
    [ProducesResponseType(typeof(LedgerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LedgerDto>> CreateRootLedger(
        [FromBody] CreateRootLedgerRequest request,
        CancellationToken ct)
    {
        // TENANCY: the owning tenant comes from the actor's context. A SystemAdmin may
        // explicitly create for another tenant; everyone else is pinned to their own.
        Guid tenantId;
        if (Actor.IsSystemAdmin && request.TenantId != Guid.Empty)
        {
            tenantId = request.TenantId;
        }
        else
        {
            var actorTenant = Actor.TenantId;
            if (actorTenant is null || actorTenant == Guid.Empty)
            {
                return Forbid();
            }

            tenantId = actorTenant.Value;
        }

        var createdByUserId = ActorUserId;

        var command = new CreateRootLedgerCommand(
            tenantId,
            request.Code,
            request.Name,
            request.CurrencyCode,
            createdByUserId,
            request.Description,
            request.Tags);

        var result = (LedgerDto)await sender.Send(command, ct)!;
        return CreatedAtAction(nameof(GetById), new { ledgerId = result.Id }, result);
    }

    /// <summary>
    /// Creates a new child ledger under a parent.
    /// </summary>
    [HttpPost("{parentLedgerId:guid}/children")]
    [Authorize(Policy = Policies.LedgersWrite)]
    [ProducesResponseType(typeof(LedgerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LedgerDto>> CreateChildLedger(
        Guid parentLedgerId,
        [FromBody] CreateChildLedgerRequest request,
        CancellationToken ct)
    {
        // TENANCY: the parent ledger must belong to the caller's tenant.
        if (!await CanReadLedgerAsync(parentLedgerId, ct))
        {
            return NotFound();
        }

        var createdByUserId = ActorUserId;

        var command = new CreateChildLedgerCommand(
            parentLedgerId,
            request.Type,
            request.Code,
            request.Name,
            createdByUserId,
            request.Description,
            request.CurrencyCode,
            request.IsShared,
            request.BudgetLimit,
            request.ProjectStartDate,
            request.ProjectEndDate,
            request.Tags);

        var result = (LedgerDto)await sender.Send(command, ct)!;
        return CreatedAtAction(nameof(GetById), new { ledgerId = result.Id }, result);
    }

    // ========================================================================
    // Tenant scoping helpers
    // ========================================================================

    private Guid ActorUserId =>
        Actor.SubjectIdAsGuid ?? throw new UnauthorizedAccessException("Authenticated actor required");

    /// <summary>
    /// True when a route-supplied tenant may be read by the current actor:
    /// SystemAdmin reads any tenant, everyone else only their own.
    /// </summary>
    private bool CanReadTenant(Guid tenantId) =>
        Actor.IsSystemAdmin || (Actor.TenantId == tenantId && tenantId != Guid.Empty);

    /// <summary>
    /// True when the ledger belongs to a tenant the actor may read. Ledgers in another
    /// tenant are invisible (404) to non-SystemAdmin callers.
    /// </summary>
    private async Task<bool> CanReadLedgerAsync(Guid ledgerId, CancellationToken ct)
    {
        var ledger = await sender.Send(new GetLedgerByIdQuery(ledgerId), ct);
        return ledger is not null && IsVisibleInCallerTenant(ledger.TenantId);
    }

    private bool IsVisibleInCallerTenant(Guid ledgerTenantId) =>
        Actor.IsSystemAdmin || Actor.TenantId == ledgerTenantId;
}

// ============================================================================
// Request DTOs
// ============================================================================

public record CreateRootLedgerRequest(
    Guid TenantId,
    string Code,
    string Name,
    string CurrencyCode,
    Guid CreatedByUserId,
    string? Description = null,
    IEnumerable<string>? Tags = null);

public record CreateChildLedgerRequest(
    LedgerType Type,
    string Code,
    string Name,
    Guid CreatedByUserId,
    string? Description = null,
    string? CurrencyCode = null,
    bool IsShared = false,
    decimal? BudgetLimit = null,
    DateOnly? ProjectStartDate = null,
    DateOnly? ProjectEndDate = null,
    IEnumerable<string>? Tags = null);
