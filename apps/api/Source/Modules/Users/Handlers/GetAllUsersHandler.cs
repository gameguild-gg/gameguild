using GameGuild.Data;
using GameGuild.Modules.Users.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Users.Handlers;

/// <summary>
/// Handler for getting all users with filtering and pagination
/// </summary>
public class GetAllUsersHandler(ApplicationDbContext context) : IRequestHandler<GetAllUsersQuery, IEnumerable<Models.User>>
{
    public async Task<IEnumerable<Models.User>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Models.User> query = context.Users.Include(u => u.Credentials);

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

        // Apply pagination
        query = query.Skip(request.Skip).Take(request.Take);

        return await query.ToListAsync(cancellationToken);
    }
}
