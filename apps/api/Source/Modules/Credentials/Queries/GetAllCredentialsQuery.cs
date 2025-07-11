using MediatR;


namespace GameGuild.Modules.Credentials.Queries;

/// <summary>
/// Query to get all credentials using CQRS pattern
/// </summary>
public class GetAllCredentialsQuery : IRequest<IEnumerable<Credential>>
{
}
