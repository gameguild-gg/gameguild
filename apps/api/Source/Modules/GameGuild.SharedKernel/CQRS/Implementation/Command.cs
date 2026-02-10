namespace GameGuild.CQRS.Implementation;

/// <summary>
///     Base record for commands without a response.
///     Implements <see cref="ICommand"/> which returns <see cref="Unit"/>.
/// </summary>
public abstract record CommandBase : ICommand;
