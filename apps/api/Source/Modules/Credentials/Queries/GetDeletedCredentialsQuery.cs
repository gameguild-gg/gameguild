namespace GameGuild.Modules.Credentials.Queries;

/// <summary>
/// Query to get soft-deleted credentials using CQRS pattern
/// </summary>
public class GetDeletedCredentialsQuery : IRequest<IEnumerable<Credential>> {
  // No parameters needed for this query
}
