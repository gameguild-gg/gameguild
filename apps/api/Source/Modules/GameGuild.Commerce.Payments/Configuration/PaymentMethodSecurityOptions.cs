using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Security policy for self-service payment-method management (issue #414). All knobs
///     fail closed to the historic behavior when unset: MFA step-up is opt-in so existing
///     deployments keep working until they enable it.
/// </summary>
public sealed class PaymentMethodSecurityOptions
{
    /// <summary>Configuration section name (<c>Payments:PaymentMethodSecurity</c>).</summary>
    public const string SectionName = "Payments:PaymentMethodSecurity";

    /// <summary>
    ///     When true, every mutating payment-method endpoint (register, update display fields,
    ///     set-default, remove, bank-account verification) requires an MFA-verified session
    ///     (<c>mfa_verified</c> on the actor context) for non-system-admin actors. System
    ///     administrators act through server-side flows and are exempt. Default false
    ///     (historic behavior).
    /// </summary>
    public bool RequireMfaForChanges { get; set; }
}

/// <summary>Validates <see cref="PaymentMethodSecurityOptions"/> at startup.</summary>
public sealed class PaymentMethodSecurityOptionsValidator : IValidateOptions<PaymentMethodSecurityOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, PaymentMethodSecurityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return ValidateOptionsResult.Success;
    }
}
