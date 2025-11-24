using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Entities;
using GameGuild.Users.Models;
using GameGuild.Users.Repositories;

namespace GameGuild.Users.Commands;

/// <summary>
///     Handler for replacing user metadata
/// </summary>
public class ReplaceUserMetadataCommandHandler(IUserRepository userRepository, IUserMetadataRepository metadataRepository)
    : ICommandHandler<ReplaceUserMetadataCommand>
{
    public async Task<Unit> Handle(ReplaceUserMetadataCommand request, CancellationToken cancellationToken)
    {
        // Verify user exists
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            throw new UserNotFoundException();
        }

        // Get or create metadata
        var metadata = await metadataRepository.GetByUserIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (metadata is null)
        {
            metadata = UserMetadata.Create(request.UserId);
            await metadataRepository.AddAsync(metadata, cancellationToken).ConfigureAwait(false);
        }

        // Replace all fields completely
        metadata.SetCustomFields(request.Request.CustomFields);
        metadata.SetTags(request.Request.Tags);
        metadata.SetExternalReferences(request.Request.ExternalReferences);

        await metadataRepository.UpdateAsync(metadata, cancellationToken).ConfigureAwait(false);
        await metadataRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
