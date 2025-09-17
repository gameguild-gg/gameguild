namespace GameGuild.CQRS;

/// <summary> Defines a handler for a request with a response </summary>
/// <typeparam name="TRequest"> The type of request being handled </typeparam>
/// <typeparam name="TResponse"> The type of response from the handler </typeparam>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> {
  /// <summary> Handles a request </summary>
  /// <param name="request"> The request </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Response from the request </returns>
  Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary> Defines a handler for a request without a response (Command pattern) </summary>
/// <typeparam name="TRequest"> The type of request being handled </typeparam>
public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit> where TRequest : IRequest<Unit> { }
