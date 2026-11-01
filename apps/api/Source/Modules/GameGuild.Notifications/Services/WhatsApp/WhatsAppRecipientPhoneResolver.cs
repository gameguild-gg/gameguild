using System.Text.Json;
using GameGuild.Identity.Users;
using GameGuild.Notifications.Services.Email;

namespace GameGuild.Notifications.Services.WhatsApp;

/// <summary>
///     Resolves the WhatsApp destination number for a notification: the metadata phone
///     (protected envelope for transactional types) when present, otherwise the user
///     profile phone — the WhatsApp twin of <see cref="Email.RecipientEmailResolver"/>.
/// </summary>
public interface IWhatsAppRecipientPhoneResolver
{
    /// <summary>Resolves the E.164 destination number, or null when none is known.</summary>
    Task<string?> ResolveAsync(Notification notification, CancellationToken cancellationToken = default);
}

/// <summary>
///     Default resolver: notification metadata first (same JSON contract as the SMS
///     pipeline: <c>{"phoneNumber":"+15551234567"}</c>, protected for transactional
///     types), user profile fallback through <see cref="IUserRepository"/> (the seam
///     this module already references).
/// </summary>
public sealed class WhatsAppRecipientPhoneResolver(
    IUserRepository userRepository,
    NotificationMetadataProtector metadataProtector) : IWhatsAppRecipientPhoneResolver
{
    private static readonly JsonSerializerOptions MetadataJsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<string?> ResolveAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var metadataPhone = ReadMetadataPhone(notification, metadataProtector);
        if (!string.IsNullOrWhiteSpace(metadataPhone))
        {
            return metadataPhone;
        }

        if (notification.RecipientId is { } userId)
        {
            var user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(user?.PhoneNumber))
            {
                return user.PhoneNumber.Trim();
            }
        }

        return null;
    }

    /// <summary>
    ///     Reads the destination phone from notification metadata, decrypting the
    ///     protected envelope for transactional types. Returns null for missing or
    ///     malformed payloads.
    /// </summary>
    internal static string? ReadMetadataPhone(Notification notification, NotificationMetadataProtector metadataProtector)
    {
        string? metadata;
        try
        {
            metadata = NotificationMetadataProtector.RequiresProtection(notification.Type)
                ? metadataProtector.GetForRendering(notification, notification.Type)
                : notification.Metadata;
        }
        catch (Exception exception) when (exception is System.Security.Cryptography.CryptographicException or FormatException)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(metadata))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<MetadataContract>(metadata, MetadataJsonOptions);
            return string.IsNullOrWhiteSpace(payload?.PhoneNumber) ? null : payload.PhoneNumber.Trim();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record MetadataContract(string? PhoneNumber);
}
