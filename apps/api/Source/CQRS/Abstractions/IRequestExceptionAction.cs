namespace GameGuild.CQRS;

/// <summary>
/// Defines a generic exception action for requests
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TException">Exception type</typeparam>
public interface IRequestExceptionAction<in TRequest, in TException>
    where TRequest : IBaseRequest
    where TException : Exception
{
    /// <summary>
    /// Executes action when exception occurs
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="exception">Exception</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task Execute(TRequest request, TException exception, CancellationToken cancellationToken);
}
