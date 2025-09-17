namespace GameGuild.CQRS;

/// <summary> Defines a request post-processor </summary>
/// <typeparam name="TRequest"> Request type </typeparam>
/// <typeparam name="TResponse"> Response type </typeparam>
public interface IRequestPostProcessor<in TRequest, in TResponse> where TRequest : IBaseRequest {
  /// <summary> Process method executed after the handler </summary>
  /// <param name="request"> Request </param>
  /// <param name="response"> Response </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Task representing the async operation </returns>
  Task Process(TRequest request, TResponse response, CancellationToken cancellationToken);
}
