using GameGuild.Database;
using GameGuild.Modules.Credentials.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Credentials.Handlers;

/// <summary>
/// Handler for getting credential by ID query using CQRS pattern
/// </summary>
public class GetCredentialByIdQueryHandler : IRequestHandler<GetCredentialByIdQuery, Credential?> {
  private readonly ApplicationDbContext _context;
  private readonly ILogger<GetCredentialByIdQueryHandler> _logger;

  public GetCredentialByIdQueryHandler(ApplicationDbContext context, ILogger<GetCredentialByIdQueryHandler> logger) {
    _context = context ?? throw new ArgumentNullException(nameof(context));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task<Credential?> Handle(GetCredentialByIdQuery request, CancellationToken cancellationToken) {
    _logger.LogInformation("Retrieving credential {CredentialId}", request.Id);

    try {
      var credential = await _context.Credentials
                                     .Include(c => c.User)
                                     .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

      if (credential != null) { _logger.LogInformation("Found credential {CredentialId}", request.Id); }
      else { _logger.LogWarning("Credential {CredentialId} not found", request.Id); }

      return credential;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Failed to retrieve credential {CredentialId}", request.Id);

      throw;
    }
  }
}
