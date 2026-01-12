using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Handler for updating user metadata
/// </summary>
public class UpdateUserMetadataCommandHandler(IUserRepository userRepository, IUserMetadataRepository metadataRepository)
    : ICommandHandler<UpdateUserMetadataCommand>
{
    public async Task<Unit> Handle(UpdateUserMetadataCommand request, CancellationToken cancellationToken)
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

        // Update custom fields (partial update - merge with existing)
        if (request.Request.CustomFields is not null)
        {
            var existingFields = metadata.GetCustomFields();
            foreach (var field in request.Request.CustomFields)
            {
                existingFields[field.Key] = field.Value;
            }
            metadata.SetCustomFields(existingFields);
        }

        // Update tags (add and remove)
        if (request.Request.TagsToAdd is not null || request.Request.TagsToRemove is not null)
        {
            var existingTags = metadata.GetTags();
            
            if (request.Request.TagsToAdd is not null)
            {
                foreach (var tag in request.Request.TagsToAdd)
                {
                    if (!existingTags.Contains(tag))
                    {
                        existingTags.Add(tag);
                    }
                }
            }

            if (request.Request.TagsToRemove is not null)
            {
                existingTags.RemoveAll(t => request.Request.TagsToRemove.Contains(t));
            }

            metadata.SetTags(existingTags);
        }

        // Update external references (partial update - merge with existing)
        if (request.Request.ExternalReferences is not null)
        {
            var existingReferences = metadata.GetExternalReferences();
            foreach (var reference in request.Request.ExternalReferences)
            {
                existingReferences[reference.Key] = reference.Value;
            }
            metadata.SetExternalReferences(existingReferences);
        }

        await metadataRepository.UpdateAsync(metadata, cancellationToken).ConfigureAwait(false);
        await metadataRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
