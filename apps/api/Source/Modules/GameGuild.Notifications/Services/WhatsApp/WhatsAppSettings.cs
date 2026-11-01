using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace GameGuild.Notifications.Services.WhatsApp;

/// <summary>
///     Configuration for the WhatsApp Business Cloud API integration (issues #54/#55),
///     bound from <c>Notifications:WhatsApp</c>. Fail-closed by default: the channel is
///     disabled until an operator opts in, and enabling it without complete credentials
///     (resolved exclusively from the WHATSAPP_APP_ID, WHATSAPP_PHONE_NUMBER_ID and
///     WHATSAPP_ACCESS_TOKEN environment variables) fails startup validation. Secret
///     values are never accepted from configuration files or source code.
/// </summary>
public sealed class WhatsAppSettings
{
    /// <summary>Configuration section name (<c>Notifications:WhatsApp</c>).</summary>
    public const string SectionName = "Notifications:WhatsApp";

    /// <summary>
    ///     Environment variable holding the WhatsApp app id. Credentials are read from
    ///     the environment only — never from config files, source or tests.
    /// </summary>
    public const string AppIdEnvironmentVariable = "WHATSAPP_APP_ID";

    /// <summary>Environment variable holding the sending phone number id.</summary>
    public const string PhoneNumberIdEnvironmentVariable = "WHATSAPP_PHONE_NUMBER_ID";

    /// <summary>Environment variable holding the permanent access token.</summary>
    public const string AccessTokenEnvironmentVariable = "WHATSAPP_ACCESS_TOKEN";

    /// <summary>
    ///     Whether the WhatsApp channel is available. Default false (fail-closed): the
    ///     <see cref="NoOpWhatsAppProvider"/> is served and every send is declined.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     Cloud API version segment (e.g. <c>v21.0</c>). Kept configurable so operators
    ///     can pin the graph version they validated against.
    /// </summary>
    [Required]
    public string ApiVersion { get; set; } = "v21.0";

    /// <summary>
    ///     Graph API base URL. Defaults to the public Meta endpoint; validated against
    ///     server-side request rules (http/https, no localhost/loopback/private/reserved
    ///     hosts) before any call is made.
    /// </summary>
    [Required]
    public string ApiBaseUrl { get; set; } = "https://graph.facebook.com";

    /// <summary>
    ///     Template names allowed for business-initiated sends. Empty allowlist means no
    ///     template may be sent (fail-closed), because WhatsApp requires pre-approved
    ///     templates for messages outside a customer service window.
    /// </summary>
    public HashSet<string> TemplateAllowlist { get; set; } = [];

    /// <summary>Whether a template name passes the configured allowlist.</summary>
    public bool IsTemplateAllowed(string templateName) =>
        !string.IsNullOrWhiteSpace(templateName)
        && TemplateAllowlist.Contains(templateName.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Resolves the resolved base endpoint (no trailing slash). Exposed for the
    ///     provider and diagnostics; the endpoint guard still validates every use.
    /// </summary>
    public string ResolveBaseUrl() => ApiBaseUrl.TrimEnd('/');
}

/// <summary>
///     Credentials resolved exclusively from environment variables. Instances never hold
///     values that appeared in configuration files or source code.
/// </summary>
/// <param name="AppId">WhatsApp app id (WHATSAPP_APP_ID).</param>
/// <param name="PhoneNumberId">Sending phone number id (WHATSAPP_PHONE_NUMBER_ID).</param>
/// <param name="AccessToken">Permanent system-user access token (WHATSAPP_ACCESS_TOKEN).</param>
public sealed record WhatsAppCredentials(string AppId, string PhoneNumberId, string AccessToken)
{
    /// <summary>
    ///     Reads the three credential variables through an injectable reader (defaults to
    ///     the process environment). Returns null unless every value is present and
    ///     non-blank — partial credentials never produce a half-configured adapter.
    /// </summary>
    public static WhatsAppCredentials? FromEnvironment(Func<string, string?>? reader = null)
    {
        reader ??= Environment.GetEnvironmentVariable;

        var appId = reader(WhatsAppSettings.AppIdEnvironmentVariable);
        var phoneNumberId = reader(WhatsAppSettings.PhoneNumberIdEnvironmentVariable);
        var accessToken = reader(WhatsAppSettings.AccessTokenEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(appId)
            || string.IsNullOrWhiteSpace(phoneNumberId)
            || string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        return new WhatsAppCredentials(appId.Trim(), phoneNumberId.Trim(), accessToken.Trim());
    }
}

/// <summary>
///     Startup validation for <see cref="WhatsAppSettings"/>. Fail-closed: enabling the
///     channel without complete environment credentials, or with an invalid API base URL,
///     fails fast instead of surfacing at the first send.
/// </summary>
public sealed class WhatsAppSettingsValidator : IValidateOptions<WhatsAppSettings>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, WhatsAppSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.Enabled && WhatsAppCredentials.FromEnvironment() is null)
        {
            failures.Add(
                $"WhatsApp is enabled but credentials are incomplete. Set the {WhatsAppSettings.AppIdEnvironmentVariable}, "
                + $"{WhatsAppSettings.PhoneNumberIdEnvironmentVariable} and {WhatsAppSettings.AccessTokenEnvironmentVariable} "
                + "environment variables, or disable Notifications:WhatsApp:Enabled.");
        }

        if (!Uri.TryCreate(options.ApiBaseUrl, UriKind.Absolute, out var baseUri))
        {
            failures.Add($"{nameof(WhatsAppSettings.ApiBaseUrl)} must be an absolute URL.");
        }
        else if (baseUri.Scheme is not "http" and not "https")
        {
            failures.Add($"{nameof(WhatsAppSettings.ApiBaseUrl)} must use http or https.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiVersion))
        {
            failures.Add($"{nameof(WhatsAppSettings.ApiVersion)} is required.");
        }
        else if (!options.ApiVersion.StartsWith('v')
            || !options.ApiVersion.Skip(1).All(static c => char.IsDigit(c) || c == '.')
            || !options.ApiVersion.Contains('.'))
        {
            failures.Add($"{nameof(WhatsAppSettings.ApiVersion)} must look like 'v21.0'.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
