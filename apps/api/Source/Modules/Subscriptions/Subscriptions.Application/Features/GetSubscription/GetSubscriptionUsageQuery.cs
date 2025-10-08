using MediatR;
using GameGuild.Modules.Subscriptions.DTOs;

namespace GameGuild.Modules.Subscriptions.Features.GetSubscription;

public record GetSubscriptionUsageQuery(Guid SubscriptionId) : IQuery<SubscriptionUsageDto>;

