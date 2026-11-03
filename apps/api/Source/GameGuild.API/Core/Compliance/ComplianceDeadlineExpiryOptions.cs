namespace GameGuild.API.Core.Compliance;

/// <summary>
///     Scheduling policy for the statutory deadline sweeper (issue #1219 N3). The
///     sweeper transitions overdue data subject requests (GDPR, 30-day deadline) and
///     FERPA inspection requests (45-day deadline) to their <c>Expired</c> states.
/// </summary>
public sealed class ComplianceDeadlineExpiryOptions
{
    public static string SectionName => "Compliance:DeadlineExpiry";
    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromMinutes(1);

    public List<string> Validate()
    {
        var errors = new List<string>();
        if (InitialDelay < TimeSpan.Zero || InitialDelay > TimeSpan.FromDays(1)) { errors.Add("InitialDelay must be between zero and one day."); }
        if (Interval < TimeSpan.FromSeconds(1) || Interval > TimeSpan.FromDays(1)) { errors.Add("Interval must be between one second and one day."); }
        if (ExecutionTimeout < TimeSpan.FromSeconds(1) || ExecutionTimeout > TimeSpan.FromMinutes(5)) { errors.Add("ExecutionTimeout must be between one second and five minutes."); }
        return errors;
    }
}
