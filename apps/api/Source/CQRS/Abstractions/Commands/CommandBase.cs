namespace GameGuild.CQRS;

/// <summary> Base record for commands without a response </summary>
public abstract record CommandBase : CommandBase<Unit>, ICommand { }

/// <summary> Base record for commands with a response </summary>
/// <typeparam name="TResponse"> The response type </typeparam>
public abstract record CommandBase<TResponse> : ICommand<TResponse> {
  /// <summary> Unique identifier for the command </summary>
  public Guid CommandId { get; init; } = Guid.NewGuid();

  /// <summary> When the command was created </summary>
  public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
