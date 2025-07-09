using GameGuild.Data;
using GameGuild.Modules.Users.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Users.Handlers;

/// <summary>
/// Handler for getting user by ID
/// </summary>
public class GetUserByIdHandler(ApplicationDbContext context) : IRequestHandler<GetUserByIdQuery, Models.User?>
{
    public async Task<Models.User?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var query = context.Users.Include(u => u.Credentials);

        if (!request.IncludeDeleted)
        {
            query = query.Where(u => u.DeletedAt == null);
        }
        else
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
    }
}

/// <summary>
/// Handler for getting user by email
/// </summary>
public class GetUserByEmailHandler(ApplicationDbContext context) : IRequestHandler<GetUserByEmailQuery, Models.User?>
{
    public async Task<Models.User?> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
    {
        var query = context.Users.Include(u => u.Credentials);

        if (!request.IncludeDeleted)
        {
            query = query.Where(u => u.DeletedAt == null);
        }
        else
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
    }
}

/// <summary>
/// Handler for searching users with filtering and pagination
/// </summary>
public class SearchUsersHandler(ApplicationDbContext context) : IRequestHandler<SearchUsersQuery, IEnumerable<Models.User>>
{
    public async Task<IEnumerable<Models.User>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        var query = context.Users.Include(u => u.Credentials);

        // Apply filters
        if (!request.IncludeDeleted)
        {
            query = query.Where(u => u.DeletedAt == null);
        }
        else
        {
            query = query.IgnoreQueryFilters();
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(u => 
                u.Name.ToLower().Contains(searchLower) || 
                u.Email.ToLower().Contains(searchLower));
        }

        if (request.MinBalance.HasValue)
        {
            query = query.Where(u => u.Balance >= request.MinBalance.Value);
        }

        if (request.MaxBalance.HasValue)
        {
            query = query.Where(u => u.Balance <= request.MaxBalance.Value);
        }

        // Apply pagination
        query = query.Skip(request.Skip).Take(request.Take);

        return await query.ToListAsync(cancellationToken);
    }
}
