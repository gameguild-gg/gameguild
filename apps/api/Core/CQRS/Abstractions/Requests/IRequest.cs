namespace GameGuild.CQRS;

/// <summary> Marker interface to represent a request with a response </summary>
/// <typeparam name="TResponse"> Response type </typeparam>
public interface IRequest<out TResponse> : IBaseRequest { }

/// <summary> Marker interface to represent a request without a response </summary>
public interface IRequest : IRequest<Unit> { }
