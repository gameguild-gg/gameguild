using GameGuild.Shared.Abstractions;
using GameGuild.Modules.Subscriptions.Models;
﻿using GameGuild.Modules.Subscriptions.Entities;

namespace GameGuild.Modules.Subscriptions.Specifications;

/// <summary>
///     Specification for finding subscriptions expiring soon
/// </summary>
public class SubscriptionsExpiringSoonSpecification : Specification<Subscription>
{
    public SubscriptionsExpiringSoonSpecification(int daysFromNow) : base(s =>
        s.EndDate.HasValue && s.EndDate.Value <= DateTime.UtcNow.AddDays(daysFromNow) && s.Status == SubscriptionStatus.Active
    )
    {
        ApplyOrderBy(s => s.EndDate!);
    }
}

