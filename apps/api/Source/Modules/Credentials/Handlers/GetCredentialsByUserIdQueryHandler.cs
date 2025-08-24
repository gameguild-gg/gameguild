using GameGuild.Database;
using GameGuild.Modules.Credentials.Queries;


namespace GameGuild.Modules.Credentials.Handlers;

/// <summary>
/// Handler for getting credentials by user ID query using CQRS pattern
/// </summary>
public class GetCredentialsByUserIdQueryHandler : IRequestHandler<GetCredentialsByUserIdQuery, IEnumerable<Credential>> {
  private readonly ApplicationDbContext _context;
  private readonly ILogger<GetCredentialsByUserIdQueryHandler> _logger;

  public GetCredentialsByUserIdQueryHandler(ApplicationDbContext context, ILogger<GetCredentialsByUserIdQueryHandler> logger) {
    _context = context ?? throw new ArgumentNullException(nameof(context));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task<IEnumerable<Credential>> Handle(GetCredentialsByUserIdQuery request, CancellationToken cancellationToken) {
    _logger.LogInformation("Retrieving credentials for user {UserId}", request.UserId);

    try {
      var credentials = await _context.Credentials
                                      .Where(c => c.UserId == request.UserId)
                                      .ToListAsync(cancellationToken);

      _logger.LogInformation("Retrieved {Count} credentials for user {UserId}", credentials.Count, request.UserId);

      return credentials;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Failed to retrieve credentials for user {UserId}", request.UserId);

      throw;
    }
  }
}
