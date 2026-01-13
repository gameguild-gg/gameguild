namespace GameGuild.Identity.Context.Actors;

/// <summary>
///     Represents the system itself acting on behalf of background jobs, schedulers, or internal processes.
/// </summary>
/// <param name="OperationName">The name of the system operation or job being performed.</param>
/// <param name="CorrelationId">Optional correlation ID for tracing the operation.</param>
public sealed record SystemActor(
    string OperationName,
    string? CorrelationId = null
) : IActor
{
    /// <summary>
    ///     Well-known subject ID for system actors.
    ///     This is the compile-time constant used in attributes.
    /// </summary>
    public const string SystemSubjectIdConstant = "system";

    /// <summary>
    ///     Well-known subject ID for system actors (runtime alias).
    /// </summary>
    public const string SystemSubjectId = SystemSubjectIdConstant;

    /// <summary>
    ///     Creates a new system actor for background job operations.
    /// </summary>
    public static SystemActor ForBackgroundJob(string jobName, string? correlationId = null)
        => new($"BackgroundJob:{jobName}", correlationId);

    /// <summary>
    ///     Creates a new system actor for scheduler operations.
    /// </summary>
    public static SystemActor ForScheduler(string schedulerName, string? correlationId = null)
        => new($"Scheduler:{schedulerName}", correlationId);

    /// <summary>
    ///     Creates a new system actor for migration operations.
    /// </summary>
    public static SystemActor ForMigration(string migrationName, string? correlationId = null)
        => new($"Migration:{migrationName}", correlationId);

    /// <summary>
    ///     Creates a new system actor for seeding operations.
    /// </summary>
    public static SystemActor ForSeeding(string? correlationId = null)
        => new("Seeding", correlationId);

    /// <inheritdoc />
    public ActorKind Kind => ActorKind.System;

    /// <inheritdoc />
    public string SubjectId => SystemSubjectId;

    /// <inheritdoc />
    public string DisplayName => OperationName;
}
