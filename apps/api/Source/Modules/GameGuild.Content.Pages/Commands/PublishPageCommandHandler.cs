using GameGuild.CQRS;

namespace GameGuild.Content.Pages;

public sealed class PublishPageCommandHandler(IPageService pageService) : ICommandHandler<PublishPageCommand, PageDto?>
{
    public async Task<PageDto?> Handle(PublishPageCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PageId == Guid.Empty)
        {
            throw new ArgumentException("Page ID must be a non-empty GUID.", nameof(request.PageId));
        }

        if (request.PublishedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Publisher user ID must be a non-empty GUID.", nameof(request.PublishedByUserId));
        }

        var page = await pageService
            .PublishAsync(request.PageId, request.PublishedByUserId, cancellationToken)
            .ConfigureAwait(false);

        return page?.ToDto();
    }
}
