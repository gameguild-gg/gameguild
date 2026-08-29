using GameGuild.Identity.Context.Actors;
using GameGuild.Assets;
using GameGuild.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.LaunchPad;

[ApiController]
[Authorize]
[Route("v1/launch-pad/events")]
public sealed class LaunchPadEventsController(
    IApplicationDbContext context,
    IRequestContextAccessor requestContext,
    IActorContextAccessor actors,
    ILaunchPadAuthorizationService authorization,
    IAssetScopedAccessService assetScopedAccessService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<ActionResult<IReadOnlyList<LaunchPadEventProjection>>> GetPublicEvents(CancellationToken cancellationToken)
    {
        var tenantId = CurrentTenantId();
        if (!tenantId.HasValue) return BadRequest(new { code = "LaunchPad.TenantRequired" });
        var visibleStatuses = new[]
        {
            LaunchPadEventStatus.ApplicationsOpen, LaunchPadEventStatus.ApplicationsClosed,
            LaunchPadEventStatus.Scheduled, LaunchPadEventStatus.Active, LaunchPadEventStatus.Completed
        };
        var events = await context.Set<LaunchPadEvent>().AsNoTracking()
            .Where(entity => entity.TenantId == tenantId && entity.DeletedAt == null && visibleStatuses.Contains(entity.Status))
            .OrderBy(entity => entity.StartsAt)
            .Select(entity => LaunchPadEventProjection.FromEntity(entity))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(events);
    }

    [AllowAnonymous]
    [HttpGet("public/{id:guid}")]
    public async Task<ActionResult<LaunchPadEventDetailProjection>> GetPublicEvent(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = CurrentTenantId();
        if (!tenantId.HasValue) return BadRequest(new { code = "LaunchPad.TenantRequired" });
        var entity = await context.Set<LaunchPadEvent>().AsNoTracking()
            .Include(candidate => candidate.Slots)
            .FirstOrDefaultAsync(candidate => candidate.Id == id && candidate.TenantId == tenantId && candidate.DeletedAt == null,
                cancellationToken).ConfigureAwait(false);
        if (entity == null || entity.Status is LaunchPadEventStatus.Draft or LaunchPadEventStatus.Archived)
            return NotFound();
        return Ok(LaunchPadEventDetailProjection.FromEntity(entity));
    }

    [HttpGet("management")]
    public async Task<ActionResult<IReadOnlyList<LaunchPadEventProjection>>> GetManagedEvents(CancellationToken cancellationToken)
    {
        var tenantId = CurrentTenantId();
        if (!tenantId.HasValue) return Unauthorized();
        if (!await authorization.CanManageEventsAsync(tenantId.Value, cancellationToken).ConfigureAwait(false)) return Forbid();
        var events = await context.Set<LaunchPadEvent>().AsNoTracking()
            .Where(entity => entity.TenantId == tenantId && entity.DeletedAt == null)
            .OrderByDescending(entity => entity.StartsAt)
            .Select(entity => LaunchPadEventProjection.FromEntity(entity))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(events);
    }

    [HttpGet("{id:guid}/management")]
    public async Task<ActionResult<LaunchPadEventDetailProjection>> GetManagedEvent(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = CurrentTenantId();
        if (!tenantId.HasValue) return Unauthorized();
        if (!await authorization.CanManageEventsAsync(tenantId.Value, cancellationToken).ConfigureAwait(false)) return Forbid();
        var entity = await context.Set<LaunchPadEvent>().AsNoTracking()
            .Include(candidate => candidate.Slots.Where(slot => slot.DeletedAt == null))
            .SingleOrDefaultAsync(candidate => candidate.Id == id && candidate.TenantId == tenantId && candidate.DeletedAt == null,
                cancellationToken).ConfigureAwait(false);
        return entity == null ? NotFound() : Ok(LaunchPadEventDetailProjection.FromEntity(entity));
    }

    [HttpPost]
    public async Task<ActionResult<LaunchPadEventProjection>> CreateEvent(
        [FromBody] CreateLaunchPadEventRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = CurrentTenantId();
        if (!tenantId.HasValue) return Unauthorized();
        if (!await authorization.CanManageEventsAsync(tenantId.Value, cancellationToken).ConfigureAwait(false)) return Forbid();
        try
        {
            var entity = LaunchPadEvent.Create(tenantId.Value, request.Name, request.StartsAt, request.EndsAt, request.Description);
            if (request.ApplicationsOpenAt.HasValue && request.ApplicationsCloseAt.HasValue)
                entity.ConfigureApplicationWindow(request.ApplicationsOpenAt.Value, request.ApplicationsCloseAt.Value);
            context.Set<LaunchPadEvent>().Add(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetPublicEvent), new { id = entity.Id }, LaunchPadEventProjection.FromEntity(entity));
        }
        catch (ArgumentException exception)
        {
            return UnprocessableEntity(new { code = "LaunchPad.Validation", message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { code = "LaunchPad.InvalidState", message = exception.Message });
        }
    }

    [HttpPost("{id:guid}:transition")]
    public async Task<ActionResult<LaunchPadEventProjection>> TransitionEvent(
        Guid id,
        [FromBody] TransitionLaunchPadEventRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await FindEventAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity == null) return NotFound();
        if (!await authorization.CanManageEventsAsync(entity.TenantId!.Value, cancellationToken).ConfigureAwait(false)) return Forbid();
        try
        {
            switch (request.Status)
            {
                case LaunchPadEventStatus.ApplicationsOpen: entity.OpenApplications(); break;
                case LaunchPadEventStatus.ApplicationsClosed: entity.CloseApplications(); break;
                case LaunchPadEventStatus.Scheduled: entity.Schedule(); break;
                case LaunchPadEventStatus.Active: entity.Activate(); break;
                case LaunchPadEventStatus.Completed: entity.Complete(); break;
                case LaunchPadEventStatus.Cancelled: entity.Cancel(); break;
                case LaunchPadEventStatus.Archived: entity.Archive(); break;
                default: return UnprocessableEntity(new { code = "LaunchPad.InvalidTargetStatus" });
            }
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Ok(LaunchPadEventProjection.FromEntity(entity));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { code = "LaunchPad.InvalidState", message = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LaunchPadEventProjection>> UpdateEvent(
        Guid id,
        [FromBody] UpdateLaunchPadEventRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await FindEventAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity == null) return NotFound();
        if (!await authorization.CanManageEventsAsync(entity.TenantId!.Value, cancellationToken).ConfigureAwait(false)) return Forbid();
        try
        {
            entity.Update(request.Name, request.Description, request.StartsAt, request.EndsAt,
                request.ApplicationsOpenAt, request.ApplicationsCloseAt);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Ok(LaunchPadEventProjection.FromEntity(entity));
        }
        catch (ArgumentException exception)
        {
            return UnprocessableEntity(new { code = "LaunchPad.Validation", message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { code = "LaunchPad.InvalidState", message = exception.Message });
        }
    }

    [HttpPost("{eventId:guid}/slots")]
    public async Task<ActionResult<LaunchPadSlotProjection>> CreateSlot(
        Guid eventId,
        [FromBody] CreateLaunchPadSlotRequest request,
        CancellationToken cancellationToken)
    {
        var launchEvent = await FindEventAsync(eventId, cancellationToken).ConfigureAwait(false);
        if (launchEvent == null) return NotFound();
        if (!await authorization.CanManageEventsAsync(launchEvent.TenantId!.Value, cancellationToken).ConfigureAwait(false)) return Forbid();
        try
        {
            if (request.StartsAt < launchEvent.StartsAt || request.EndsAt > launchEvent.EndsAt)
                return UnprocessableEntity(new { code = "LaunchPad.SlotOutsideEvent" });
            var slot = LaunchPadParticipantSlot.Create(launchEvent.TenantId.Value, eventId, request.Name, request.Role,
                request.Capacity, request.StartsAt, request.EndsAt);
            context.Set<LaunchPadParticipantSlot>().Add(slot);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Created(string.Empty, LaunchPadSlotProjection.FromEntity(slot));
        }
        catch (ArgumentException exception)
        {
            return UnprocessableEntity(new { code = "LaunchPad.Validation", message = exception.Message });
        }
    }

    [HttpPut("slots/{slotId:guid}")]
    public async Task<ActionResult<LaunchPadSlotProjection>> UpdateSlot(
        Guid slotId,
        [FromBody] CreateLaunchPadSlotRequest request,
        CancellationToken cancellationToken)
    {
        var slot = await context.Set<LaunchPadParticipantSlot>().Include(candidate => candidate.LaunchPadEvent)
            .SingleOrDefaultAsync(candidate => candidate.Id == slotId && candidate.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);
        if (slot == null || slot.TenantId != CurrentTenantId()) return NotFound();
        if (!await authorization.CanManageEventsAsync(slot.TenantId!.Value, cancellationToken).ConfigureAwait(false)) return Forbid();
        if (slot.LaunchPadEvent.Status is LaunchPadEventStatus.Active or LaunchPadEventStatus.Completed or LaunchPadEventStatus.Cancelled or LaunchPadEventStatus.Archived)
            return Conflict(new { code = "LaunchPad.InvalidState" });
        if (request.StartsAt < slot.LaunchPadEvent.StartsAt || request.EndsAt > slot.LaunchPadEvent.EndsAt)
            return UnprocessableEntity(new { code = "LaunchPad.SlotOutsideEvent" });
        try
        {
            slot.Update(request.Name, request.Role, request.Capacity, request.StartsAt, request.EndsAt);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Ok(LaunchPadSlotProjection.FromEntity(slot));
        }
        catch (ArgumentException exception)
        {
            return UnprocessableEntity(new { code = "LaunchPad.Validation", message = exception.Message });
        }
    }

    [HttpDelete("slots/{slotId:guid}")]
    public async Task<IActionResult> DeleteSlot(Guid slotId, CancellationToken cancellationToken)
    {
        var slot = await context.Set<LaunchPadParticipantSlot>().Include(candidate => candidate.Registrations)
            .SingleOrDefaultAsync(candidate => candidate.Id == slotId && candidate.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);
        if (slot == null || slot.TenantId != CurrentTenantId()) return NotFound();
        if (!await authorization.CanManageEventsAsync(slot.TenantId!.Value, cancellationToken).ConfigureAwait(false)) return Forbid();
        if (slot.Registrations.Any(registration => registration.DeletedAt == null))
            return Conflict(new { code = "LaunchPad.SlotHasRegistrations" });
        context.Set<LaunchPadParticipantSlot>().Remove(slot);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{eventId:guid}/applications")]
    public async Task<ActionResult<LaunchPadApplicationProjection>> SubmitApplication(
        Guid eventId,
        [FromBody] SubmitLaunchPadApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var launchEvent = await FindEventAsync(eventId, cancellationToken).ConfigureAwait(false);
        if (launchEvent == null || launchEvent.Status != LaunchPadEventStatus.ApplicationsOpen) return Conflict(new { code = "LaunchPad.ApplicationsClosed" });
        var actorId = actors.ActorContext.SubjectIdAsGuid;
        if (!actorId.HasValue || !await authorization.CanParticipateAsync(launchEvent.TenantId!.Value, cancellationToken).ConfigureAwait(false))
            return Unauthorized();
        if (!await authorization.CanSubmitProjectAsync(request.ProjectId, cancellationToken).ConfigureAwait(false)) return NotFound();

        var version = await context.Set<ProjectVersion>().AsNoTracking()
            .FirstOrDefaultAsync(version => version.Id == request.ProjectVersionId && version.ProjectId == request.ProjectId &&
                                 version.Project.TenantId == launchEvent.TenantId && version.DeletedAt == null,
                cancellationToken).ConfigureAwait(false);
        if (version == null) return UnprocessableEntity(new { code = "LaunchPad.ProjectVersionMismatch" });
        var submissionPolicy = await GetVersionSubmissionPolicyAsync(launchEvent.TenantId.Value, cancellationToken).ConfigureAwait(false);
        if (!ProjectVersionEligibility.IsEligible(version.Status, submissionPolicy))
            return UnprocessableEntity(new { code = "LaunchPad.ProjectVersionIneligible" });
        var submittedAssetIds = request.SubmittedAssetReferenceIds?
            .Where(id => id != Guid.Empty).Distinct().Take(100).ToArray() ?? [];
        if (submittedAssetIds.Length > 0)
        {
            var validAssets = await context.Set<AssetReference>().AsNoTracking().CountAsync(reference =>
                submittedAssetIds.Contains(reference.Id) && reference.TenantId == launchEvent.TenantId &&
                reference.ParentResourceId == request.ProjectId &&
                (reference.ParentResourceType == "Project" || reference.ParentResourceType == "Projects") &&
                reference.DeletedAt == null,
                cancellationToken).ConfigureAwait(false);
            if (validAssets != submittedAssetIds.Length)
                return UnprocessableEntity(new { code = "LaunchPad.SubmittedAssetMismatch" });
        }
        var duplicate = await context.Set<LaunchPadApplication>().AnyAsync(application =>
            application.LaunchPadEventId == eventId && application.ProjectId == request.ProjectId && application.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
        if (duplicate) return Conflict(new { code = "LaunchPad.ApplicationExists" });

        var application = LaunchPadApplication.Submit(launchEvent.TenantId.Value, eventId, request.ProjectId,
            request.ProjectVersionId, actorId.Value, request.Pitch, submittedAssetIds, submissionPolicy);
        context.Set<LaunchPadApplication>().Add(application);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Created(string.Empty, LaunchPadApplicationProjection.FromEntity(application));
    }

    [HttpGet("applications/me")]
    public async Task<ActionResult<IReadOnlyList<LaunchPadApplicationProjection>>> GetMyApplications(CancellationToken cancellationToken)
    {
        var tenantId = CurrentTenantId();
        if (!tenantId.HasValue || actors.ActorContext.SubjectIdAsGuid == null) return Unauthorized();
        var projects = context.Set<Project>().Where(project => project.TenantId == tenantId && project.DeletedAt == null);
        var accessibleIds = await projects.Select(project => project.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var allowed = new List<Guid>();
        foreach (var projectId in accessibleIds)
            if (await authorization.CanSubmitProjectAsync(projectId, cancellationToken).ConfigureAwait(false)) allowed.Add(projectId);
        var applications = await context.Set<LaunchPadApplication>().AsNoTracking()
            .Include(application => application.LaunchPadEvent)
            .Where(application => application.TenantId == tenantId && allowed.Contains(application.ProjectId) && application.DeletedAt == null)
            .OrderByDescending(application => application.SubmittedAt)
            .Select(application => LaunchPadApplicationProjection.FromEntity(application))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(applications);
    }

    [HttpPut("applications/{applicationId:guid}")]
    public async Task<ActionResult<LaunchPadApplicationProjection>> UpdateApplication(
        Guid applicationId,
        [FromBody] UpdateLaunchPadApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var application = await context.Set<LaunchPadApplication>().Include(candidate => candidate.LaunchPadEvent)
            .SingleOrDefaultAsync(candidate => candidate.Id == applicationId && candidate.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);
        if (application == null || application.TenantId != CurrentTenantId()) return NotFound();
        if (!await authorization.CanSubmitProjectAsync(application.ProjectId, cancellationToken).ConfigureAwait(false)) return NotFound();
        if (application.LaunchPadEvent.Status != LaunchPadEventStatus.ApplicationsOpen)
            return Conflict(new { code = "LaunchPad.ApplicationsClosed" });
        var replacingVersion = request.ProjectVersionId != application.ProjectVersionId;
        if (replacingVersion && !ProjectVersionEligibility.CanReplaceAfterSubmission(application.SubmissionVersionPolicy))
            return Conflict(new { code = "LaunchPad.ProjectVersionImmutable" });
        var version = await context.Set<ProjectVersion>().AsNoTracking().FirstOrDefaultAsync(version =>
            version.Id == request.ProjectVersionId && version.ProjectId == application.ProjectId &&
            version.Project.TenantId == application.TenantId && version.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
        if (version == null) return UnprocessableEntity(new { code = "LaunchPad.ProjectVersionMismatch" });
        if (replacingVersion)
        {
            var currentPolicy = await GetVersionSubmissionPolicyAsync(application.TenantId!.Value, cancellationToken).ConfigureAwait(false);
            if (!ProjectVersionEligibility.IsEligible(version.Status, currentPolicy))
                return UnprocessableEntity(new { code = "LaunchPad.ProjectVersionIneligible" });
        }
        var assetIds = request.SubmittedAssetReferenceIds?.Where(id => id != Guid.Empty).Distinct().Take(100).ToArray() ?? [];
        var validAssets = await context.Set<AssetReference>().AsNoTracking().CountAsync(reference =>
            assetIds.Contains(reference.Id) && reference.TenantId == application.TenantId &&
            reference.ParentResourceId == application.ProjectId &&
            (reference.ParentResourceType == "Project" || reference.ParentResourceType == "Projects") &&
            reference.DeletedAt == null, cancellationToken).ConfigureAwait(false);
        if (validAssets != assetIds.Length) return UnprocessableEntity(new { code = "LaunchPad.SubmittedAssetMismatch" });
        try { application.Update(request.ProjectVersionId, request.Pitch, assetIds); }
        catch (InvalidOperationException exception) { return Conflict(new { code = "LaunchPad.InvalidState", message = exception.Message }); }
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(LaunchPadApplicationProjection.FromEntity(application));
    }

    [HttpPost("applications/{applicationId:guid}:withdraw")]
    public async Task<ActionResult<LaunchPadApplicationProjection>> WithdrawApplication(Guid applicationId, CancellationToken cancellationToken)
    {
        var application = await context.Set<LaunchPadApplication>()
            .FirstOrDefaultAsync(candidate => candidate.Id == applicationId && candidate.DeletedAt == null, cancellationToken).ConfigureAwait(false);
        if (application == null || application.TenantId != CurrentTenantId()) return NotFound();
        if (!await authorization.CanSubmitProjectAsync(application.ProjectId, cancellationToken).ConfigureAwait(false)) return NotFound();
        try
        {
            application.Withdraw();
            await assetScopedAccessService.RevokeScopeAsync("LaunchPadApplication", application.Id, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException exception) { return Conflict(new { code = "LaunchPad.InvalidState", message = exception.Message }); }
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(LaunchPadApplicationProjection.FromEntity(application));
    }

    [HttpGet("{eventId:guid}/applications/management")]
    public async Task<ActionResult<IReadOnlyList<LaunchPadApplicationProjection>>> GetEventApplications(Guid eventId, CancellationToken cancellationToken)
    {
        var launchEvent = await FindEventAsync(eventId, cancellationToken).ConfigureAwait(false);
        if (launchEvent == null) return NotFound();
        if (!await authorization.CanReviewApplicationsAsync(launchEvent.TenantId!.Value, cancellationToken).ConfigureAwait(false)) return Forbid();
        var applications = await context.Set<LaunchPadApplication>().AsNoTracking()
            .Where(application => application.LaunchPadEventId == eventId && application.TenantId == launchEvent.TenantId && application.DeletedAt == null)
            .OrderBy(application => application.SubmittedAt)
            .Select(application => LaunchPadApplicationProjection.FromEntity(application))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(applications);
    }

    [HttpPost("applications/{applicationId:guid}:review")]
    public async Task<ActionResult<LaunchPadApplicationProjection>> ReviewApplication(
        Guid applicationId,
        [FromBody] ReviewLaunchPadApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var application = await context.Set<LaunchPadApplication>()
            .Include(candidate => candidate.LaunchPadEvent)
            .FirstOrDefaultAsync(candidate => candidate.Id == applicationId && candidate.DeletedAt == null, cancellationToken).ConfigureAwait(false);
        if (application == null || application.TenantId != CurrentTenantId()) return NotFound();
        if (!await authorization.CanReviewApplicationsAsync(application.TenantId!.Value, cancellationToken).ConfigureAwait(false)) return Forbid();
        var reviewerId = actors.ActorContext.SubjectIdAsGuid;
        if (!reviewerId.HasValue) return Unauthorized();
        try
        {
            switch (request.Status)
            {
                case LaunchPadApplicationStatus.UnderReview: application.StartReview(); break;
                case LaunchPadApplicationStatus.Waitlisted: application.Waitlist(reviewerId.Value); break;
                case LaunchPadApplicationStatus.Approved:
                    application.Approve(reviewerId.Value);
                    var exists = await context.Set<LaunchPlan>().AnyAsync(plan => plan.LaunchPadApplicationId == application.Id && plan.DeletedAt == null,
                        cancellationToken).ConfigureAwait(false);
                    if (!exists)
                        context.Set<LaunchPlan>().Add(LaunchPlan.CreateForApprovedApplication(application.TenantId.Value,
                            application.LaunchPadEventId, application.Id, application.ProjectId, application.ProjectVersionId, request.LaunchPlanName ?? "Launch plan"));
                    break;
                case LaunchPadApplicationStatus.Rejected: application.Reject(reviewerId.Value); break;
                default: return UnprocessableEntity(new { code = "LaunchPad.InvalidApplicationStatus" });
            }
            if (request.Status is LaunchPadApplicationStatus.UnderReview or LaunchPadApplicationStatus.Waitlisted &&
                application.SubmittedAssetReferenceIds.Count > 0)
            {
                var expiresAt = application.LaunchPadEvent.EndsAt.AddDays(7);
                if (expiresAt <= SystemClock.UtcNow) expiresAt = SystemClock.UtcNow.AddHours(24);
                await assetScopedAccessService.GrantAsync(
                    application.SubmittedAssetReferenceIds,
                    reviewerId.Value,
                    application.TenantId!.Value,
                    "LaunchPadApplication",
                    application.Id,
                    expiresAt,
                    reviewerId.Value,
                    cancellationToken).ConfigureAwait(false);
            }
            else if (request.Status is LaunchPadApplicationStatus.Approved or LaunchPadApplicationStatus.Rejected)
            {
                await assetScopedAccessService.RevokeScopeAsync("LaunchPadApplication", application.Id, cancellationToken).ConfigureAwait(false);
            }
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Ok(LaunchPadApplicationProjection.FromEntity(application));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { code = "LaunchPad.InvalidState", message = exception.Message });
        }
    }

    [HttpPost("slots/{slotId:guid}/registrations")]
    public async Task<ActionResult<LaunchPadRegistrationProjection>> Register(Guid slotId, CancellationToken cancellationToken)
    {
        var slot = await context.Set<LaunchPadParticipantSlot>()
            .Include(candidate => candidate.LaunchPadEvent)
            .FirstOrDefaultAsync(candidate => candidate.Id == slotId && candidate.DeletedAt == null, cancellationToken).ConfigureAwait(false);
        if (slot == null || slot.TenantId != CurrentTenantId() ||
            slot.LaunchPadEvent.Status is LaunchPadEventStatus.Draft or LaunchPadEventStatus.Completed or LaunchPadEventStatus.Cancelled or LaunchPadEventStatus.Archived)
            return NotFound();
        var actorId = actors.ActorContext.SubjectIdAsGuid;
        if (!actorId.HasValue || !await authorization.CanParticipateAsync(slot.TenantId!.Value, cancellationToken).ConfigureAwait(false)) return Unauthorized();
        var existing = await context.Set<LaunchPadParticipantRegistration>().AnyAsync(registration =>
            registration.LaunchPadParticipantSlotId == slotId && registration.UserId == actorId && registration.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
        if (existing) return Conflict(new { code = "LaunchPad.AlreadyRegistered" });
        var waitlisted = !slot.HasCapacity;
        if (!waitlisted) slot.Reserve();
        var registration = LaunchPadParticipantRegistration.Register(slot.TenantId.Value, slot.Id, actorId.Value, waitlisted);
        context.Set<LaunchPadParticipantRegistration>().Add(registration);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Created(string.Empty, LaunchPadRegistrationProjection.FromEntity(registration));
    }

    [HttpGet("registrations/me")]
    public async Task<ActionResult<IReadOnlyList<LaunchPadRegistrationProjection>>> GetMyRegistrations(CancellationToken cancellationToken)
    {
        var actorId = actors.ActorContext.SubjectIdAsGuid;
        var tenantId = CurrentTenantId();
        if (!actorId.HasValue || !tenantId.HasValue) return Unauthorized();
        var registrations = await context.Set<LaunchPadParticipantRegistration>().AsNoTracking()
            .Where(registration => registration.UserId == actorId && registration.TenantId == tenantId && registration.DeletedAt == null)
            .OrderByDescending(registration => registration.RegisteredAt)
            .Select(registration => LaunchPadRegistrationProjection.FromEntity(registration))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(registrations);
    }

    [HttpPost("registrations/{registrationId:guid}:cancel")]
    public async Task<ActionResult<LaunchPadRegistrationProjection>> CancelRegistration(Guid registrationId, CancellationToken cancellationToken)
    {
        var registration = await context.Set<LaunchPadParticipantRegistration>()
            .Include(candidate => candidate.LaunchPadParticipantSlot)
            .FirstOrDefaultAsync(candidate => candidate.Id == registrationId && candidate.DeletedAt == null, cancellationToken).ConfigureAwait(false);
        if (registration == null || registration.TenantId != CurrentTenantId()) return NotFound();
        var actorId = actors.ActorContext.SubjectIdAsGuid;
        if (registration.UserId != actorId &&
            !await authorization.CanManageParticipantsAsync(registration.TenantId!.Value, cancellationToken).ConfigureAwait(false)) return Forbid();
        var reserved = registration.Status == LaunchPadParticipantStatus.Registered;
        try { registration.Cancel(); }
        catch (InvalidOperationException exception) { return Conflict(new { code = "LaunchPad.InvalidState", message = exception.Message }); }
        if (reserved)
        {
            registration.LaunchPadParticipantSlot.Release();
            await PromoteOldestWaitlistedAsync(registration.LaunchPadParticipantSlot, cancellationToken).ConfigureAwait(false);
        }
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(LaunchPadRegistrationProjection.FromEntity(registration));
    }

    [HttpPost("registrations/{registrationId:guid}:transition")]
    public async Task<ActionResult<LaunchPadRegistrationProjection>> TransitionRegistration(
        Guid registrationId,
        [FromBody] TransitionLaunchPadRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var registration = await context.Set<LaunchPadParticipantRegistration>()
            .Include(candidate => candidate.LaunchPadParticipantSlot)
            .FirstOrDefaultAsync(candidate => candidate.Id == registrationId && candidate.DeletedAt == null, cancellationToken).ConfigureAwait(false);
        if (registration == null || registration.TenantId != CurrentTenantId()) return NotFound();
        if (!await authorization.CanManageParticipantsAsync(registration.TenantId!.Value, cancellationToken).ConfigureAwait(false)) return Forbid();
        try
        {
            switch (request.Status)
            {
                case LaunchPadParticipantStatus.Registered:
                    registration.LaunchPadParticipantSlot.Reserve(); registration.Promote(); break;
                case LaunchPadParticipantStatus.CheckedIn: registration.CheckIn(); break;
                case LaunchPadParticipantStatus.Attended: registration.MarkAttended(); break;
                case LaunchPadParticipantStatus.Completed: registration.Complete(); break;
                case LaunchPadParticipantStatus.NoShow: registration.MarkNoShow(); break;
                case LaunchPadParticipantStatus.Cancelled:
                    var reserved = registration.Status == LaunchPadParticipantStatus.Registered;
                    registration.Cancel();
                    if (reserved)
                    {
                        registration.LaunchPadParticipantSlot.Release();
                        await PromoteOldestWaitlistedAsync(registration.LaunchPadParticipantSlot, cancellationToken).ConfigureAwait(false);
                    }
                    break;
                default: return UnprocessableEntity(new { code = "LaunchPad.InvalidParticipantStatus" });
            }
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Ok(LaunchPadRegistrationProjection.FromEntity(registration));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { code = "LaunchPad.InvalidState", message = exception.Message });
        }
    }

    [HttpGet("{eventId:guid}/registrations/management")]
    public async Task<ActionResult<IReadOnlyList<LaunchPadRegistrationProjection>>> GetEventRegistrations(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var launchEvent = await FindEventAsync(eventId, cancellationToken).ConfigureAwait(false);
        if (launchEvent == null) return NotFound();
        if (!await authorization.CanManageParticipantsAsync(launchEvent.TenantId!.Value, cancellationToken).ConfigureAwait(false)) return Forbid();
        var registrations = await context.Set<LaunchPadParticipantRegistration>().AsNoTracking()
            .Where(registration => registration.LaunchPadParticipantSlot.LaunchPadEventId == eventId &&
                                   registration.TenantId == launchEvent.TenantId && registration.DeletedAt == null)
            .OrderBy(registration => registration.RegisteredAt)
            .Select(registration => LaunchPadRegistrationProjection.FromEntity(registration))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(registrations);
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<LaunchPadAnalyticsProjection>> GetAnalytics(CancellationToken cancellationToken)
    {
        var tenantId = CurrentTenantId();
        if (!tenantId.HasValue) return Unauthorized();
        if (!await authorization.CanViewAnalyticsAsync(tenantId.Value, cancellationToken).ConfigureAwait(false)) return Forbid();
        var events = context.Set<LaunchPadEvent>().AsNoTracking().Where(entity => entity.TenantId == tenantId && entity.DeletedAt == null);
        var applications = context.Set<LaunchPadApplication>().AsNoTracking().Where(entity => entity.TenantId == tenantId && entity.DeletedAt == null);
        var registrations = context.Set<LaunchPadParticipantRegistration>().AsNoTracking().Where(entity => entity.TenantId == tenantId && entity.DeletedAt == null);
        var result = new LaunchPadAnalyticsProjection(
            await events.CountAsync(cancellationToken).ConfigureAwait(false),
            await events.CountAsync(entity => entity.Status == LaunchPadEventStatus.Completed, cancellationToken).ConfigureAwait(false),
            await applications.CountAsync(cancellationToken).ConfigureAwait(false),
            await applications.CountAsync(entity => entity.Status == LaunchPadApplicationStatus.Approved, cancellationToken).ConfigureAwait(false),
            await registrations.CountAsync(cancellationToken).ConfigureAwait(false),
            await registrations.CountAsync(entity => entity.Status == LaunchPadParticipantStatus.Completed, cancellationToken).ConfigureAwait(false));
        return Ok(result);
    }

    private Guid? CurrentTenantId() => requestContext.CurrentTenantId ?? actors.ActorContext.TenantId;

    private async Task<VersionSubmissionPolicy> GetVersionSubmissionPolicyAsync(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        await context.Set<LaunchPadSettings>().AsNoTracking()
            .Where(settings => settings.TenantId == tenantId && settings.DeletedAt == null)
            .Select(settings => (VersionSubmissionPolicy?)settings.VersionSubmissionPolicy)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
        ?? VersionSubmissionPolicy.ReleasedImmutable;

    private async Task<LaunchPadEvent?> FindEventAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = CurrentTenantId();
        if (!tenantId.HasValue) return null;
        return await context.Set<LaunchPadEvent>().FirstOrDefaultAsync(entity => entity.Id == id && entity.TenantId == tenantId && entity.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task PromoteOldestWaitlistedAsync(LaunchPadParticipantSlot slot, CancellationToken cancellationToken)
    {
        if (!slot.HasCapacity) return;
        var next = await context.Set<LaunchPadParticipantRegistration>()
            .Where(registration => registration.LaunchPadParticipantSlotId == slot.Id &&
                                   registration.Status == LaunchPadParticipantStatus.Waitlisted &&
                                   registration.DeletedAt == null)
            .OrderBy(registration => registration.RegisteredAt)
            .ThenBy(registration => registration.Id)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (next == null) return;
        slot.Reserve();
        next.Promote();
    }
}

public sealed record CreateLaunchPadEventRequest(
    string Name, string? Description, DateTime StartsAt, DateTime EndsAt,
    DateTime? ApplicationsOpenAt = null, DateTime? ApplicationsCloseAt = null);
public sealed record TransitionLaunchPadEventRequest(LaunchPadEventStatus Status);
public sealed record UpdateLaunchPadEventRequest(
    string Name, string? Description, DateTime StartsAt, DateTime EndsAt,
    DateTime? ApplicationsOpenAt = null, DateTime? ApplicationsCloseAt = null);
public sealed record CreateLaunchPadSlotRequest(string Name, LaunchPadParticipantRole Role, int Capacity, DateTime StartsAt, DateTime EndsAt);
public sealed record SubmitLaunchPadApplicationRequest(
    Guid ProjectId,
    Guid ProjectVersionId,
    string? Pitch,
    IReadOnlyList<Guid>? SubmittedAssetReferenceIds = null);
public sealed record UpdateLaunchPadApplicationRequest(
    Guid ProjectVersionId,
    string? Pitch,
    IReadOnlyList<Guid>? SubmittedAssetReferenceIds = null);
public sealed record ReviewLaunchPadApplicationRequest(LaunchPadApplicationStatus Status, string? LaunchPlanName = null);
public sealed record TransitionLaunchPadRegistrationRequest(LaunchPadParticipantStatus Status);

public sealed record LaunchPadEventProjection(Guid Id, string Name, string? Description, DateTime StartsAt, DateTime EndsAt,
    LaunchPadEventStatus Status, DateTime? ApplicationsOpenAt, DateTime? ApplicationsCloseAt)
{
    public static LaunchPadEventProjection FromEntity(LaunchPadEvent entity) => new(entity.Id, entity.Name, entity.Description,
        entity.StartsAt, entity.EndsAt, entity.Status, entity.ApplicationsOpenAt, entity.ApplicationsCloseAt);
}

public sealed record LaunchPadEventDetailProjection(LaunchPadEventProjection Event, IReadOnlyList<LaunchPadSlotProjection> Slots)
{
    public static LaunchPadEventDetailProjection FromEntity(LaunchPadEvent entity) => new(
        LaunchPadEventProjection.FromEntity(entity), entity.Slots.Select(LaunchPadSlotProjection.FromEntity).ToList());
}

public sealed record LaunchPadSlotProjection(Guid Id, Guid EventId, string Name, LaunchPadParticipantRole Role,
    int Capacity, int ReservedCount, DateTime StartsAt, DateTime EndsAt)
{
    public static LaunchPadSlotProjection FromEntity(LaunchPadParticipantSlot entity) => new(entity.Id, entity.LaunchPadEventId,
        entity.Name, entity.Role, entity.Capacity, entity.ReservedCount, entity.StartsAt, entity.EndsAt);
}

public sealed record LaunchPadApplicationProjection(Guid Id, Guid EventId, Guid ProjectId, Guid ProjectVersionId,
    Guid SubmittedByUserId, LaunchPadApplicationStatus Status, string? Pitch, DateTime SubmittedAt,
    IReadOnlyList<Guid> SubmittedAssetReferenceIds,
    VersionSubmissionPolicy SubmissionVersionPolicy)
{
    public static LaunchPadApplicationProjection FromEntity(LaunchPadApplication entity) => new(entity.Id, entity.LaunchPadEventId,
        entity.ProjectId, entity.ProjectVersionId, entity.SubmittedByUserId, entity.Status, entity.Pitch, entity.SubmittedAt,
        entity.SubmittedAssetReferenceIds, entity.SubmissionVersionPolicy);
}

public sealed record LaunchPadRegistrationProjection(Guid Id, Guid SlotId, Guid UserId, LaunchPadParticipantStatus Status,
    DateTime RegisteredAt, DateTime? CheckedInAt, DateTime? CompletedAt)
{
    public static LaunchPadRegistrationProjection FromEntity(LaunchPadParticipantRegistration entity) => new(entity.Id,
        entity.LaunchPadParticipantSlotId, entity.UserId, entity.Status, entity.RegisteredAt, entity.CheckedInAt, entity.CompletedAt);
}

public sealed record LaunchPadAnalyticsProjection(
    int Events, int CompletedEvents, int Applications, int ApprovedApplications, int Registrations, int CompletedRegistrations);
