using GameGuild.Modules.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// GraphQL queries for Auth module using CQRS pattern
/// </summary>
[ExtendObjectType<DbLoggerCategory.Query>]
public class AuthQueries {
  /// <summary>
  /// Get user by email using CQRS
  /// </summary>
  public async Task<User?> GetUserByEmail(string email, [Service] IMediator mediator) {
    var query = new GetUserByEmailQuery { Email = email };

    return await mediator.Send(query);
  }
}
