using GameGuild.Assets;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Assessments;

/// <summary>
/// Authorizes files attached to one concrete assessment submission.
/// </summary>
public sealed class AssessmentSubmissionAssetAuthorizationResolver(
    IApplicationDbContext context,
    IActorContextAccessor actorContextAccessor,
    IPermissionQueryService permissionQueryService) : IAssetParentAuthorizationResolver
{
    public bool Supports(string resourceType) =>
        resourceType.Equals(nameof(AssessmentSubmission), StringComparison.OrdinalIgnoreCase) ||
        resourceType.Equals("submissions", StringComparison.OrdinalIgnoreCase);

    public async Task<bool> CanReadAsync(
        Guid parentResourceId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        if (!HasAuthoritativeActor(userId, tenantId)) return false;

        var authorization = await ResolveAsync(parentResourceId, tenantId!.Value, cancellationToken)
            .ConfigureAwait(false);
        if (authorization is null) return false;
        if (authorization.OwnerId == userId) return true;

        return await HasAnyPermissionAsync(
            authorization.ProgramId,
            userId,
            tenantId.Value,
            cancellationToken,
            "Review", "Edit", "Publish").ConfigureAwait(false);
    }

    public async Task<bool> CanManageAsync(
        Guid parentResourceId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        if (!HasAuthoritativeActor(userId, tenantId)) return false;

        var authorization = await ResolveAsync(parentResourceId, tenantId!.Value, cancellationToken)
            .ConfigureAwait(false);
        return authorization is { } value &&
               value.OwnerId == userId &&
               value.Status == SubmissionStatus.InProgress;
    }

    private bool HasAuthoritativeActor(Guid userId, Guid? tenantId)
    {
        var actor = actorContextAccessor.ActorContext;
        return actor.IsAuthenticated &&
               actor.SubjectIdAsGuid == userId &&
               tenantId.HasValue &&
               actor.TenantId == tenantId;
    }

    private Task<SubmissionAuthorization?> ResolveAsync(
        Guid submissionId,
        Guid tenantId,
        CancellationToken cancellationToken) =>
        (from submission in context.Set<AssessmentSubmission>().AsNoTracking()
         join assessment in context.Set<Assessment>().AsNoTracking()
             on submission.AssessmentId equals assessment.Id
         join program in context.Set<Program>().AsNoTracking()
             on assessment.CourseId equals program.Id
         where submission.Id == submissionId &&
               submission.DeletedAt == null &&
               assessment.DeletedAt == null &&
               program.DeletedAt == null &&
               submission.TenantId == tenantId &&
               assessment.TenantId == tenantId &&
               program.TenantId == tenantId
         select new SubmissionAuthorization(submission.UserId, submission.Status, program.Id))
        .SingleOrDefaultAsync(cancellationToken);

    private async Task<bool> HasAnyPermissionAsync(
        Guid programId,
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken,
        params string[] permissions)
    {
        foreach (var permission in permissions)
        {
            if (await permissionQueryService.HasTenantPermissionAsync(
                    userId,
                    tenantId,
                    $"Program.{programId}.{permission}",
                    cancellationToken)
                .ConfigureAwait(false)) return true;
        }

        return false;
    }

    private sealed record SubmissionAuthorization(Guid OwnerId, SubmissionStatus Status, Guid ProgramId);
}
