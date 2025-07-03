using MediatR;
using GameGuild.Modules.Auth.Queries;
using GameGuild.Modules.Users.GraphQL;
using GameGuild.Modules.Users.Models;


namespace GameGuild.Modules.Auth.GraphQL;

/// <summary>
/// GraphQL queries for Auth module using CQRS pattern
/// </summary>
[ExtendObjectType<Query>]
public class AuthQueries {
  /// <summary>
  /// Get user by email using CQRS
  /// </summary>
  public async Task<User?> GetUserByEmail(string email, [Service] IMediator mediator) {
    var query = new GetUserByEmailQuery { Email = email };

    return await mediator.Send(query);
  }
}
