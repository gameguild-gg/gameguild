using GameGuild.Database;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for getting user by ID
/// </summary>
public class GetUserByIdHandler(ApplicationDbContext context) : IRequestHandler<GetUserByIdQuery, User?> {
  public async Task<User?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken) {
    IQueryable<User> query = context.Users.Include(user => user.Credentials);

    query = !request.IncludeDeleted ? query.Where(user => user.DeletedAt == null) : query.IgnoreQueryFilters();

    return await query.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
  }
}
