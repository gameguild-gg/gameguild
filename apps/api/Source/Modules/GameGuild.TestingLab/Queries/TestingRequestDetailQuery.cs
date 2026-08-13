using System.Linq.Expressions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;

namespace GameGuild.TestingLab;

/// <summary>
/// Stable read model for a Testing Lab project request.
/// It intentionally omits EF navigation collections so an optional project version
/// cannot make the API response unparseable for the dashboard client.
/// </summary>
public sealed record TestingRequestProjectProjection(Guid Id, string Title, string Slug);

public sealed record TestingRequestProjectVersionProjection(
    Guid Id,
    Guid ProjectId,
    string VersionNumber,
    string Status,
    TestingRequestProjectProjection? Project);

public sealed record TestingRequestDetailProjection(
    Guid Id,
    string Title,
    string? Description,
    string? DownloadUrl,
    string? InstructionsContent,
    string? FeedbackFormContent,
    int? MaxTesters,
    int CurrentTesterCount,
    DateTime StartDate,
    DateTime EndDate,
    TestingRequestStatus Status,
    Guid? ProjectVersionId,
    TestingRequestProjectVersionProjection? ProjectVersion,
    bool IsDeleted)
{
    public static TestingRequestDetailProjection FromEntity(TestingRequest testingRequest)
        => new(
            testingRequest.Id,
            testingRequest.Title,
            testingRequest.Description,
            testingRequest.DownloadUrl,
            testingRequest.InstructionsContent,
            testingRequest.FeedbackFormContent,
            testingRequest.MaxTesters,
            testingRequest.CurrentTesterCount,
            testingRequest.StartDate,
            testingRequest.EndDate,
            testingRequest.Status,
            testingRequest.ProjectVersionId,
            testingRequest.ProjectVersion == null
                ? null
                : new TestingRequestProjectVersionProjection(
                    testingRequest.ProjectVersion.Id,
                    testingRequest.ProjectVersion.ProjectId,
                    testingRequest.ProjectVersion.VersionNumber,
                    testingRequest.ProjectVersion.Status,
                    testingRequest.ProjectVersion.Project == null
                        ? null
                        : new TestingRequestProjectProjection(
                            testingRequest.ProjectVersion.Project.Id,
                            testingRequest.ProjectVersion.Project.Title,
                            testingRequest.ProjectVersion.Project.Slug)),
            testingRequest.DeletedAt != null);
}

public sealed record GetTestingRequestDetailQuery(Guid RequestId)
    : IQuery<Result<TestingRequestDetailProjection>>;

public sealed class TestingRequestDetailQueryHandler(
    IApplicationDbContext context,
    IActorContextAccessor actorContextAccessor)
    : IQueryHandler<GetTestingRequestDetailQuery, Result<TestingRequestDetailProjection>>
{
    public async Task<Result<TestingRequestDetailProjection>> Handle(
        GetTestingRequestDetailQuery request,
        CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        var userId = actor.SubjectIdAsGuid;
        if (!actor.IsAuthenticated || userId == null || actor.TenantId == null)
        {
            return Result.Failure<TestingRequestDetailProjection>(
                Error.Unauthorized("TestingLab.Unauthenticated", "An authenticated tenant actor is required."));
        }

        var hasAccess = await TestingLabActorAccess.IsActiveTenantActorAsync(context, actor, cancellationToken).ConfigureAwait(false);
        if (!hasAccess)
        {
            return Result.Failure<TestingRequestDetailProjection>(
                Error.Unauthorized("TestingLab.InactiveActor", "An active tenant membership is required."));
        }

        var projection = await context.Set<TestingRequest>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(testingRequest =>
                testingRequest.Id == request.RequestId &&
                testingRequest.TenantId == actor.TenantId.Value)
            .Select(Projection)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return projection == null
            ? Result.Failure<TestingRequestDetailProjection>(
                Error.NotFound("TestingLab.RequestNotFound", "Testing request not found."))
            : Result.Success(projection);
    }

    private static readonly Expression<Func<TestingRequest, TestingRequestDetailProjection>> Projection = testingRequest => new(
        testingRequest.Id,
        testingRequest.Title,
        testingRequest.Description,
        testingRequest.DownloadUrl,
        testingRequest.InstructionsContent,
        testingRequest.FeedbackFormContent,
        testingRequest.MaxTesters,
        testingRequest.CurrentTesterCount,
        testingRequest.StartDate,
        testingRequest.EndDate,
        testingRequest.Status,
        testingRequest.ProjectVersionId,
        testingRequest.ProjectVersion == null
            ? null
            : new TestingRequestProjectVersionProjection(
                testingRequest.ProjectVersion.Id,
                testingRequest.ProjectVersion.ProjectId,
                testingRequest.ProjectVersion.VersionNumber,
                testingRequest.ProjectVersion.Status,
                testingRequest.ProjectVersion.Project == null
                    ? null
                    : new TestingRequestProjectProjection(
                        testingRequest.ProjectVersion.Project.Id,
                        testingRequest.ProjectVersion.Project.Title,
                        testingRequest.ProjectVersion.Project.Slug)),
        testingRequest.DeletedAt != null);
}
