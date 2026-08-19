namespace GameGuild.Notifications.Services.Email;

/// <summary>
/// Applies suppression side effects to a stored <see cref="EmailDeliveryEvent"/>: hard bounces
/// and spam complaints auto-suppress the recipient address and deadletter its pending email sends.
/// The processor is called inline by the SNS webhook controller WITHIN its transaction — it mutates
/// tracked entities only and NEVER saves; the controller's single SaveChanges persists event +
/// suppression + deadletters atomically (a mid-batch failure rolls back everything → 500 → the SNS
/// retry re-ingests cleanly).
/// </summary>
public interface IEmailEventProcessor
{
    /// <summary>
    /// Processes a stored delivery event for suppression side effects. Idempotent by construction
    /// (upserts, not inserts), so re-running on SNS redelivery of the same event is safe.
    /// </summary>
    /// <param name="deliveryEvent">The already-stored event whose side effects to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessAsync(EmailDeliveryEvent deliveryEvent, CancellationToken cancellationToken = default);
}
