using GameGuild.ValueObjects;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Abstraction for resolving subscription plan pricing.
///     This interface enables decoupling between Payments and Subscriptions modules
///     by allowing the Subscriptions module to implement this interface without
///     creating a circular dependency.
/// </summary>
/// <remarks>
///     The implementation is registered by the Subscriptions module during DI configuration.
///     This follows the Dependency Inversion Principle - the Payments module depends on an
///     abstraction it owns, not on the concrete Subscriptions implementation.
/// </remarks>
public interface IPlanPricingResolver
{
    /// <summary>
    ///     Gets the monthly price for a subscription plan.
    /// </summary>
    /// <param name="planId">The subscription plan ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The monthly price, or null if the plan doesn't exist</returns>
    Task<Money?> GetPlanMonthlyPriceAsync(Guid planId, CancellationToken cancellationToken = default);
    
    /// <summary>
    ///     Gets the price for a subscription plan based on billing cycle.
    /// </summary>
    /// <param name="planId">The subscription plan ID</param>
    /// <param name="billingCycle">The billing cycle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The price for the specified billing cycle, or null if the plan doesn't exist</returns>
    Task<Money?> GetPlanPriceAsync(Guid planId, BillingCycle billingCycle, CancellationToken cancellationToken = default);
    
    /// <summary>
    ///     Checks if a plan exists.
    /// </summary>
    /// <param name="planId">The subscription plan ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the plan exists</returns>
    Task<bool> PlanExistsAsync(Guid planId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Null implementation of IPlanPricingResolver for when the Subscriptions module is not available.
///     Returns null/false for all queries, allowing graceful degradation.
/// </summary>
public sealed class NullPlanPricingResolver : IPlanPricingResolver
{
    public Task<Money?> GetPlanMonthlyPriceAsync(Guid planId, CancellationToken cancellationToken = default)
        => Task.FromResult<Money?>(null);

    public Task<Money?> GetPlanPriceAsync(Guid planId, BillingCycle billingCycle, CancellationToken cancellationToken = default)
        => Task.FromResult<Money?>(null);

    public Task<bool> PlanExistsAsync(Guid planId, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
