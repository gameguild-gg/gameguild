using System.Text.Json;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Serializes and merges tenant integration settings stored as JSON on <see cref="TenantSettings"/>.
/// </summary>
public static class TenantIntegrationSettingsSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     Deserialize a persisted integration settings payload.
    /// </summary>
    public static TenantIntegrationSettingsDto Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Empty();

        try
        {
            return JsonSerializer.Deserialize<TenantIntegrationSettingsDto>(json, SerializerOptions) ?? Empty();
        }
        catch (JsonException)
        {
            return Empty();
        }
    }

    /// <summary>
    ///     Serialize a tenant integration settings DTO for persistence.
    /// </summary>
    public static string Serialize(TenantIntegrationSettingsDto settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return JsonSerializer.Serialize(settings, SerializerOptions);
    }

    /// <summary>
    ///     Merge an update payload into an existing tenant integration settings payload.
    /// </summary>
    public static TenantIntegrationSettingsDto Merge(
        TenantIntegrationSettingsDto current,
        UpdateTenantIntegrationSettingsRequest update)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(update);

        return new TenantIntegrationSettingsDto(
            update.ExternalServices ?? current.ExternalServices,
            update.WebhookSettings ?? current.WebhookSettings,
            update.ApiKeys ?? current.ApiKeys,
            update.SsoConfiguration ?? current.SsoConfiguration);
    }

    /// <summary>
    ///     Create an empty integration settings payload.
    /// </summary>
    public static TenantIntegrationSettingsDto Empty()
        => new(
            new Dictionary<string, object?>(),
            new Dictionary<string, object?>(),
            new Dictionary<string, string>(),
            new Dictionary<string, object?>());
}