using GameGuild.CQRS;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

public sealed record SubscriptionNotificationDto(
    Guid Id,
    Guid RecipientId,
    Guid? TenantId,
    Guid? SubscriptionId,
    string Channel,
    string Title,
    string Message,
    bool IsSent,
    DateTime? SentAt,
    DateTime CreatedAt);

public sealed record GetSubscriptionNotificationsQuery(
    Guid? TenantId = null,
    Guid? SubscriptionId = null,
    NotificationChannel? Channel = null,
    bool? IsSent = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<SubscriptionNotificationDto>>;

public sealed record ResendSubscriptionNotificationCommand(
    Guid NotificationId,
    NotificationChannel? Channel = null) : ICommand<SubscriptionNotificationDto>;

public sealed class GetSubscriptionNotificationsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetSubscriptionNotificationsQuery, PagedResult<SubscriptionNotificationDto>>
{
    public async Task<PagedResult<SubscriptionNotificationDto>> Handle(GetSubscriptionNotificationsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = context.Set<Notification>()
            .AsNoTracking()
            .Where(notification =>
                notification.Type == NotificationType.Billing &&
                notification.ReferenceEntityType == SubscriptionNotificationReferenceTypes.Subscription);

        if (request.TenantId.HasValue)
        {
            var tenantId = request.TenantId.Value;
            query = query.Where(notification => notification.TenantId == tenantId);
        }

        if (request.SubscriptionId.HasValue)
        {
            query = query.Where(notification => notification.ReferenceEntityId == request.SubscriptionId.Value);
        }

        if (request.Channel.HasValue)
        {
            query = query.Where(notification => notification.Channel == request.Channel.Value);
        }

        if (request.IsSent.HasValue)
        {
            query = query.Where(notification => notification.IsSent == request.IsSent.Value);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var notifications = await query
            .OrderByDescending(notification => notification.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return PagedResult<SubscriptionNotificationDto>.FromPage(
            notifications.Select(SubscriptionNotificationMapper.ToDto),
            total,
            page,
            pageSize);
    }
}

public sealed class ResendSubscriptionNotificationCommandHandler(
    IApplicationDbContext context,
    INotificationService notificationService)
    : ICommandHandler<ResendSubscriptionNotificationCommand, SubscriptionNotificationDto>
{
    public async Task<SubscriptionNotificationDto> Handle(ResendSubscriptionNotificationCommand request, CancellationToken cancellationToken)
    {
        var source = await context.Set<Notification>()
            .AsNoTracking()
            .FirstOrDefaultAsync(notification =>
                    notification.Id == request.NotificationId &&
                    notification.Type == NotificationType.Billing &&
                    notification.ReferenceEntityType == SubscriptionNotificationReferenceTypes.Subscription,
                cancellationToken)
            .ConfigureAwait(false);

        if (source is null)
        {
            throw new InvalidOperationException($"Subscription notification {request.NotificationId} was not found.");
        }

        var result = await notificationService.SendAsync(
                source.RecipientId,
                NotificationType.Billing,
                source.Title,
                source.Message,
                request.Channel ?? source.Channel,
                source.TenantId,
                source.ActionUrl,
                source.Priority,
                source.ReferenceEntityId,
                source.ReferenceEntityType,
                source.Metadata,
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Error.Description);
        }

        return SubscriptionNotificationMapper.ToDto(result.Value);
    }
}

internal static class SubscriptionNotificationReferenceTypes
{
    public const string Subscription = "subscription";
}

file static class SubscriptionNotificationMapper
{
    public static SubscriptionNotificationDto ToDto(Notification notification)
        => new(
            notification.Id,
            notification.RecipientId,
            notification.TenantId,
            notification.ReferenceEntityId,
            notification.Channel.ToString(),
            notification.Title,
            notification.Message,
            notification.IsSent,
            notification.SentAt,
            notification.CreatedAt);
}
