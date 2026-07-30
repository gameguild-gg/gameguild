
namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Service interface for error budget calculations
/// </summary>
public interface IErrorBudgetCalculator
{
    /// <summary>
    ///     Calculates the error budget for an SLO
    /// </summary>
    /// <param name="sloId">SLO identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Error budget information</returns>
    Task<ErrorBudgetDto> CalculateAsync(Guid sloId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calculates the error budget for an SLO within a specific time window
    /// </summary>
    /// <param name="sloId">SLO identifier</param>
    /// <param name="startTime">Start of time window</param>
    /// <param name="endTime">End of time window</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Error budget information</returns>
    Task<ErrorBudgetDto> CalculateForPeriodAsync(Guid sloId, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);
}
