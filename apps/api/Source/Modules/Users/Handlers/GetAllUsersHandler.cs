using GameGuild.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for getting all users with filtering and pagination
/// </summary>
public class GetAllUsersHandler(ApplicationDbContext context) : IRequestHandler<GetAllUsersQuery, IEnumerable<User>> {
  public async Task<IEnumerable<User>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken) {
    IQueryable<User> query = context.Users.Include(u => u.Credentials);

    // Apply filters
    query = !request.IncludeDeleted ? query.Where(user => user.DeletedAt == null) : query.IgnoreQueryFilters();

    if (request.IsActive.HasValue) query = query.Where(user => user.IsActive == request.IsActive.Value);

    // Apply pagination
    query = query.Skip(request.Skip).Take(request.Take);

    return await query.ToListAsync(cancellationToken);
  }
}
