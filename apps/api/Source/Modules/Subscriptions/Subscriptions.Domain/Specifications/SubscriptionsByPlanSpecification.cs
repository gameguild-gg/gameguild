using GameGuild.Modules.Subscriptions.Models;
﻿using GameGuild.Modules.Subscriptions.Entities;

namespace GameGuild.Modules.Subscriptions.Specifications;

/// <summary>
///     Specification for finding subscriptions by plan
/// </summary>
public class SubscriptionsByPlanSpecification : Specification<Subscription>
{
    public SubscriptionsByPlanSpecification(Guid planId) : base(s => s.PlanId == planId) { ApplyOrderByDescending(s => s.CreatedAt); }
}

