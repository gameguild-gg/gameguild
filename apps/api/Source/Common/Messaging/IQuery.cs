using MediatR;


namespace GameGuild.Common;

/// <summary>
/// Base interface for all queries in the system
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse> { }
