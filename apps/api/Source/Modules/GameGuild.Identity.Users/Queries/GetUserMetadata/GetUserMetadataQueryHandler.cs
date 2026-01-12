using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
/// Query handler for getting user metadata
/// </summary>
public class GetUserMetadataQueryHandler(IUserRepository userRepository) : IQueryHandler<GetUserMetadataQuery, UserMetadataDto?>
{
    public async Task<UserMetadataDto?> Handle(GetUserMetadataQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        
        if (user == null)
            return null;

        // Return default metadata since User entity doesn't have metadata yet
        return new UserMetadataDto(
            Id: Guid.NewGuid(),
            UserId: user.Id,
            CustomFields: new Dictionary<string, object?>(),
            Tags: new List<string>(),
            ExternalReferences: new Dictionary<string, string>(),
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt,
            Version: new byte[] { 1 }
        );
    }
}
