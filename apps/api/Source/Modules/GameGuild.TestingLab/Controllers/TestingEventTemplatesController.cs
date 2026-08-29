using Asp.Versioning;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.TestingLab;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}/testing/templates")]
[Authorize]
public sealed class TestingEventTemplatesController(
    IApplicationDbContext context,
    IActorContextAccessor actors) : ControllerBase
{
    [HttpGet]
    [RequireTestingLabPermission(TestingLabActions.Read, TestingLabResourceTypes.Settings)]
    public async Task<ActionResult<IReadOnlyList<TestingEventTemplateProjection>>> GetTemplates(
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var actor = actors.ActorContext;
        if (!await IsActiveActorAsync(actor, cancellationToken).ConfigureAwait(false)) return Unauthorized();
        var query = context.Set<TestingEventTemplate>().AsNoTracking()
            .Include(template => template.Revisions)
            .Where(template => template.TenantId == actor.TenantId && template.DeletedAt == null);
        if (!includeArchived) query = query.Where(template => template.ArchivedAt == null);
        var templates = await query.OrderBy(template => template.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(templates.Select(TestingEventTemplateProjection.FromEntity).ToArray());
    }

    [HttpGet("{templateId:guid}/revisions/{revisionId:guid}")]
    [RequireTestingLabPermission(TestingLabActions.Read, TestingLabResourceTypes.Settings)]
    public async Task<ActionResult<TestingEventTemplateRevisionProjection>> GetRevision(
        Guid templateId,
        Guid revisionId,
        CancellationToken cancellationToken = default)
    {
        var actor = actors.ActorContext;
        if (!await IsActiveActorAsync(actor, cancellationToken).ConfigureAwait(false)) return Unauthorized();
        var revision = await context.Set<TestingEventTemplateRevision>().AsNoTracking()
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == revisionId && candidate.TemplateId == templateId &&
                candidate.TenantId == actor.TenantId && candidate.DeletedAt == null,
                cancellationToken).ConfigureAwait(false);
        return revision == null ? NotFound() : Ok(TestingEventTemplateRevisionProjection.FromEntity(revision));
    }

    [HttpPost]
    [RequireTestingLabPermission(TestingLabActions.Edit, TestingLabResourceTypes.Settings)]
    public async Task<ActionResult<TestingEventTemplateProjection>> CreateTemplate(
        [FromBody] UpsertTestingEventTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = actors.ActorContext;
        if (!await IsActiveActorAsync(actor, cancellationToken).ConfigureAwait(false)) return Unauthorized();
        try
        {
            var template = TestingEventTemplate.Create(
                actor.TenantId!.Value,
                request.Name,
                request.GeneralRules,
                request.CandidateInstructions,
                request.TesterInstructions,
                request.ProjectApplicationSchema,
                request.TesterRegistrationSchema,
                request.DefaultMode,
                request.DefaultApprovalMode,
                request.DefaultRequiresFeedback,
                actor.SubjectIdAsGuid!.Value,
                request.Description);
            context.Set<TestingEventTemplate>().Add(template);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetRevision),
                new { templateId = template.Id, revisionId = template.CurrentRevision.Id },
                TestingEventTemplateProjection.FromEntity(template));
        }
        catch (ArgumentException exception)
        {
            return UnprocessableEntity(new { code = "TestingLab.InvalidTemplate", message = exception.Message });
        }
    }

    [HttpPut("{templateId:guid}")]
    [RequireTestingLabPermission(TestingLabActions.Edit, TestingLabResourceTypes.Settings)]
    public async Task<ActionResult<TestingEventTemplateProjection>> CreateRevision(
        Guid templateId,
        [FromBody] UpsertTestingEventTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = actors.ActorContext;
        if (!await IsActiveActorAsync(actor, cancellationToken).ConfigureAwait(false)) return Unauthorized();
        var template = await context.Set<TestingEventTemplate>()
            .Include(candidate => candidate.Revisions)
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == templateId && candidate.TenantId == actor.TenantId && candidate.DeletedAt == null,
                cancellationToken).ConfigureAwait(false);
        if (template == null) return NotFound();
        try
        {
            template.CreateRevision(
                request.GeneralRules,
                request.CandidateInstructions,
                request.TesterInstructions,
                request.ProjectApplicationSchema,
                request.TesterRegistrationSchema,
                request.DefaultMode,
                request.DefaultApprovalMode,
                request.DefaultRequiresFeedback,
                actor.SubjectIdAsGuid!.Value,
                request.Name,
                request.Description);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Ok(TestingEventTemplateProjection.FromEntity(template));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { code = "TestingLab.TemplateArchived", message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return UnprocessableEntity(new { code = "TestingLab.InvalidTemplate", message = exception.Message });
        }
    }

    [HttpPost("{templateId:guid}:archive")]
    [RequireTestingLabPermission(TestingLabActions.Edit, TestingLabResourceTypes.Settings)]
    public Task<ActionResult<TestingEventTemplateProjection>> ArchiveTemplate(
        Guid templateId,
        CancellationToken cancellationToken = default) => SetArchivedAsync(templateId, true, cancellationToken);

    [HttpPost("{templateId:guid}:restore")]
    [RequireTestingLabPermission(TestingLabActions.Edit, TestingLabResourceTypes.Settings)]
    public Task<ActionResult<TestingEventTemplateProjection>> RestoreTemplate(
        Guid templateId,
        CancellationToken cancellationToken = default) => SetArchivedAsync(templateId, false, cancellationToken);

    private async Task<ActionResult<TestingEventTemplateProjection>> SetArchivedAsync(
        Guid templateId,
        bool archived,
        CancellationToken cancellationToken)
    {
        var actor = actors.ActorContext;
        if (!await IsActiveActorAsync(actor, cancellationToken).ConfigureAwait(false)) return Unauthorized();
        var template = await context.Set<TestingEventTemplate>()
            .Include(candidate => candidate.Revisions)
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == templateId && candidate.TenantId == actor.TenantId && candidate.DeletedAt == null,
                cancellationToken).ConfigureAwait(false);
        if (template == null) return NotFound();
        if (archived) template.Archive(); else template.RestoreArchivedTemplate();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(TestingEventTemplateProjection.FromEntity(template));
    }

    private Task<bool> IsActiveActorAsync(ActorContext actor, CancellationToken cancellationToken) =>
        TestingLabActorAccess.IsActiveTenantActorAsync(context, actor, cancellationToken);
}
public sealed record UpsertTestingEventTemplateRequest(
    string Name,
    string? Description,
    string GeneralRules,
    string CandidateInstructions,
    string TesterInstructions,
    QuestionnaireSchema ProjectApplicationSchema,
    QuestionnaireSchema TesterRegistrationSchema,
    TestingEventMode DefaultMode,
    TestingEventApprovalMode DefaultApprovalMode,
    bool DefaultRequiresFeedback);

