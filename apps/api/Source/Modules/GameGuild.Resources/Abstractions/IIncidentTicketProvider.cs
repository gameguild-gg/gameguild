namespace GameGuild.Resources;

/// <summary>
///     Abstraction for creating incident tickets in external incident management systems.
///     Implemented by the Incident Management module or external integrations.
/// </summary>
public interface IIncidentTicketProvider
{
    /// <summary>
    ///     Creates an incident ticket for an SLA violation.
    /// </summary>
    /// <param name="violation">The SLA violation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created incident ticket ID</returns>
    Task<string> CreateTicketAsync(SlaImpactAnalysis violation, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing incident ticket with new information.
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="status">New status</param>
    /// <param name="notes">Additional notes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateTicketAsync(string ticketId, string status, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Closes an incident ticket.
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="resolutionNotes">Resolution notes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CloseTicketAsync(string ticketId, string? resolutionNotes = null, CancellationToken cancellationToken = default);
}

/// <summary>
///     Default implementation that generates simple ticket IDs without external integration.
///     Replace with real implementation when Incident Management module is available.
/// </summary>
public class DefaultIncidentTicketProvider : IIncidentTicketProvider
{
    public Task<string> CreateTicketAsync(SlaImpactAnalysis violation, CancellationToken cancellationToken = default)
    {
        // Generate a ticket ID based on violation details
        var ticketId = $"INC-{DateTime.UtcNow:yyyyMMdd}-{violation.Id.ToString()[..8].ToUpper()}";
        return Task.FromResult(ticketId);
    }

    public Task UpdateTicketAsync(string ticketId, string status, string? notes = null, CancellationToken cancellationToken = default)
    {
        // No-op in default implementation
        return Task.CompletedTask;
    }

    public Task CloseTicketAsync(string ticketId, string? resolutionNotes = null, CancellationToken cancellationToken = default)
    {
        // No-op in default implementation
        return Task.CompletedTask;
    }
}
