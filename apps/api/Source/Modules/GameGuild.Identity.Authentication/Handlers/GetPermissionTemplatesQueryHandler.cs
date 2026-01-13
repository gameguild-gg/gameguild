using GameGuild.Abstractions;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Query handler for retrieving available permission templates.
/// </summary>
public class GetPermissionTemplatesQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetPermissionTemplatesQueryHandler> logger
) : IQueryHandler<GetPermissionTemplatesQuery, IEnumerable<PermissionTemplateDto>>
{
    public async Task<IEnumerable<PermissionTemplateDto>> Handle(
        GetPermissionTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching permission templates");

        try
        {
            var templates = await dbContext.Set<PermissionTemplate>()
                .Where(t => t.IsActive)
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Name)
                .ToListAsync(cancellationToken);

            var dtos = templates.Select(t => new PermissionTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Category = t.Category ?? "General",
                Permissions = t.Permissions.ToList(),
                IsSystemTemplate = t.IsSystemTemplate,
                IsActive = t.IsActive,
                MinimumTier = t.MinimumTier,
                CreatedAt = t.CreatedAt
            });

            logger.LogInformation("Retrieved {Count} permission templates", templates.Count);

            return dtos;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve permission templates");
            throw;
        }
    }
}
