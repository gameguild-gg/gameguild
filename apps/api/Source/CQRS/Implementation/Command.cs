namespace GameGuild.CQRS;

/// <summary>
/// Base record for commands without a response
/// </summary>
public abstract record CommandBase : CommandBase<Unit>, ICommand { }
