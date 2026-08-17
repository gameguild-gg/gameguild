using GameGuild.CQRS;

namespace GameGuild.Announcements.Contracts;

public enum PublicationKind
{
    CoursePublished,
    ProjectPublished,
    TestingEventCreated,
    ProjectJoinedTestingEvent,
}

/// <summary>
/// Dispatched by domain modules through the mediator when content is published;
/// handled by the Social.Announcements module (community post + notifications).
/// </summary>
public sealed record AnnouncePublicationCommand : ICommand<Result>
{
    public PublicationKind Kind { get; init; }

    public Guid ActorId { get; init; }

    public required string Title { get; init; }

    public Guid EntityId { get; init; }

    public string? Slug { get; init; }

    public string? SecondaryTitle { get; init; }

    public Guid? NotifyUserId { get; init; }

    public DateTime? StartsAt { get; init; }

    public Guid? TenantId { get; init; }
}
