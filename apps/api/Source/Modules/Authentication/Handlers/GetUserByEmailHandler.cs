using GameGuild.Data;
using GameGuild.Modules.Authentication.Queries;
using GameGuild.Modules.Users.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Authentication.Handlers;

/// <summary>
/// Handler for getting user by email query
/// </summary>
public class GetUserByEmailHandler(ApplicationDbContext context) : IRequestHandler<GetUserByEmailQuery, User?> {
  public async Task<User?> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken) { return await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted, cancellationToken); }
}
