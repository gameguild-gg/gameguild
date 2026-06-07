namespace GameGuild;

/// <summary>
///     Captures metrics from the CQRS handler/validator registration process.
/// </summary>
public class RegistrationMetrics
{
    /// <summary>Total number of request handlers registered from scanned assemblies.</summary>
    public int TotalHandlersRegistered { get; set; }

    /// <summary>Total number of FluentValidation validators registered from scanned assemblies.</summary>
    public int TotalValidatorsRegistered { get; set; }

    /// <summary>Wall-clock time spent scanning assemblies and registering services.</summary>
    public TimeSpan RegistrationDuration { get; set; }
}
