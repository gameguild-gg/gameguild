using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Abstractions;
using GameGuild.Modules.Subscriptions.Commands.CancelSubscription;
using GameGuild.Modules.Subscriptions.Commands.CreateSubscription;
using GameGuild.Modules.Subscriptions.Models;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.Subscriptions.Controllers;

/// <summary> Controller for managing user subscriptions with enhanced domain patterns </summary>
[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase {
  private readonly IMediator _mediator;

  private readonly ISubscriptionRepository _subscriptionRepository;

  public SubscriptionsController(IMediator mediator, ISubscriptionRepository subscriptionRepository) {
    _mediator = mediator;
    _subscriptionRepository = subscriptionRepository;
  }

  /// <summary> Create a new subscription </summary>
  [HttpPost]
  public async Task<ActionResult<SubscriptionResponse>> CreateSubscription([FromBody] CreateSubscriptionRequest request, CancellationToken cancellationToken) {
    var command = new CreateSubscriptionCommand(request.UserId, request.SubscriptionPlanId, request.BillingCycle, request.Amount, request.Currency ?? "USD", request.StartDate, request.TrialDays);

    var subscriptionId = await _mediator.Send(command, cancellationToken);

    var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);

    return Ok(MapToResponse(subscription!));
  }

  /// <summary> Cancel a subscription </summary>
  [HttpPost("{id:guid}/cancel")]
  public async Task<ActionResult> CancelSubscription(Guid id, [FromBody] CancelSubscriptionRequest request, CancellationToken cancellationToken) {
    var command = new CancelSubscriptionCommand(id, request.Reason, request.Note, request.EffectiveDate);

    await _mediator.Send(command, cancellationToken);

    return NoContent();
  }

  /// <summary> Get subscription details </summary>
  [HttpGet("{id:guid}")]
  public async Task<ActionResult<SubscriptionResponse>> GetSubscription(Guid id, CancellationToken cancellationToken) {
    var subscription = await _subscriptionRepository.GetByIdAsync(id, cancellationToken);

    if (subscription == null) return NotFound();

    return Ok(MapToResponse(subscription));
  }

  /// <summary> Get user subscriptions </summary>
  [HttpGet("user/{userId:guid}")]
  public async Task<ActionResult<IEnumerable<SubscriptionResponse>>> GetUserSubscriptions(Guid userId, CancellationToken cancellationToken) {
    var subscriptions = await _subscriptionRepository.GetByUserIdAsync(userId, cancellationToken);

    return Ok(subscriptions.Select(MapToResponse));
  }

  /// <summary> Activate a subscription (transition from trial or pending) </summary>
  [HttpPost("{id:guid}/activate")]
  public async Task<ActionResult> ActivateSubscription(Guid id, CancellationToken cancellationToken) {
    var subscription = await _subscriptionRepository.GetByIdAsync(id, cancellationToken);

    if (subscription == null) return NotFound();

    subscription.Activate();
    await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
    await _subscriptionRepository.SaveChangesAsync(cancellationToken);

    return NoContent();
  }

  /// <summary> Start trial for a subscription </summary>
  [HttpPost("{id:guid}/start-trial")]
  public async Task<ActionResult> StartTrial(Guid id, [FromBody] StartTrialRequest request, CancellationToken cancellationToken) {
    var subscription = await _subscriptionRepository.GetByIdAsync(id, cancellationToken);

    if (subscription == null) return NotFound();

    subscription.StartTrial(request.TrialEndDate);
    await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
    await _subscriptionRepository.SaveChangesAsync(cancellationToken);

    return NoContent();
  }

  private static SubscriptionResponse MapToResponse(UserSubscription subscription) {
    return new SubscriptionResponse {
      Id = subscription.Id,
      UserId = subscription.UserId,
      SubscriptionPlanId = subscription.SubscriptionPlanId,
      Status = subscription.Status,
      BillingCycle = subscription.BillingCycle,
      Amount = subscription.Amount,
      AutoRenew = subscription.AutoRenew,
      BillingCycleCount = subscription.BillingCycleCount,
      CurrentPeriodStart = subscription.CurrentPeriodStart,
      CurrentPeriodEnd = subscription.CurrentPeriodEnd,
      NextBillingAt = subscription.NextBillingAt,
      TrialEndsAt = subscription.TrialEndsAt,
      CanceledAt = subscription.CanceledAt,
      EndsAt = subscription.EndsAt,
      LastPaymentAt = subscription.LastPaymentAt,
      CancellationReason = subscription.CancellationReason,
      CancellationNote = subscription.CancellationNote,
      ExternalSubscriptionId = subscription.ExternalSubscriptionId,
      ExternalCustomerId = subscription.ExternalCustomerId,
      IsActive = subscription.IsActive,
      IsTrialing = subscription.IsTrialing,
      IsCancelled = subscription.IsCancelled,
      RemainingTrialDays = subscription.GetRemainingTrialDays(),
      DaysUntilNextBilling = subscription.GetDaysUntilNextBilling(),
      CreatedAt = subscription.CreatedAt,
      UpdatedAt = subscription.UpdatedAt,
    };
  }
}

// DTOs

public class CreateSubscriptionRequest {
  public Guid UserId { get; set; }

  public Guid SubscriptionPlanId { get; set; }

  public BillingCycle BillingCycle { get; set; }

  public decimal Amount { get; set; }

  public string? Currency { get; set; }

  public DateTime? StartDate { get; set; }

  public int? TrialDays { get; set; }
}

public class CancelSubscriptionRequest {
  public CancellationReason Reason { get; set; }

  public string? Note { get; set; }

  public DateTime? EffectiveDate { get; set; }
}

public class StartTrialRequest {
  public DateTime TrialEndDate { get; set; }
}

public class SubscriptionResponse {
  public Guid Id { get; set; }

  public Guid UserId { get; set; }

  public Guid SubscriptionPlanId { get; set; }

  public SubscriptionStatus Status { get; set; }

  public BillingCycle BillingCycle { get; set; }

  public Money Amount { get; set; } = Money.Zero();

  public bool AutoRenew { get; set; }

  public int BillingCycleCount { get; set; }

  public DateTime CurrentPeriodStart { get; set; }

  public DateTime CurrentPeriodEnd { get; set; }

  public DateTime? NextBillingAt { get; set; }

  public DateTime? TrialEndsAt { get; set; }

  public DateTime? CanceledAt { get; set; }

  public DateTime? EndsAt { get; set; }

  public DateTime? LastPaymentAt { get; set; }

  public CancellationReason? CancellationReason { get; set; }

  public string? CancellationNote { get; set; }

  public string? ExternalSubscriptionId { get; set; }

  public string? ExternalCustomerId { get; set; }

  public bool IsActive { get; set; }

  public bool IsTrialing { get; set; }

  public bool IsCancelled { get; set; }

  public int? RemainingTrialDays { get; set; }

  public int DaysUntilNextBilling { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime UpdatedAt { get; set; }
}
