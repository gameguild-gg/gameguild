using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameGuild.Modules.Authentication;

/// <summary>
/// JSON converter for polymorphic credential deserialization
/// </summary>
public class PolymorphicCredentialConverter : JsonConverter<ICredentialData>
{
    public override ICredentialData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Determine credential type from JSON structure
        if (root.TryGetProperty("type", out var typeElement))
        {
            var credentialType = typeElement.GetString()?.ToLowerInvariant();

            return credentialType switch
            {
                "email" => JsonSerializer.Deserialize<EmailCredentialData>(root.GetRawText(), options),
                "phone" => JsonSerializer.Deserialize<PhoneCredentialData>(root.GetRawText(), options),
                "username" => JsonSerializer.Deserialize<UsernameCredentialData>(root.GetRawText(), options),
                "oauth" => JsonSerializer.Deserialize<OAuthCredentialData>(root.GetRawText(), options),
                "web3" => JsonSerializer.Deserialize<Web3CredentialData>(root.GetRawText(), options),
                _ => throw new JsonException($"Unknown credential type: {credentialType}")
            };
        }

        // Attempt auto-detection based on properties
        if (root.TryGetProperty("email", out _))
            return JsonSerializer.Deserialize<EmailCredentialData>(root.GetRawText(), options);

        if (root.TryGetProperty("phoneNumber", out _))
            return JsonSerializer.Deserialize<PhoneCredentialData>(root.GetRawText(), options);

        if (root.TryGetProperty("username", out _))
            return JsonSerializer.Deserialize<UsernameCredentialData>(root.GetRawText(), options);

        if (root.TryGetProperty("provider", out _))
            return JsonSerializer.Deserialize<OAuthCredentialData>(root.GetRawText(), options);

        if (root.TryGetProperty("walletAddress", out _))
            return JsonSerializer.Deserialize<Web3CredentialData>(root.GetRawText(), options);

        throw new JsonException("Unable to determine credential type from JSON structure");
    }

    public override void Write(Utf8JsonWriter writer, ICredentialData value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}

/// <summary>
/// Base interface for credential data
/// </summary>
public interface ICredentialData
{
    string Type { get; }
}

/// <summary>
/// Email credential data
/// </summary>
public class EmailCredentialData : ICredentialData
{
    public string Type => "email";
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Phone credential data
/// </summary>
public class PhoneCredentialData : ICredentialData
{
    public string Type => "phone";
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Username credential data
/// </summary>
public class UsernameCredentialData : ICredentialData
{
    public string Type => "username";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// OAuth credential data
/// </summary>
public class OAuthCredentialData : ICredentialData
{
    public string Type => "oauth";
    public string Provider { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Web3 wallet credential data
/// </summary>
public class Web3CredentialData : ICredentialData
{
    public string Type => "web3";
    public string WalletAddress { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string Challenge { get; set; } = string.Empty;
}
