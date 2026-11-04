using GameGuild.Learning.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Cohorts;

/// <summary>
///     Cohorts-module implementation of <see cref="ICohortEnrollmentGuard"/> (issue
///     #1226 N17): gates cohort-tagged enrollment on <see cref="Cohort.CanEnroll"/> and
///     keeps the cohort's <c>CurrentEnrollmentCount</c> in step with the enrollment rows.
///     Mutations are staged on the shared scoped <c>DbContext</c> — the Enrollments
///     module's save persists them in the same transaction as the enrollment row, and the
///     cohort's optimistic-concurrency version (bumped by the application DbContext on
///     every modified save) makes two concurrent racers for the last seat fail with
///     <c>DbUpdateConcurrencyException</c> for exactly one of them.
/// </summary>
public sealed class CohortEnrollmentGuard(IApplicationDbContext context) : ICohortEnrollmentGuard
{
    public async Task<Result> ReserveCapacityAsync(Guid cohortId, CancellationToken cancellationToken = default)
    {
        var cohort = await context.Set<Cohort>()
            .FirstOrDefaultAsync(candidate => candidate.Id == cohortId, cancellationToken)
            .ConfigureAwait(false);

        if (cohort is null)
        {
            return Result.Failure(Error.NotFound("Cohort", "Cohort not found"));
        }

        if (!cohort.CanEnroll())
        {
            var reason = cohort.Status != CohortStatus.Active || !cohort.IsOpen
                ? "Cohort is not open for enrollment"
                : "Cohort is at capacity";
            return Result.Failure(Error.Conflict("Cohort", reason));
        }

        // Stage the reservation; the caller's SaveChanges persists it atomically with the
        // enrollment row (and the version bump trips on a concurrent capacity writer).
        cohort.IncrementEnrollment();
        return Result.Success();
    }

    public async Task<Result> ReleaseCapacityAsync(Guid cohortId, CancellationToken cancellationToken = default)
    {
        var cohort = await context.Set<Cohort>()
            .FirstOrDefaultAsync(candidate => candidate.Id == cohortId, cancellationToken)
            .ConfigureAwait(false);

        if (cohort is null)
        {
            return Result.Failure(Error.NotFound("Cohort", "Cohort not found"));
        }

        // Stage the release; the caller's SaveChanges persists it with the status change.
        cohort.DecrementEnrollment();
        return Result.Success();
    }
}
