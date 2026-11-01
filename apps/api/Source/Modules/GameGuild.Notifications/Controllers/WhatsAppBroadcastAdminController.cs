using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Notifications.Services.WhatsApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GameGuild.Notifications.Controllers;

/// <summary>
///     WhatsApp broadcast campaign administration (issue #55): create, schedule, send,
///     cancel and inspect template campaigns delivered to filtered tenant segments.
///     Delivery tracking is exposed per campaign through the recipient rows. Requires
///     the Admin policy; campaigns are tenant-scoped by construction.
/// </summary>
[ApiVersion("1.0")]
[Microsoft.AspNetCore.Http.Tags("notifications")]
[Route("api")]
[Authorize(Policy = Policies.Admin)]
public class WhatsAppBroadcastAdminController : BaseApiController
{
    private readonly IWhatsAppBroadcastService broadcastService;
    private readonly IWhatsAppProvider provider;
    private readonly IOptions<WhatsAppSettings> settings;
    private readonly IActorContextAccessor actorContextAccessor;
    private readonly ISender sender;

    public WhatsAppBroadcastAdminController(
        IWhatsAppBroadcastService broadcastService,
        IWhatsAppProvider provider,
        IOptions<WhatsAppSettings> settings,
        IActorContextAccessor actorContextAccessor,
        ISender sender)
    {
        this.broadcastService = broadcastService;
        this.provider = provider;
        this.settings = settings;
        this.actorContextAccessor = actorContextAccessor;
        this.sender = sender;
    }

