using GameGuild.CQRS;
using GameGuild.Database;
using GameGuild.Infrastructure.Common.ValueObjects;
using GameGuild.Modules.Subscriptions.Abstractions;
using GameGuild.Modules.Subscriptions.Models;
using GameGuild.CQRS.Abstractions.Commands;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Subscriptions.Commands.CreateSubscription;

/// <summary>
/// Command handler for creating a new subscription
/// </summary>
public class CreateSubscriptionCommandHandler : ICommandHandler<CreateSubscriptionCommand, Guid>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ApplicationDbContext _context;

    public CreateSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository, ApplicationDbContext context)
    {
        _subscriptionRepository = subscriptionRepository;
        _context = context;
    }

    public async Task<Guid> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        // Validate that the user exists
        var userExists = await _context.Users
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);
        
        if (!userExists)
            throw new ArgumentException($"User with ID {request.UserId} not found");

        // Validate that the subscription plan exists
        var planExists = await _context.ProductSubscriptionPlans
            .AnyAsync(p => p.Id == request.SubscriptionPlanId, cancellationToken);
            
        if (!planExists)
            throw new ArgumentException($"Subscription plan with ID {request.SubscriptionPlanId} not found");

        DateTime startDate = request.StartDate ?? DateTime.UtcNow;
        DateTime? trialEndDate = request.TrialDays.HasValue
            ? startDate.AddDays(request.TrialDays.Value)
            : null;

        var money = new Money(request.Amount, request.Currency);

        var subscription = UserSubscription.Create(
            request.UserId,
            request.SubscriptionPlanId,
            request.BillingCycle,
            money,
            startDate,
            trialEndDate
        );

        await _subscriptionRepository.AddAsync(subscription, cancellationToken);
        await _subscriptionRepository.SaveChangesAsync(cancellationToken);

        return subscription.Id;
    }
}