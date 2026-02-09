
namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Service interface for alert management.
///     Integrated with the Notifications module (GameGuild.Notifications) for delivery.
/// </summary>
public interface IAlertManager
{
    /// <summary>
    ///     Checks if an SLO should trigger an alert and sends it
    /// </summary>
    /// <param name="slo">Service Level Objective</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if alert was triggered</returns>
    Task<bool> CheckAndTriggerAlertAsync(ServiceLevelObjective slo, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends an alert for a violation
    /// </summary>
    /// <param name="violation">SLO violation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if alert was sent successfully</returns>
    Task<bool> SendViolationAlertAsync(SloViolation violation, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends an alert when error budget threshold is exceeded
    /// </summary>
    /// <param name="slo">Service Level Objective</param>
    /// <param name="remainingBudgetPercentage">Remaining budget percentage</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if alert was sent successfully</returns>
    Task<bool> SendErrorBudgetAlertAsync(ServiceLevelObjective slo, double remainingBudgetPercentage, CancellationToken cancellationToken = default);
}
