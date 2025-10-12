using GameGuild.CQRS;

namespace GameGuild.Modules.Users.Queries;

/// <summary>
///     Query to check if multiple users exist by their email addresses
/// </summary>
public sealed class BulkUserExistsByEmailsQuery : IRequest<Dictionary<string, bool>>
{
    /// <summary>
    ///     Collection of email addresses to check
    /// </summary>
    public required IEnumerable<string> Emails { get; init; }
}

/// <summary>
///     Handler for BulkUserExistsByEmailsQuery
/// </summary>
public sealed class BulkUserExistsByEmailsQueryHandler : IRequestHandler<BulkUserExistsByEmailsQuery, Dictionary<string, bool>>
{
    private readonly IUserRepository _userRepository;

    public BulkUserExistsByEmailsQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Dictionary<string, bool>> Handle(BulkUserExistsByEmailsQuery request, CancellationToken cancellationToken)
    {
        var results = await _userRepository.CheckEmailsExistAsync(request.Emails, cancellationToken);
        return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
