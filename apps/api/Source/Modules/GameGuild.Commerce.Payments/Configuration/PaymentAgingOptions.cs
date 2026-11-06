using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Options for the pending-payment aging boundary (issue #1202 gaps N6), the automated
///     exit for outcome-unknown payments left stuck in <see cref="PaymentStatus.Pending" />.
///     Mirrors the sibling retry-queue worker's opt-in rollout: disabled by default.
/// </summary>
public sealed class PaymentAgingOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Payments:Aging";

    /// <summary>
    ///     Whether the aging sweep runs. Default <c>false</c> (opt-in), matching the sibling
    ///     retry-queue and revenue-auditing workers.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>Minutes between aging sweep cycles. Default 60 (hourly).</summary>
    [Range(1, 1440)]
    public int PollIntervalMinutes { get; set; } = 60;

    /// <summary>
    ///     Age a payment must reach while Pending before the sweep exits it as failed with
    ///     reason. Default 48 hours — long enough for every webhook redelivery and
    ///     reconciliation window to have had its chance.
    /// </summary>
    [Range(1, 720)]
    public int StuckPendingTimeoutHours { get; set; } = 48;

    /// <summary>Maximum payments exited per sweep cycle. Default 100.</summary>
    [Range(1, 500)]
    public int MaxBatchSize { get; set; } = 100;
}

/// <summary>Startup validation for <see cref="PaymentAgingOptions" />.</summary>
public sealed class PaymentAgingOptionsValidator : IValidateOptions<PaymentAgingOptions>
{
    public ValidateOptionsResult Validate(string? name, PaymentAgingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.PollIntervalMinutes is < 1 or > 1440)
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(PaymentAgingOptions.PollIntervalMinutes)} must be between 1 and 1440 minutes.");
        }

        if (options.StuckPendingTimeoutHours is < 1 or > 720)
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(PaymentAgingOptions.StuckPendingTimeoutHours)} must be between 1 and 720 hours.");
        }

        if (options.MaxBatchSize is < 1 or > 500)
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(PaymentAgingOptions.MaxBatchSize)} must be between 1 and 500.");
        }

        return ValidateOptionsResult.Success;
    }
}
