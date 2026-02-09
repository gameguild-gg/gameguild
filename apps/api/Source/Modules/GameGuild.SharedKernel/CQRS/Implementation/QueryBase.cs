using GameGuild;

namespace GameGuild.CQRS.Implementation;

/// <summary>
///     Base record for queries.
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public abstract record QueryBase<TResponse> : RequestBase, IQuery<TResponse>
{
    /// <summary>
    ///     Alias for <see cref="RequestBase.RequestId" /> for backward compatibility.
    /// </summary>
    public Guid QueryId => RequestId;
}
