namespace GameGuild.CQRS;

/// <summary> Defines a request pre-processor </summary>
/// <typeparam name="TRequest"> Request type </typeparam>
public interface IRequestPreProcessor<in TRequest> where TRequest : IBaseRequest {
  /// <summary> Process method executed before the handler </summary>
  /// <param name="request"> Incoming request </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Task representing the async operation </returns>
  Task Process(TRequest request, CancellationToken cancellationToken);
}
