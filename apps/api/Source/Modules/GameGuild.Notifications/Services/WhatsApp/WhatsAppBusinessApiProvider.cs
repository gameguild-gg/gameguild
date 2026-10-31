using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Notifications.Services.WhatsApp;

/// <summary>
///     WhatsApp Business Cloud API adapter (issues #54/#55). Calls the Graph API
///     (<c>graph.facebook.com</c> by default) with credentials resolved exclusively from
///     the WHATSAPP_APP_ID / WHATSAPP_PHONE_NUMBER_ID / WHATSAPP_ACCESS_TOKEN environment
///     variables — never from configuration files or source code. Every request target is
///     validated by <see cref="WhatsAppEndpointGuard"/> (http/https, public hosts only)
///     before a connection is opened. Registered only when the channel is enabled; the
///     fail-closed default is <see cref="NoOpWhatsAppProvider"/>.
/// </summary>
public sealed class WhatsAppBusinessApiProvider(
    IOptions<WhatsAppSettings> settings,
    HttpClient httpClient,
    WhatsAppEndpointGuard endpointGuard,
    Func<string, string?>? environmentReader = null,
    ILogger<WhatsAppBusinessApiProvider>? logger = null) : IWhatsAppProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>WhatsApp hard cap on a text message body.</summary>
    private const int MaximumTextBodyLength = 4096;

    /// <summary>
    ///     Named HTTP client used by the composition root when it activates this adapter
    ///     (isolated module hosts register the NoOp default instead).
    /// </summary>
    public const string HttpClientName = "WhatsAppBusinessApi";

    private readonly ConcurrentDictionary<string, byte> validatedBaseUrls = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public bool IsConfigured =>
        settings.Value.Enabled && WhatsAppCredentials.FromEnvironment(environmentReader) is not null;

    /// <inheritdoc />
    public async Task<Result<WhatsAppSendResult>> SendMessageAsync(WhatsAppOutboundMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var to = NormalizePhoneNumber(message.To);
        if (to is null)
        {
            return Result.Failure<WhatsAppSendResult>(Error.Validation(
                "WhatsApp.InvalidRecipient", "The destination phone number must be in E.164 format (e.g. +15551234567)."));
        }

        var body = message.Body.Length > MaximumTextBodyLength
            ? message.Body[..MaximumTextBodyLength]
            : message.Body;

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to,
            type = "text",
            text = new { preview_url = false, body }
        };

        return await SendRequestAsync(to, payload, "text message", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<WhatsAppSendResult>> SendTemplateAsync(WhatsAppTemplateMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!settings.Value.IsTemplateAllowed(message.TemplateName))
        {
            return Result.Failure<WhatsAppSendResult>(Error.Validation(
                "WhatsApp.TemplateNotAllowed",
                $"Template '{message.TemplateName}' is not in the WhatsApp template allowlist (Notifications:WhatsApp:TemplateAllowlist)."));
        }

        var to = NormalizePhoneNumber(message.To);
        if (to is null)
        {
            return Result.Failure<WhatsAppSendResult>(Error.Validation(
                "WhatsApp.InvalidRecipient", "The destination phone number must be in E.164 format (e.g. +15551234567)."));
        }

        // Template variables are positional in the Cloud API; named placeholders map to
        // positions in ordinal (case-insensitive) key order, which is deterministic for
        // operators authoring {{1}}/{{2}} template slots.
        var parameters = (message.Placeholders ?? new Dictionary<string, string>())
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new { type = "text", text = pair.Value })
            .ToArray();

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to,
            type = "template",
            template = new
            {
                name = message.TemplateName.Trim(),
                language = new { code = string.IsNullOrWhiteSpace(message.LanguageCode) ? "en_US" : message.LanguageCode.Trim() },
                components = parameters.Length == 0
                    ? null
                    : new[] { new { type = "body", parameters } }
            }
        };

        return await SendRequestAsync(to, payload, $"template '{message.TemplateName.Trim()}'", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     The Cloud API offers no synchronous recipient-reachability probe (reachability
    ///     surfaces asynchronously through delivery status). Verification therefore
    ///     normalizes and format-checks the number locally, then confirms the sending
    ///     number and credentials are operational with a cheap authenticated read of the
    ///     phone-number node, failing closed when either half does not hold.
    /// </remarks>
    public async Task<Result<WhatsAppNumberVerification>> VerifyNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var to = NormalizePhoneNumber(phoneNumber);
        if (to is null)
        {
            return Result.Success(new WhatsAppNumberVerification(IsValid: false, CanonicalPhoneNumber: null));
        }

        var credentials = RequireCredentials();
        var baseUri = PrepareEndpoint(credentials);
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{baseUri}/{settings.Value.ApiVersion}/{credentials.PhoneNumberId}?fields=display_phone_number,verified_name,quality_rating");

        var sendResult = await ExecuteAsync(request, readContent: true, "number verification", cancellationToken).ConfigureAwait(false);
        if (sendResult.IsFailure)
        {
            return Result.Failure<WhatsAppNumberVerification>(sendResult.Error);
        }

        return Result.Success(new WhatsAppNumberVerification(IsValid: true, CanonicalPhoneNumber: "+" + to));
    }

    /// <inheritdoc />
    public async Task<Result<WhatsAppMessageStatus>> GetMessageStatusAsync(string providerMessageId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerMessageId))
        {
            return Result.Failure<WhatsAppMessageStatus>(Error.Validation(
                "WhatsApp.InvalidMessageId", "A provider message id is required to query delivery status."));
        }

        var credentials = RequireCredentials();
        var baseUri = PrepareEndpoint(credentials);
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{baseUri}/{settings.Value.ApiVersion}/{Uri.EscapeDataString(providerMessageId.Trim())}");

        var responseResult = await ExecuteAsync(request, readContent: true, "message status query", cancellationToken).ConfigureAwait(false);
        if (responseResult.IsFailure)
        {
            return Result.Failure<WhatsAppMessageStatus>(responseResult.Error);
        }

        try
        {
            using var document = JsonDocument.Parse(responseResult.Value);
            JsonElement? messageNode = document.RootElement.TryGetProperty("messages", out var messages)
                && messages.GetArrayLength() > 0
                ? messages[0]
                : null;

            var status = messageNode?.TryGetProperty("status", out var statusNode) == true
                ? statusNode.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(status))
            {
                return Result.Failure<WhatsAppMessageStatus>(Error.Problem(
                    "WhatsApp.StatusUnavailable", "The provider response did not include a delivery status for the message."));
            }

            string? errorCode = null;
            string? errorMessage = null;
            if (messageNode?.TryGetProperty("errors", out var errors) == true && errors.GetArrayLength() > 0)
            {
                errorCode = errors[0].TryGetProperty("code", out var codeNode) && codeNode.ValueKind == JsonValueKind.Number
                    ? codeNode.GetInt32().ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : null;
                errorMessage = errors[0].TryGetProperty("title", out var titleNode) ? titleNode.GetString() : null;
            }

            return Result.Success(new WhatsAppMessageStatus(status, errorCode, errorMessage));
        }
        catch (JsonException ex)
        {
            return Result.Failure<WhatsAppMessageStatus>(Error.Problem(
                "WhatsApp.InvalidProviderResponse", $"The provider returned a malformed status payload: {ex.Message}"));
        }
    }

    private async Task<Result<WhatsAppSendResult>> SendRequestAsync(string to, object payload, string operationLabel, CancellationToken cancellationToken)
    {
        var credentials = RequireCredentials();
        var baseUri = PrepareEndpoint(credentials);

        var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUri}/{settings.Value.ApiVersion}/{credentials.PhoneNumberId}/messages")
        {
            Content = content
        };

        var responseResult = await ExecuteAsync(request, readContent: true, operationLabel, cancellationToken).ConfigureAwait(false);
        if (responseResult.IsFailure)
        {
            return Result.Failure<WhatsAppSendResult>(responseResult.Error);
        }

        try
        {
            using var document = JsonDocument.Parse(responseResult.Value);
            if (!document.RootElement.TryGetProperty("messages", out var messages) || messages.GetArrayLength() == 0)
            {
                return Result.Failure<WhatsAppSendResult>(Error.Problem(
                    "WhatsApp.InvalidProviderResponse", "The provider accepted the request but returned no message id."));
            }

            var wamid = messages[0].TryGetProperty("id", out var idNode) ? idNode.GetString() : null;
            if (string.IsNullOrWhiteSpace(wamid))
            {
                return Result.Failure<WhatsAppSendResult>(Error.Problem(
                    "WhatsApp.InvalidProviderResponse", "The provider returned an empty message id."));
            }

            return Result.Success(new WhatsAppSendResult(wamid));
        }
        catch (JsonException ex)
        {
            return Result.Failure<WhatsAppSendResult>(Error.Problem(
                "WhatsApp.InvalidProviderResponse", $"The provider returned a malformed payload: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Sends a prepared request with bearer authentication. The authorization header is
    ///     built per call from the environment so credential rotation needs no restart, and
    ///     no credential material is ever logged or included in error text.
    /// </summary>
    private async Task<Result<string>> ExecuteAsync(HttpRequestMessage request, bool readContent, string operationLabel, CancellationToken cancellationToken)
    {
        var credentials = RequireCredentials();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            logger?.LogWarning(ex, "WhatsApp Cloud API request failed during {Operation}.", operationLabel);
            return Result.Failure<string>(Error.Problem(
                "WhatsApp.ProviderUnavailable", $"The WhatsApp Cloud API could not be reached during {operationLabel}."));
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger?.LogWarning(ex, "WhatsApp Cloud API request timed out during {Operation}.", operationLabel);
            return Result.Failure<string>(Error.Problem(
                "WhatsApp.ProviderTimeout", $"The WhatsApp Cloud API request timed out during {operationLabel}."));
        }

        using (response)
        {
            var body = readContent || !response.IsSuccessStatusCode
                ? await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)
                : string.Empty;

            if (!response.IsSuccessStatusCode)
            {
                logger?.LogWarning(
                    "WhatsApp Cloud API rejected {Operation} with status {StatusCode}.",
                    operationLabel, (int)response.StatusCode);
                return Result.Failure<string>(Error.Problem(
                    "WhatsApp.ProviderRejected",
                    $"The WhatsApp Cloud API rejected the {operationLabel} request with HTTP {(int)response.StatusCode} " +
                    $"and error '{ExtractProviderError(body) ?? "unspecified"}'."));
            }

            return Result.Success(body);
        }
    }

    private WhatsAppCredentials RequireCredentials()
    {
        if (!settings.Value.Enabled)
        {
            throw new InvalidOperationException(
                "The WhatsApp Business provider was invoked while the channel is disabled; the NoOp provider should be served instead.");
        }

        return WhatsAppCredentials.FromEnvironment(environmentReader)
            ?? throw new InvalidOperationException(
                $"WhatsApp credentials are incomplete; set {WhatsAppSettings.AppIdEnvironmentVariable}, "
                + $"{WhatsAppSettings.PhoneNumberIdEnvironmentVariable} and {WhatsAppSettings.AccessTokenEnvironmentVariable}.");
    }

    /// <summary>
    ///     Validates the configured base URL against the server-side request rules once per
    ///     unique endpoint per process and returns it without a trailing slash.
    /// </summary>
    private string PrepareEndpoint(WhatsAppCredentials credentials)
    {
        var baseUri = settings.Value.ResolveBaseUrl();
        validatedBaseUrls.GetOrAdd(baseUri, url =>
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed))
            {
                throw new InvalidOperationException("The configured WhatsApp API base URL is not a valid absolute URL.");
            }

            endpointGuard.ValidateBaseUri(parsed);
            return 0;
        });
        return baseUri;
    }

    private static string? ExtractProviderError(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            return document.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var message)
                ? message.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    ///     Normalizes E.164 input to the digits-only form the Cloud API expects.
    ///     Returns null for anything that is not a plausible E.164 number
    ///     (plus, 7-15 digits).
    /// </summary>
    internal static string? NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var trimmed = phoneNumber.Trim();
        if (trimmed.StartsWith('+'))
        {
            trimmed = trimmed[1..];
        }

        if (trimmed.Length is < 7 or > 15 || !trimmed.All(char.IsDigit))
        {
            return null;
        }

        return trimmed;
    }
}