    private Guid GetAdminUserId()
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.SubjectIdAsGuid.HasValue)
        {
            throw new UnauthorizedAccessException("User must be authenticated");
        }

        return actor.SubjectIdAsGuid.Value;
    }

    /// <summary>
    /// Gets the WhatsApp channel status: whether the Business API integration is configured and which templates are allowlisted.
    /// </summary>
    [HttpGet("v{version:apiVersion}/whatsapp/status")]
    [ProducesResponseType(typeof(WhatsAppChannelStatusDto), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        return Ok(new WhatsAppChannelStatusDto(
            settings.Value.Enabled,
            provider.IsConfigured,
            settings.Value.TemplateAllowlist.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList()));
    }

    /// <summary>
    /// Creates a draft broadcast campaign. The template must be on the configured allowlist.
    /// </summary>
    [HttpPost("v{version:apiVersion}/whatsapp/broadcasts")]
    [ProducesResponseType(typeof(WhatsAppBroadcastDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateWhatsAppBroadcastRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new CreateWhatsAppBroadcastCommand(new WhatsAppBroadcastCreateRequest(
                request.TenantId,
                request.Title,
                request.TemplateName,
                request.LanguageCode,
                request.VariablesJson,
                request.SegmentRole,
                request.SegmentActiveMembersOnly ?? true,
                GetAdminUserId())),
            cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return FromError(result.Error);
        }

        return CreatedAtAction(
            nameof(Get),
            new { version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0", tenantId = result.Value.TenantId, id = result.Value.Id },
            MapToDto(result.Value));
    }

    /// <summary>
    /// Lists broadcast campaigns for a tenant (newest first).
    /// </summary>
    [HttpGet("v{version:apiVersion}/whatsapp/broadcasts")]
    [ProducesResponseType(typeof(PagedResult<WhatsAppBroadcastDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid tenantId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await broadcastService.ListAsync(tenantId, skip, take, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return FromError(result.Error);
        }

        return Ok(new PagedResult<WhatsAppBroadcastDto>(
            result.Value.Items.Select(MapToDto),
            result.Value.TotalCount,
            result.Value.Skip,
            result.Value.Take));
    }

    /// <summary>
    /// Gets one broadcast campaign.
    /// </summary>
    [HttpGet("v{version:apiVersion}/whatsapp/broadcasts/{id:guid}")]
    [ProducesResponseType(typeof(WhatsAppBroadcastDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        [FromRoute] Guid id,
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var result = await broadcastService.GetAsync(tenantId, id, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return FromError(result.Error);
        }

        return Ok(MapToDto(result.Value));
    }

    /// <summary>
    /// Schedules a draft broadcast for a future send time.
    /// </summary>
    [HttpPost("v{version:apiVersion}/whatsapp/broadcasts/{id:guid}:schedule")]
    [ProducesResponseType(typeof(WhatsAppBroadcastDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Schedule(
        [FromRoute] Guid id,
        [FromBody] ScheduleWhatsAppBroadcastRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ScheduleWhatsAppBroadcastCommand(request.TenantId, id, request.ScheduledAtUtc), cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return FromError(result.Error);
        }

        return Ok(MapToDto(result.Value));
    }

    /// <summary>
    /// Sends a draft or due scheduled broadcast now and returns the send summary.
    /// </summary>
    [HttpPost("v{version:apiVersion}/whatsapp/broadcasts/{id:guid}:send")]
    [ProducesResponseType(typeof(WhatsAppBroadcastSendReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Send(
        [FromRoute] Guid id,
        [FromBody] SendWhatsAppBroadcastRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SendWhatsAppBroadcastCommand(request.TenantId, id), cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return FromError(result.Error);
        }

        return Ok(new WhatsAppBroadcastSendReportDto(
            result.Value.BroadcastId,
            result.Value.Attempted,
            result.Value.Sent,
            result.Value.Failed,
            result.Value.Skipped));
    }

    /// <summary>
    /// Cancels a draft or scheduled broadcast.
    /// </summary>
    [HttpPost("v{version:apiVersion}/whatsapp/broadcasts/{id:guid}:cancel")]
    [ProducesResponseType(typeof(WhatsAppBroadcastDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid id,
        [FromBody] CancelWhatsAppBroadcastRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new CancelWhatsAppBroadcastCommand(request.TenantId, id), cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return FromError(result.Error);
        }

        return Ok(MapToDto(result.Value));
    }

    /// <summary>
    /// Gets the delivery-tracking rows of one broadcast campaign (oldest first).
    /// </summary>
    [HttpGet("v{version:apiVersion}/whatsapp/broadcasts/{id:guid}/recipients")]
    [ProducesResponseType(typeof(PagedResult<WhatsAppBroadcastRecipientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecipients(
        [FromRoute] Guid id,
        [FromQuery] Guid tenantId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await broadcastService.GetRecipientsAsync(tenantId, id, skip, take, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return FromError(result.Error);
        }

        return Ok(new PagedResult<WhatsAppBroadcastRecipientDto>(
            result.Value.Items.Select(MapToRecipientDto),
            result.Value.TotalCount,
            result.Value.Skip,
            result.Value.Take));
    }

    private static WhatsAppBroadcastDto MapToDto(WhatsAppBroadcast broadcast) => new(
        broadcast.Id,
        broadcast.TenantId!.Value,
        broadcast.Title,
        broadcast.TemplateName,
        broadcast.LanguageCode,
        broadcast.SegmentRole,
        broadcast.SegmentActiveMembersOnly,
        broadcast.Status.ToString(),
        broadcast.ScheduledAtUtc,
        broadcast.SentAtUtc,
        broadcast.RecipientCount,
        broadcast.CreatedBy,
        broadcast.CreatedAt,
        broadcast.Error);

    private static WhatsAppBroadcastRecipientDto MapToRecipientDto(WhatsAppBroadcastRecipient recipient) => new(
        recipient.Id,
        recipient.BroadcastId,
        recipient.UserId,
        recipient.PhoneNumber,
        recipient.Status.ToString(),
        recipient.ProviderMessageId,
        recipient.Error,
        recipient.AttemptCount,
        recipient.SentAtUtc);

    // Explicit ProblemDetails (not ControllerBase.Problem) so direct unit invocation works without ProblemDetailsFactory.
    private static ObjectResult FromError(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        return new ObjectResult(new ProblemDetails { Status = status, Title = error.Code, Detail = error.Description })
        {
            StatusCode = status
        };
    }
}

#region Requests and DTOs

public sealed record CreateWhatsAppBroadcastRequest(
    Guid TenantId,
    string Title,
    string TemplateName,
    string? LanguageCode = null,
    string? VariablesJson = null,
    string? SegmentRole = null,
    bool? SegmentActiveMembersOnly = true);

public sealed record ScheduleWhatsAppBroadcastRequest(Guid TenantId, DateTime ScheduledAtUtc);

public sealed record SendWhatsAppBroadcastRequest(Guid TenantId);

public sealed record CancelWhatsAppBroadcastRequest(Guid TenantId);

public sealed record WhatsAppChannelStatusDto(
    bool Enabled,
    bool IsConfigured,
    IReadOnlyList<string> AllowlistedTemplates);

public sealed record WhatsAppBroadcastDto(
    Guid Id,
    Guid TenantId,
    string Title,
    string TemplateName,
    string LanguageCode,
    string? SegmentRole,
    bool SegmentActiveMembersOnly,
    string Status,
    DateTime? ScheduledAtUtc,
    DateTime? SentAtUtc,
    int RecipientCount,
    Guid CreatedBy,
    DateTime CreatedAt,
    string? Error);

public sealed record WhatsAppBroadcastSendReportDto(
    Guid BroadcastId,
    int Attempted,
    int Sent,
    int Failed,
    int Skipped);

public sealed record WhatsAppBroadcastRecipientDto(
    Guid Id,
    Guid BroadcastId,
    Guid UserId,
    string PhoneNumber,
    string Status,
    string? ProviderMessageId,
    string? Error,
    int AttemptCount,
    DateTime? SentAtUtc);

#endregion
