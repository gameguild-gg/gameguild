namespace GameGuild.CQRS;

/// <summary> Represents a request that can be streamed </summary>
/// <typeparam name="TResponse"> Response type </typeparam>
public interface IStreamRequest<out TResponse> : IBaseRequest { }
