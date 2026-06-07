using GameGuild.CQRS;

namespace GameGuild.Content.Pages;

public sealed class PublishContentResourceCommandHandler(IContentResourceService resourceService) : ICommandHandler<PublishContentResourceCommand, ContentResourceDto?>
{
    public async Task<ContentResourceDto?> Handle(PublishContentResourceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ResourceId == Guid.Empty)
        {
            throw new ArgumentException("Content resource ID must be a non-empty GUID.", nameof(request.ResourceId));
        }

        if (request.PublishedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Publisher user ID must be a non-empty GUID.", nameof(request.PublishedByUserId));
        }

        var resource = await resourceService
            .PublishAsync(request.ResourceId, request.PublishedByUserId, cancellationToken)
            .ConfigureAwait(false);

        return resource?.ToDto();
    }
}
