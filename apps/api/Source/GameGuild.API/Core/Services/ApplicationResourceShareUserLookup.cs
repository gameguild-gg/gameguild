using GameGuild.CQRS.Models;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.API.Core.Services;

public sealed class ApplicationResourceShareUserLookup(IApplicationDbContext dbContext) : IResourceShareUserLookup
{
    public async Task<ResourceShareUser?> FindByEmailAsync(
        TenantId tenantId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await dbContext.Set<User>()
            .Where(user => user.IsActive && !user.IsSuspended && user.Email.ToLower() == normalizedEmail)
            .Select(user => new ResourceShareUser(user.Id, user.Email, user.Name))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ResourceShareUser?> FindByIdAsync(
        TenantId tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<User>()
            .Where(user => user.Id == userId)
            .Select(user => new ResourceShareUser(user.Id, user.Email, user.Name))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
