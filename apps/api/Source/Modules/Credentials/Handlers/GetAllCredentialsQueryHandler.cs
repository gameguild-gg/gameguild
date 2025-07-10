using GameGuild.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// Handler for getting all credentials query using CQRS pattern
/// </summary>
public class GetAllCredentialsQueryHandler : IRequestHandler<GetAllCredentialsQuery, IEnumerable<Credential>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetAllCredentialsQueryHandler> _logger;

    public GetAllCredentialsQueryHandler(ApplicationDbContext context, ILogger<GetAllCredentialsQueryHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<Credential>> Handle(GetAllCredentialsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all credentials");

        try
        {
            var credentials = await _context.Credentials
                .Include(c => c.User)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} credentials", credentials.Count);
            return credentials;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve credentials");
            throw;
        }
    }
}
