namespace GameGuild.SharedKernel.Configuration;

/// <summary>
///     Configuration options for background services.
/// </summary>
public class BackgroundServiceOptions : BaseOptions
{
    /// <summary>
    ///     Whether background services are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Maximum number of concurrent background tasks.
    /// </summary>
    public int MaxConcurrentTasks { get; set; } = Environment.ProcessorCount;

    /// <summary>
    ///     Task timeout in seconds.
    /// </summary>
    public int TaskTimeoutSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    ///     Validates the background service options.
    /// </summary>
    public override void Validate()
    {
        base.Validate();

        if (MaxConcurrentTasks <= 0) throw new InvalidOperationException("Max concurrent tasks must be greater than zero.");

        if (TaskTimeoutSeconds <= 0) throw new InvalidOperationException("Task timeout must be greater than zero.");
    }

    /// <summary>
    ///     Creates default background service options.
    /// </summary>
    public static BackgroundServiceOptions CreateDefault() { return new BackgroundServiceOptions(); }
}
