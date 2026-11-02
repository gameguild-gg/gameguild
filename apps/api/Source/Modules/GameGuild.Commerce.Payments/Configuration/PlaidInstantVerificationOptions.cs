using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Configuration for Plaid instant bank-account verification (issue #414, optional
///     acceptance box), bound from <c>Payments:Plaid</c>. Fail-closed by default: instant
///     verification is disabled until an operator opts in with credentials, and enabling it
///     without credentials fails startup validation. Until then the
///     <see cref="IPlaidInstantVerification"/> port serves the NoOp implementation and US
///     bank accounts verify through the micro-deposit challenge (issue #1030 flow).
/// </summary>
public sealed class PlaidInstantVerificationOptions
{
    /// <summary>Configuration section name (<c>Payments:Plaid</c>).</summary>
    public const string SectionName = "Payments:Plaid";

    /// <summary>
    ///     Whether Plaid instant verification is available. Default false (fail-closed):
    ///     the NoOp implementation is served and callers fall back to micro-deposits.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    ///     Plaid environment: <c>sandbox</c> (default) or <c>production</c>. The sandbox
    ///     default means a misconfigured deployment can only ever reach the sandbox API.
    /// </summary>
    [Required]
    public string Environment { get; set; } = "sandbox";

    /// <summary>Plaid API client id; required when enabled (real client secret pending).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Plaid API client secret; required when enabled (real client secret pending).</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    ///     Optional explicit API base URL (for tests or proxies). Empty derives the base
    ///     URL from <see cref="Environment"/>.
    /// </summary>
    public string ApiBaseUrl { get; set; } = string.Empty;

    /// <summary>Plaid link-token products; fixed to the verification use case.</summary>
    public string[] Products { get; set; } = ["auth"];

    /// <summary>
    ///     Resolves the API base URL: the explicit override when set, otherwise the
    ///     sandbox/production Plaid endpoint. Unknown environments resolve to the sandbox
    ///     endpoint (fail-closed to the safer side).
    /// </summary>
    public string ResolveBaseUrl() =>
        !string.IsNullOrWhiteSpace(ApiBaseUrl)
            ? ApiBaseUrl.TrimEnd('/')
            : string.Equals(Environment, "production", StringComparison.OrdinalIgnoreCase)
                ? "https://production.plaid.com"
                : "https://sandbox.plaid.com";

    /// <summary>The environment label surfaced in logs ("sandbox" or "production").</summary>
    public string EnvironmentLabel =>
        string.Equals(Environment, "production", StringComparison.OrdinalIgnoreCase) ? "production" : "sandbox";
}

/// <summary>
///     Startup validation for <see cref="PlaidInstantVerificationOptions"/>. Fail-closed:
///     enabling instant verification without credentials (or with an invalid environment or
///     base URL) fails fast instead of surfacing at the first link-token request.
/// </summary>
public sealed class PlaidInstantVerificationOptionsValidator : IValidateOptions<PlaidInstantVerificationOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, PlaidInstantVerificationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (!string.Equals(options.Environment, "sandbox", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(options.Environment, "production", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{nameof(PlaidInstantVerificationOptions.Environment)} must be 'sandbox' or 'production'.");
        }

        if (options.IsEnabled)
        {
            if (string.IsNullOrWhiteSpace(options.ClientId))
            {
                failures.Add(
                    $"{nameof(PlaidInstantVerificationOptions.ClientId)} is required when Plaid instant verification is enabled.");
            }

            if (string.IsNullOrWhiteSpace(options.ClientSecret))
            {
                failures.Add(
                    $"{nameof(PlaidInstantVerificationOptions.ClientSecret)} is required when Plaid instant verification is enabled.");
            }
        }

        if (!string.IsNullOrWhiteSpace(options.ApiBaseUrl))
        {
            if (!Uri.TryCreate(options.ApiBaseUrl, UriKind.Absolute, out var baseUri))
            {
                failures.Add($"{nameof(PlaidInstantVerificationOptions.ApiBaseUrl)} must be an absolute URL when set.");
            }
            else if (baseUri.Scheme is not "http" and not "https")
            {
                failures.Add($"{nameof(PlaidInstantVerificationOptions.ApiBaseUrl)} must use http or https.");
            }
        }

        if (options.Products is null || options.Products.Length == 0)
        {
            failures.Add($"{nameof(PlaidInstantVerificationOptions.Products)} must contain at least one Plaid product.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
