using GameGuild.Database;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for getting user by email
/// </summary>
public class GetUserByEmailHandler(ApplicationDbContext context) : IRequestHandler<GetUserByEmailQuery, User?> {
  public async Task<User?> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken) {
    IQueryable<User> query = context.Users.Include(u => u.Credentials);

    query = !request.IncludeDeleted ? query.Where(user => user.DeletedAt == null) : query.IgnoreQueryFilters();

    return await query.FirstOrDefaultAsync(user => user.Email == request.Email, cancellationToken);
  }
}