public sealed record TestingEventTemplateProjection(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    bool IsArchived,
    int CurrentRevisionNumber,
    TestingEventTemplateRevisionProjection CurrentRevision)
{
    public static TestingEventTemplateProjection FromEntity(TestingEventTemplate entity) => new(
        entity.Id,
        entity.TenantId!.Value,
        entity.Name,
        entity.Description,
        entity.ArchivedAt.HasValue,
        entity.CurrentRevisionNumber,
        TestingEventTemplateRevisionProjection.FromEntity(entity.CurrentRevision));
}

public sealed record TestingEventTemplateRevisionProjection(
    Guid Id,
    Guid TemplateId,
    int RevisionNumber,
    string GeneralRules,
    string CandidateInstructions,
    string TesterInstructions,
    QuestionnaireSchema ProjectApplicationSchema,
    QuestionnaireSchema TesterRegistrationSchema,
    TestingEventMode DefaultMode,
    TestingEventApprovalMode DefaultApprovalMode,
    bool DefaultRequiresFeedback,
    Guid CreatedByUserId,
    DateTime CreatedAt)
{
    public static TestingEventTemplateRevisionProjection FromEntity(TestingEventTemplateRevision entity) => new(
        entity.Id,
        entity.TemplateId,
        entity.RevisionNumber,
        entity.GeneralRules,
        entity.CandidateInstructions,
        entity.TesterInstructions,
        entity.ProjectApplicationSchema,
        entity.TesterRegistrationSchema,
        entity.DefaultMode,
        entity.DefaultApprovalMode,
        entity.DefaultRequiresFeedback,
        entity.CreatedByUserId,
        entity.CreatedAt);
}
