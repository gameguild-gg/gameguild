using GameGuild.Abstractions;
using GameGuild.CQRS;
using GameGuild.SharedKernel.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Queries;

/// <summary>
/// Query handler for GetProgramBySlugQuery
/// </summary>
public class GetProgramBySlugQueryHandler(IApplicationDbContext context, ILogger<GetProgramBySlugQueryHandler> logger)
    : IQueryHandler<GetProgramBySlugQuery, Program?>
{
    public async Task<Program?> Handle(GetProgramBySlugQuery request, CancellationToken cancellationToken) {
    logger.LogInformation("Getting program by slug: {Slug}", request.Slug);

    var query = context.Set<Program>()
      .AsNoTracking() // Read-only query optimization
      .Where(p => p.Slug == request.Slug && p.DeletedAt == null);

    if (request.IncludeContent) query = query.Include(p => p.ProgramContents.Where(pc => !pc.IsDeleted));

    if (request.IncludeEnrollments) query = query.Include(p => p.ProgramUsers.Where(pu => !pu.IsDeleted));

    if (request.IncludeRatings) query = query.Include(p => p.ProgramRatings);

    var program = await query.FirstOrDefaultAsync(cancellationToken);

    if (program != null)
      logger.LogInformation("Found program by slug: {Slug}", request.Slug);
    else
      logger.LogWarning("Program not found by slug: {Slug}", request.Slug);

    return program;
  }
}
