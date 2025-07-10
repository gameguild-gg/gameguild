using GameGuild.Database;
using GameGuild.Modules.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// Handler for getting user by email
/// </summary>
public class GetUserByEmailQueryHandler(ApplicationDbContext context) : IRequestHandler<GetUserByEmailQuery, User?> {
  public async Task<User?> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken) { return await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted, cancellationToken); }
}
