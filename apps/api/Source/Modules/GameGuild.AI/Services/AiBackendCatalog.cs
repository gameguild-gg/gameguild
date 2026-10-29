using GameGuild;
using Microsoft.Extensions.Options;

namespace GameGuild.AI;

internal static class AiAdapterKeys
{
    public const string OpenAi = "OpenAi";
    public const string StandardAi = "StandardAi";
    public const string Anthropic = "Anthropic";
    public const string Google = "Google";
}

internal sealed class AiAdapterRegistry
{
    private readonly Dictionary<string, IAiAdapter> _adapters = new(StringComparer.OrdinalIgnoreCase);

    public AiAdapterRegistry(IEnumerable<IAiAdapter> adapters)
    {
        foreach (var adapter in adapters)
        {
            if (string.IsNullOrWhiteSpace(adapter.Key) ||
                !string.Equals(adapter.Key, adapter.Descriptor.Key, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Every AI adapter must expose one matching, non-empty key.");
            }

            if (!_adapters.TryAdd(adapter.Key.Trim(), adapter))
            {
                throw new InvalidOperationException($"AI adapter key '{adapter.Key.Trim()}' is registered more than once.");
            }
        }
    }

    public bool TryGet(string key, out IAiAdapter adapter) => _adapters.TryGetValue(key, out adapter!);
}

internal sealed record AiBackendResolution(
    string Backend,
    string Adapter,
    string Model,
    string? ApiKey,
    string BaseUrl,
    AiAdapterDescriptor Descriptor);

internal sealed class AiBackendCatalog(
    IOptions<AiOptions> options,
    AiAdapterRegistry adapterRegistry) : IAiBackendCatalog
{
    private readonly AiOptions _options = options.Value;

    public AiBackendReadinessDto GetReadiness(string? backend = null, string? model = null)
    {
        var resolution = Resolve(backend, model, null);
        return resolution.IsSuccess
            ? new AiBackendReadinessDto(
                true,
                Backend: resolution.Value.Backend,
                Adapter: resolution.Value.Adapter,
                Model: resolution.Value.Model)
            : new AiBackendReadinessDto(false, resolution.Error.Code, resolution.Error.Description);
    }

    public IReadOnlyList<AiBackendStatusDto> GetStatus()
    {
        var backends = _options.Backends ?? new Dictionary<string, AiBackendOptions>();
        return backends
            .OrderBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Select(entry => BuildStatus(entry.Key, entry.Value))
            .ToArray();
    }

    internal Result<AiBackendResolution> Resolve(
        string? requestedBackend,
        string? requestedModel,
        string? apiKeyOverride)
    {
        if (!_options.Enabled)
        {
            return Result.Failure<AiBackendResolution>(
                Error.Forbidden("AI.Disabled", "AI functionality is disabled for this environment."));
        }

        var backendName = string.IsNullOrWhiteSpace(requestedBackend)
            ? _options.DefaultBackend
            : requestedBackend;
        if (string.IsNullOrWhiteSpace(backendName))
        {
            return Result.Failure<AiBackendResolution>(Error.Problem(
                "AI.BackendNotConfigured",
                "No default AI backend is configured for this environment."));
        }

        if (!TryGetBackend(backendName, out var canonicalBackend, out var backendOptions))
        {
            return Result.Failure<AiBackendResolution>(Error.Validation(
                "AI.BackendInvalid",
                $"Backend '{backendName.Trim()}' is not configured."));
        }

        if (string.IsNullOrWhiteSpace(backendOptions.Adapter))
        {
            return Result.Failure<AiBackendResolution>(Error.Problem(
                "AI.AdapterNotConfigured",
                $"Backend '{canonicalBackend}' has no adapter configured."));
        }

        var adapterKey = backendOptions.Adapter.Trim();
        if (!adapterRegistry.TryGet(adapterKey, out var adapter))
        {
            return Result.Failure<AiBackendResolution>(Error.Problem(
                "AI.AdapterNotRegistered",
                $"Adapter '{adapterKey}' for backend '{canonicalBackend}' is not registered."));
        }

        var model = string.IsNullOrWhiteSpace(requestedModel)
            ? backendOptions.DefaultModel
            : requestedModel;
        if (string.IsNullOrWhiteSpace(model))
        {
            return Result.Failure<AiBackendResolution>(Error.Problem(
                "AI.ModelNotConfigured",
                $"No default model is configured for backend '{canonicalBackend}'."));
        }

        var configuredBaseUrl = string.IsNullOrWhiteSpace(backendOptions.BaseUrl)
            ? null
            : backendOptions.BaseUrl.Trim();
        if (adapter.Descriptor.RequiresExplicitBaseUrl && configuredBaseUrl is null)
        {
            return Result.Failure<AiBackendResolution>(Error.Problem(
                "AI.BaseUrlMissing",
                $"Backend '{canonicalBackend}' requires an explicit server URL."));
        }

        var baseUrl = configuredBaseUrl ?? adapter.Descriptor.DefaultBaseUrl;
        if (!TryNormalizeBaseUrl(baseUrl, out var normalizedBaseUrl))
        {
            return Result.Failure<AiBackendResolution>(Error.Validation(
                "AI.BaseUrlInvalid",
                $"Backend '{canonicalBackend}' has an invalid server URL."));
        }

        var apiKey = string.IsNullOrWhiteSpace(apiKeyOverride)
            ? backendOptions.ApiKey
            : apiKeyOverride;
        apiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();
        if (adapter.Descriptor.CredentialPolicy == AiCredentialPolicy.Required && apiKey is null)
        {
            return Result.Failure<AiBackendResolution>(Error.Problem(
                "AI.CredentialsMissing",
                $"Backend '{canonicalBackend}' requires a server credential."));
        }

        if (adapter.Descriptor.CredentialPolicy == AiCredentialPolicy.None)
        {
            apiKey = null;
        }

        return Result.Success(new AiBackendResolution(
            canonicalBackend,
            adapter.Key,
            model.Trim(),
            apiKey,
            normalizedBaseUrl,
            adapter.Descriptor));
    }

    private AiBackendStatusDto BuildStatus(string backend, AiBackendOptions backendOptions)
    {
        var readiness = Resolve(backend, null, null);
        var adapterKey = string.IsNullOrWhiteSpace(backendOptions.Adapter)
            ? null
            : backendOptions.Adapter.Trim();
        AiAdapterDescriptor? descriptor = null;
        if (adapterKey is not null && adapterRegistry.TryGet(adapterKey, out var adapter))
        {
            descriptor = adapter.Descriptor;
        }

        var baseUrl = string.IsNullOrWhiteSpace(backendOptions.BaseUrl)
            ? descriptor?.DefaultBaseUrl
            : backendOptions.BaseUrl.Trim();
        return new AiBackendStatusDto(
            backend,
            adapterKey,
            true,
            readiness.IsSuccess,
            readiness.IsFailure ? readiness.Error.Code : null,
            backendOptions.DefaultModel,
            baseUrl,
            !string.IsNullOrWhiteSpace(backendOptions.ApiKey),
            descriptor?.CredentialPolicy.ToString());
    }

    private bool TryGetBackend(
        string backend,
        out string canonicalBackend,
        out AiBackendOptions backendOptions)
    {
        var backends = _options.Backends ?? new Dictionary<string, AiBackendOptions>();
        foreach (var entry in backends)
        {
            if (!string.Equals(entry.Key, backend.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            canonicalBackend = entry.Key;
            backendOptions = entry.Value;
            return true;
        }

        canonicalBackend = string.Empty;
        backendOptions = new AiBackendOptions();
        return false;
    }

    private static bool TryNormalizeBaseUrl(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        normalized = value.Trim().TrimEnd('/');
        return true;
    }
}
