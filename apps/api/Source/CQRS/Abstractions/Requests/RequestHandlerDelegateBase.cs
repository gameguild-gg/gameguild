namespace GameGuild.CQRS;

/// <summary>
/// Request handler delegate
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
/// <returns>Response</returns>
public delegate Task<TResponse> RequestHandlerDelegateBase<TResponse>();
