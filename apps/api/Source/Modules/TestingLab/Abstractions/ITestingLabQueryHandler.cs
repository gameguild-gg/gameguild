namespace GameGuild.Modules.TestingLab.Abstractions;

/// <summary> Base interface for query handlers in the Testing Lab module </summary>
/// <typeparam name="TQuery"> The query type </typeparam>
/// <typeparam name="TResult"> The result type </typeparam>
public interface ITestingLabQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, TResult>
  where TQuery : IRequest<TResult> { }
