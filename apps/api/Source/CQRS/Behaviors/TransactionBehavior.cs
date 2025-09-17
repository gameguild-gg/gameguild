using System.Transactions;
using GameGuild.Database;


namespace GameGuild.CQRS;

/// <summary> Interface for requests that require a database transaction </summary>
public interface ITransactionalRequest {
  /// <summary> Isolation level for the transaction </summary>
  IsolationLevel? IsolationLevel { get => null; }
}

/// <summary> Pipeline behavior for wrapping commands in database transactions </summary>
/// <typeparam name="TRequest"> Request type </typeparam>
/// <typeparam name="TResponse"> Response type </typeparam>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IBaseRequest {
  private readonly ApplicationDbContext _context;

  private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

  /// <summary> Initializes a new instance of the TransactionBehavior class </summary>
  /// <param name="context"> Database context </param>
  /// <param name="logger"> Logger </param>
  public TransactionBehavior(ApplicationDbContext context, ILogger<TransactionBehavior<TRequest, TResponse>> logger) {
    _context = context;
    _logger = logger;
  }

  /// <summary> Handles the request pipeline with transaction management </summary>
  /// <param name="request"> Request </param>
  /// <param name="next"> Next handler delegate </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Response </returns>
  public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegateBase<TResponse> next, CancellationToken cancellationToken) {
    ArgumentNullException.ThrowIfNull(next);

    // Only wrap commands in transactions, not queries
    if (request is not ICommand && request is not ITransactionalRequest) { return await next().ConfigureAwait(false); }

    // If we already have an active transaction, don't create a new one
    if (_context.Database.CurrentTransaction is not null) { return await next().ConfigureAwait(false); }

    var isolationLevel = (request as ITransactionalRequest)?.IsolationLevel ?? System.Data.IsolationLevel.ReadCommitted;

    _logger.LogDebug("Starting transaction for {RequestType} with isolation level {IsolationLevel}", typeof(TRequest).Name, isolationLevel);

    var strategy = _context.Database.CreateExecutionStrategy();

    return await strategy.ExecuteAsync(async () => {
               await using var transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);

               try {
                 var response = await next().ConfigureAwait(false);

                 // Check if the response indicates failure (for Result types)
                 if (IsFailureResponse(response)) {
                   _logger.LogDebug("Rolling back transaction for {RequestType} due to failure response", typeof(TRequest).Name);
                   await transaction.RollbackAsync(cancellationToken);

                   return response;
                 }

                 await transaction.CommitAsync(cancellationToken);

                 _logger.LogDebug("Committed transaction for {RequestType}", typeof(TRequest).Name);

                 return response;
               }
               catch (Exception ex) {
                 _logger.LogError(ex, "Error in transaction for {RequestType}, rolling back", typeof(TRequest).Name);

                 try { await transaction.RollbackAsync(cancellationToken); }
                 catch (Exception rollbackEx) { _logger.LogError(rollbackEx, "Error rolling back transaction for {RequestType}", typeof(TRequest).Name); }

                 throw;
               }
             }
           );
  }

  /// <summary> Determines if a response indicates failure (for Result types) </summary>
  /// <param name="response"> The response to check </param>
  /// <returns> True if the response indicates failure </returns>
  private static bool IsFailureResponse(TResponse response) { return response switch { IResult result => result.IsFailure, _ => false }; }
}
