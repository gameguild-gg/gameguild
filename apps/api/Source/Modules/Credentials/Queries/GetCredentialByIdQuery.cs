using MediatR;


namespace GameGuild.Modules.Credentials.Queries;

/// <summary>
/// Query to get credential by ID using CQRS pattern
/// </summary>
public class GetCredentialByIdQuery : IRequest<Credential?> {
  /// <summary>
  /// The credential ID to search for
  /// </summary>
  public Guid Id { get; set; }

  public GetCredentialByIdQuery(Guid id) { Id = id; }
}
