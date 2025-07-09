using GameGuild.Data;
using GameGuild.Modules.UserProfiles.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.UserProfiles.Handlers;

/// <summary>
/// Handler for getting all user profiles with filtering and pagination
/// </summary>
public class GetAllUserProfilesHandler(ApplicationDbContext context) 
    : IRequestHandler<GetAllUserProfilesQuery, IEnumerable<Models.UserProfile>>
{
    public async Task<IEnumerable<Models.UserProfile>> Handle(GetAllUserProfilesQuery request, CancellationToken cancellationToken)
    {
        var query = context.Resources.OfType<Models.UserProfile>()
            .Include(up => up.Metadata);

        // Apply filters
        if (!request.IncludeDeleted)
        {
            query = query.Where(up => up.DeletedAt == null);
        }
        else
        {
            query = query.IgnoreQueryFilters();
        }

        if (request.TenantId.HasValue)
        {
            query = query.Where(up => up.TenantId == request.TenantId);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(up => 
                (up.GivenName != null && up.GivenName.ToLower().Contains(searchLower)) ||
                (up.FamilyName != null && up.FamilyName.ToLower().Contains(searchLower)) ||
                (up.DisplayName != null && up.DisplayName.ToLower().Contains(searchLower)));
        }

        // Apply pagination
        query = query.Skip(request.Skip).Take(request.Take);

        return await query.ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Handler for getting user profile by ID
/// </summary>
public class GetUserProfileByIdHandler(ApplicationDbContext context) 
    : IRequestHandler<GetUserProfileByIdQuery, Models.UserProfile?>
{
    public async Task<Models.UserProfile?> Handle(GetUserProfileByIdQuery request, CancellationToken cancellationToken)
    {
        var query = context.Resources.OfType<Models.UserProfile>()
            .Include(up => up.Metadata);

        if (!request.IncludeDeleted)
        {
            query = query.Where(up => up.DeletedAt == null);
        }
        else
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.FirstOrDefaultAsync(up => up.Id == request.UserProfileId, cancellationToken);
    }
}

/// <summary>
/// Handler for getting user profile by user ID
/// </summary>
public class GetUserProfileByUserIdHandler(ApplicationDbContext context) 
    : IRequestHandler<GetUserProfileByUserIdQuery, Models.UserProfile?>
{
    public async Task<Models.UserProfile?> Handle(GetUserProfileByUserIdQuery request, CancellationToken cancellationToken)
    {
        var query = context.Resources.OfType<Models.UserProfile>()
            .Include(up => up.Metadata);

        if (!request.IncludeDeleted)
        {
            query = query.Where(up => up.DeletedAt == null);
        }
        else
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.FirstOrDefaultAsync(up => up.Id == request.UserId, cancellationToken);
    }
}
