namespace GameGuild.Notifications.Services.Email;

/// <summary>
/// Registry built from DI-registered renderers; one renderer per <see cref="NotificationType"/>.
/// </summary>
public sealed class EmailRendererRegistry(IEnumerable<IEmailRenderer> renderers) : IEmailRendererRegistry
{
    private readonly Dictionary<NotificationType, IEmailRenderer> _renderers =
        renderers.ToDictionary(renderer => renderer.Type);

    public IEmailRenderer? Resolve(NotificationType type) =>
        _renderers.TryGetValue(type, out var renderer) ? renderer : null;
}
