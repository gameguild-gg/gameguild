using GameGuild.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.LaunchPad;

[ApiController]
[Authorize]
[Route("v1/launch-pad/settings")]
public sealed class LaunchPadSettingsController(
    IApplicationDbContext context,
    IRequestContextAccessor requestContext,
    ILaunchPadAuthorizationService authorization) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<LaunchPadSettingsProjection>> GetSettings(CancellationToken cancellationToken)
    {
        var tenantId = requestContext.CurrentTenantId;
        if (!tenantId.HasValue) return Unauthorized();
        if (!await authorization.CanParticipateAsync(tenantId.Value, cancellationToken).ConfigureAwait(false)) return Forbid();

        var settings = await GetOrCreateAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        return Ok(LaunchPadSettingsProjection.FromEntity(settings));
    }

    [HttpPut]
    public async Task<ActionResult<LaunchPadSettingsProjection>> UpdateSettings(
        [FromBody] UpdateLaunchPadSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = requestContext.CurrentTenantId;
        if (!tenantId.HasValue) return Unauthorized();
        if (!await authorization.CanManageSettingsAsync(tenantId.Value, cancellationToken).ConfigureAwait(false)) return Forbid();

        var settings = await GetOrCreateAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        settings.VersionSubmissionPolicy = request.VersionSubmissionPolicy;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(LaunchPadSettingsProjection.FromEntity(settings));
    }

    private async Task<LaunchPadSettings> GetOrCreateAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var settings = await context.Set<LaunchPadSettings>()
            .SingleOrDefaultAsync(candidate => candidate.TenantId == tenantId && candidate.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);
        if (settings != null) return settings;

        settings = new LaunchPadSettings
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VersionSubmissionPolicy = VersionSubmissionPolicy.ReleasedImmutable
        };
        context.Set<LaunchPadSettings>().Add(settings);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return settings;
    }
}
public sealed record UpdateLaunchPadSettingsRequest(VersionSubmissionPolicy VersionSubmissionPolicy);

public sealed record LaunchPadSettingsProjection(
    Guid Id,
    Guid TenantId,
    VersionSubmissionPolicy VersionSubmissionPolicy,
    DateTime UpdatedAt)
{
    public static LaunchPadSettingsProjection FromEntity(LaunchPadSettings entity) => new(
        entity.Id,
        entity.TenantId!.Value,
        entity.VersionSubmissionPolicy,
        entity.UpdatedAt);
}
