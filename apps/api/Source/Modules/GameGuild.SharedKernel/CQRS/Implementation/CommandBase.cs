using GameGuild;

namespace GameGuild.CQRS.Implementation;

/// <summary>
///     Base record for commands with a response.
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public abstract record CommandBase<TResponse> : RequestBase, ICommand<TResponse>
{
    /// <summary>
    ///     Alias for <see cref="RequestBase.RequestId" /> for backward compatibility.
    /// </summary>
    public Guid CommandId => RequestId;
}
