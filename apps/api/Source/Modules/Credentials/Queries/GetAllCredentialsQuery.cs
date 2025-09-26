using GameGuild.CQRS;

namespace GameGuild.Modules.Credentials;

/// <summary> Query to get all credentials using CQRS pattern </summary>
public class GetAllCredentialsQuery : IRequest<IEnumerable<Credential>> { }
