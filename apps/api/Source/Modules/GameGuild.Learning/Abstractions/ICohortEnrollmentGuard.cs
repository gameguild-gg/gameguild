namespace GameGuild.Learning.Abstractions;

/// <summary>
///     Capacity-side port for cohort-gated enrollment (issue #1226 N17): the Enrollments
///     module consults it before storing a <c>CohortId</c> on a new enrollment, and after
///     an enrolled learner drops or expires. The Cohorts module supplies the
///     implementation, keeping Enrollments independent from cohort persistence — the same
///     dependency inversion <c>ICourseEnrollmentAccessReader</c> uses for the
///     Courses&#8594;Enrollments direction.
/// </summary>
public interface ICohortEnrollmentGuard
{
    /// <summary>
    ///     Verifies the cohort exists and <c>Cohort.CanEnroll()</c> accepts the learner
    ///     (open, active, spare capacity), then reserves one seat by incrementing the
    ///     cohort's current enrollment count. The increment is staged on the shared scoped
    ///     <c>DbContext</c>: the caller's <c>SaveChanges</c> persists it atomically with
    ///     the enrollment row, and the entity's optimistic-concurrency version turns two
    ///     concurrent racers for the last seat into a <c>DbUpdateConcurrencyException</c>
    ///     for exactly one of them (the caller maps that to a capacity conflict).
    ///     <para>
    ///         The caller also passes the course ID of the enrollment being created; a
    ///         cohort whose own course differs refuses the reservation, so an enrollment
    ///         can never consume another course's seats.
    ///     </para>
    /// </summary>
    Task<Result> ReserveCapacityAsync(Guid cohortId, Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Releases a previously reserved seat (drop/expire path) by decrementing the
    ///     cohort's current enrollment count. Like the reservation, the decrement is
    ///     staged on the shared context and persisted by the caller's save.
    /// </summary>
    Task<Result> ReleaseCapacityAsync(Guid cohortId, CancellationToken cancellationToken = default);
}
