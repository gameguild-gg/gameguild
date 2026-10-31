using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Notifications.Services.WhatsApp;

/// <summary>Send summary for one executed broadcast campaign.</summary>
/// <param name="BroadcastId">Campaign id.</param>
/// <param name="Attempted">Recipients the provider was asked to deliver to.</param>
/// <param name="Sent">Recipients the provider accepted.</param>
/// <param name="Failed">Recipients the provider rejected.</param>
/// <param name="Skipped">Audience members deliberately not sent to (opted out).</param>
public sealed record WhatsAppBroadcastSendReport(
    Guid BroadcastId,
    int Attempted,
    int Sent,
    int Failed,
    int Skipped);

/// <summary>Page of items with total count (mirrors the email admin service contract).</summary>
public sealed record WhatsAppBroadcastPage<T>(IReadOnlyList<T> Items, int TotalCount, int Skip, int Take);

/// <summary>
///     Broadcast campaign management (issue #55): create, schedule, cancel, send and
///     inspect admin-authored WhatsApp template campaigns against filtered tenant
///     segments. Campaigns are tenant-scoped by construction, bounded by
///     <see cref="WhatsAppBroadcastOptions.MaxRecipientsPerBroadcast"/>, restricted to
///     allowlisted templates, and only delivered to users who opted into the WhatsApp
///     channel and the marketing category (issue #54 preference surface).
/// </summary>
public interface IWhatsAppBroadcastService
{
    /// <summary>Creates a draft campaign (template must be allowlisted).</summary>
    Task<Result<WhatsAppBroadcast>> CreateAsync(WhatsAppBroadcastCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Schedules a draft campaign for a future time.</summary>
    Task<Result<WhatsAppBroadcast>> ScheduleAsync(Guid tenantId, Guid broadcastId, DateTime scheduledAtUtc, CancellationToken cancellationToken = default);

    /// <summary>Sends a draft or due scheduled campaign now and returns the send summary.</summary>
    Task<Result<WhatsAppBroadcastSendReport>> SendAsync(Guid tenantId, Guid broadcastId, CancellationToken cancellationToken = default);

    /// <summary>Cancels a draft or scheduled campaign.</summary>
    Task<Result<WhatsAppBroadcast>> CancelAsync(Guid tenantId, Guid broadcastId, CancellationToken cancellationToken = default);

    /// <summary>Gets one campaign (tenant-scoped).</summary>
    Task<Result<WhatsAppBroadcast>> GetAsync(Guid tenantId, Guid broadcastId, CancellationToken cancellationToken = default);

    /// <summary>Lists campaigns for a tenant (newest first).</summary>
    Task<Result<WhatsAppBroadcastPage<WhatsAppBroadcast>>> ListAsync(Guid tenantId, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Lists the delivery-tracking rows of one campaign (tenant-scoped).</summary>
    Task<Result<WhatsAppBroadcastPage<WhatsAppBroadcastRecipient>>> GetRecipientsAsync(Guid tenantId, Guid broadcastId, int skip, int take, CancellationToken cancellationToken = default);
}

/// <summary>Creation payload for a broadcast campaign.</summary>
/// <param name="TenantId">Owning tenant.</param>
/// <param name="Title">Admin-facing campaign label.</param>
/// <param name="TemplateName">Allowlisted WhatsApp template name.</param>
/// <param name="LanguageCode">Optional template language (defaults to en_US).</param>
/// <param name="VariablesJson">Optional JSON object of shared template variables.</param>
/// <param name="SegmentRole">Optional tenant membership role filter.</param>
/// <param name="SegmentActiveMembersOnly">Limit to active memberships (default true).</param>
/// <param name="CreatedBy">Admin user creating the campaign.</param>
public sealed record WhatsAppBroadcastCreateRequest(
    Guid TenantId,
    string Title,
    string TemplateName,
    string? LanguageCode,
    string? VariablesJson,
    string? SegmentRole,
    bool SegmentActiveMembersOnly,
    Guid CreatedBy);

/// <summary>
///     Default <see cref="IWhatsAppBroadcastService"/> implementation.
/// </summary>
public sealed class WhatsAppBroadcastService(
    IApplicationDbContext context,
    IWhatsAppProvider provider,
    IWhatsAppBroadcastAudienceResolver audienceResolver,
    IOptions<WhatsAppSettings> settings,
    IOptions<WhatsAppBroadcastOptions> options,
    ILogger<WhatsAppBroadcastService> logger) : IWhatsAppBroadcastService
{
    private const int MaxTake = 100;

    /// <inheritdoc />
    public async Task<Result<WhatsAppBroadcast>> CreateAsync(WhatsAppBroadcastCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Result.Failure<WhatsAppBroadcast>(Error.Validation("WhatsAppBroadcast.TitleRequired", "A campaign title is required."));
        }

        if (string.IsNullOrWhiteSpace(request.TemplateName) || !settings.Value.IsTemplateAllowed(request.TemplateName))
        {
            return Result.Failure<WhatsAppBroadcast>(Error.Validation(
                "WhatsAppBroadcast.TemplateNotAllowed",
                $"Template '{request.TemplateName}' is not in the WhatsApp template allowlist (Notifications:WhatsApp:TemplateAllowlist)."));
        }

        if (!string.IsNullOrWhiteSpace(request.VariablesJson))
        {
            try
            {
                using var _ = System.Text.Json.JsonDocument.Parse(request.VariablesJson);
            }
            catch (System.Text.Json.JsonException)
            {
                return Result.Failure<WhatsAppBroadcast>(Error.Validation(
                    "WhatsAppBroadcast.InvalidVariables", "Variables must be a JSON object of template placeholder values."));
            }
        }

        var broadcast = WhatsAppBroadcast.Create(
            request.TenantId,
            request.Title.Trim(),
            request.TemplateName.Trim(),
            request.LanguageCode,
            request.VariablesJson,
            request.SegmentRole,
            request.SegmentActiveMembersOnly,
            request.CreatedBy);

        context.Set<WhatsAppBroadcast>().Add(broadcast);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("WhatsApp broadcast created. BroadcastId: {BroadcastId}, Tenant: {TenantId}, Template: {Template}",
            broadcast.Id, LogRedaction.Sanitize(broadcast.TenantId.ToString()), broadcast.TemplateName);

        return Result.Success(broadcast);
    }

    /// <inheritdoc />
    public async Task<Result<WhatsAppBroadcast>> ScheduleAsync(Guid tenantId, Guid broadcastId, DateTime scheduledAtUtc, CancellationToken cancellationToken = default)
    {
        var broadcast = await FindAsync(tenantId, broadcastId, cancellationToken).ConfigureAwait(false);
        if (broadcast is null)
        {
            return NotFound(broadcastId);
        }

        if (scheduledAtUtc <= SystemClock.UtcNow)
        {
            return Result.Failure<WhatsAppBroadcast>(Error.Validation(
                "WhatsAppBroadcast.InvalidSchedule", "The scheduled time must be in the future."));
        }

        try
        {
            broadcast.Schedule(scheduledAtUtc);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<WhatsAppBroadcast>(Error.Conflict("WhatsAppBroadcast.InvalidState", ex.Message));
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(broadcast);
    }

    /// <inheritdoc />
    public async Task<Result<WhatsAppBroadcastSendReport>> SendAsync(Guid tenantId, Guid broadcastId, CancellationToken cancellationToken = default)
    {
        var broadcast = await FindAsync(tenantId, broadcastId, cancellationToken).ConfigureAwait(false);
        if (broadcast is null)
        {
            return Result.Failure<WhatsAppBroadcastSendReport>(NotFound(broadcastId).Error);
        }

        if (!provider.IsConfigured)
        {
            return Result.Failure<WhatsAppBroadcastSendReport>(Error.Problem(
                "WhatsApp.NotConfigured",
                "WhatsApp delivery is not configured; broadcasts require the WhatsApp Business Cloud API credentials."));
        }

        if (!settings.Value.IsTemplateAllowed(broadcast.TemplateName))
        {
            return Result.Failure<WhatsAppBroadcastSendReport>(Error.Validation(
                "WhatsAppBroadcast.TemplateNotAllowed",
                $"Template '{broadcast.TemplateName}' is no longer in the WhatsApp template allowlist."));
        }

        try
        {
            broadcast.MarkSending();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<WhatsAppBroadcastSendReport>(Error.Conflict("WhatsAppBroadcast.InvalidState", ex.Message));
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var audience = await audienceResolver.ResolveAsync(
            broadcast.TenantId!.Value,
            broadcast.SegmentRole,
            broadcast.SegmentActiveMembersOnly,
            cancellationToken).ConfigureAwait(false);

        var cap = options.Value.MaxRecipientsPerBroadcast;
        if (audience.Count > cap)
        {
            broadcast.MarkFailed($"audience of {audience.Count} exceeds the configured cap of {cap}");
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Failure<WhatsAppBroadcastSendReport>(Error.Validation(
                "WhatsAppBroadcast.AudienceTooLarge",
                $"The segment resolves to {audience.Count} recipients which exceeds the configured cap of {cap}."));
        }

        var optedIn = await ResolveOptedInUserIdsAsync(audience, cancellationToken).ConfigureAwait(false);

        var recipients = new List<WhatsAppBroadcastRecipient>(audience.Count);
        var skipped = 0;
        foreach (var candidate in audience)
        {
            var recipient = WhatsAppBroadcastRecipient.Create(broadcast.Id, broadcast.TenantId!.Value, candidate.UserId, candidate.PhoneNumber);
            if (!optedIn.Contains(candidate.UserId))
            {
                recipient.MarkSkipped("opted out: WhatsApp channel or marketing category disabled");
                skipped++;
            }

            recipients.Add(recipient);
        }

        context.Set<WhatsAppBroadcastRecipient>().AddRange(recipients);
        broadcast.RecordRecipients(recipients.Count);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var placeholders = DeserializePlaceholders(broadcast.VariablesJson);
        var batchSize = Math.Max(1, options.Value.SendBatchSize);
        var sent = 0;
        var failed = 0;

        for (var offset = 0; offset < recipients.Count; offset += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = recipients.Skip(offset).Take(batchSize).Where(r => r.Status == WhatsAppBroadcastDeliveryStatus.Pending);

            foreach (var recipient in batch)
            {
                var sendResult = await provider.SendTemplateAsync(
                    new WhatsAppTemplateMessage(recipient.PhoneNumber, broadcast.TemplateName, broadcast.LanguageCode, placeholders),
                    cancellationToken).ConfigureAwait(false);

                if (sendResult.IsSuccess)
                {
                    recipient.MarkSent(sendResult.Value.ProviderMessageId);
                    sent++;
                }
                else
                {
                    recipient.MarkFailed(sendResult.Error.Description);
                    failed++;
                }
            }

            // Persist after each bounded batch so progress survives an interrupted sweep.
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        broadcast.MarkSent();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "WhatsApp broadcast delivered. BroadcastId: {BroadcastId}, Attempted: {Attempted}, Sent: {Sent}, Failed: {Failed}, Skipped: {Skipped}",
            broadcast.Id, sent + failed, sent, failed, skipped);

        return Result.Success(new WhatsAppBroadcastSendReport(broadcast.Id, sent + failed, sent, failed, skipped));
    }

    /// <inheritdoc />
    public async Task<Result<WhatsAppBroadcast>> CancelAsync(Guid tenantId, Guid broadcastId, CancellationToken cancellationToken = default)
    {
        var broadcast = await FindAsync(tenantId, broadcastId, cancellationToken).ConfigureAwait(false);
        if (broadcast is null)
        {
            return NotFound(broadcastId);
        }

        try
        {
            broadcast.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<WhatsAppBroadcast>(Error.Conflict("WhatsAppBroadcast.InvalidState", ex.Message));
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(broadcast);
    }

    /// <inheritdoc />
    public async Task<Result<WhatsAppBroadcast>> GetAsync(Guid tenantId, Guid broadcastId, CancellationToken cancellationToken = default)
    {
        var broadcast = await FindAsync(tenantId, broadcastId, cancellationToken).ConfigureAwait(false);
        return broadcast is null ? NotFound(broadcastId) : Result.Success(broadcast);
    }

    /// <inheritdoc />
    public async Task<Result<WhatsAppBroadcastPage<WhatsAppBroadcast>>> ListAsync(Guid tenantId, int skip, int take, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, MaxTake);

        var query = context.Set<WhatsAppBroadcast>()
            .Where(b => b.TenantId == tenantId);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new WhatsAppBroadcastPage<WhatsAppBroadcast>(items, total, skip, take));
    }

    /// <inheritdoc />
    public async Task<Result<WhatsAppBroadcastPage<WhatsAppBroadcastRecipient>>> GetRecipientsAsync(Guid tenantId, Guid broadcastId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var broadcast = await FindAsync(tenantId, broadcastId, cancellationToken).ConfigureAwait(false);
        if (broadcast is null)
        {
            return Result.Failure<WhatsAppBroadcastPage<WhatsAppBroadcastRecipient>>(NotFound(broadcastId).Error);
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, MaxTake);

        var query = context.Set<WhatsAppBroadcastRecipient>()
            .Where(r => r.BroadcastId == broadcastId);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderBy(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new WhatsAppBroadcastPage<WhatsAppBroadcastRecipient>(items, total, skip, take));
    }

    /// <summary>
    ///     Resolves the subset of audience ids allowed to receive broadcast marketing:
    ///     an explicit preference row with both the WhatsApp channel and the marketing
    ///     category enabled. Users without a preference row are opted out (WhatsApp is
    ///     strictly opt-in).
    /// </summary>
    private async Task<HashSet<Guid>> ResolveOptedInUserIdsAsync(IReadOnlyList<WhatsAppAudienceCandidate> audience, CancellationToken cancellationToken)
    {
        var userIds = audience.Select(c => c.UserId).Distinct().ToList();
        var preferences = await context.Set<NotificationPreference>()
            .Where(p => userIds.Contains(p.UserId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return preferences
            .Where(p => p.WhatsAppEnabled && p.MarketingEnabled)
            .Select(p => p.UserId)
            .ToHashSet();
    }

    private static IReadOnlyDictionary<string, string>? DeserializePlaceholders(string? variablesJson)
    {
        if (string.IsNullOrWhiteSpace(variablesJson))
        {
            return null;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(variablesJson);
        }
        catch (System.Text.Json.JsonException)
        {
            // Creation validated the JSON; treat deserialization failure as no placeholders.
            return null;
        }
    }

    private async Task<WhatsAppBroadcast?> FindAsync(Guid tenantId, Guid broadcastId, CancellationToken cancellationToken) =>
        await context.Set<WhatsAppBroadcast>()
            .FirstOrDefaultAsync(b => b.Id == broadcastId && b.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false);

    private static Result<WhatsAppBroadcast> NotFound(Guid broadcastId) =>
        Result.Failure<WhatsAppBroadcast>(Error.NotFound(
            "WhatsAppBroadcast.NotFound", $"WhatsApp broadcast with ID {broadcastId} not found for this tenant."));
}
