using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace GameGuild.Notifications.Services.Email;

/// <summary>
/// Creates and validates signed one-click unsubscribe tokens.
/// Tokens carry the payload (userId, scope, optional value) encrypted with ASP.NET Core DataProtection
/// (purpose "notification-unsubscribe-v1") and are URL-safe base64 — the userId never appears in cleartext.
/// No expiry by design (recorded decision A5); tampered/expired tokens are indistinguishable and yield a single Invalid result.
/// </summary>
public interface IUnsubscribeTokenService
{
    /// <summary>
    /// Generates a signed, URL-safe unsubscribe token for the given scope ("type", "category" or "all") and optional value.
    /// </summary>
    string Generate(Guid userId, string scope, string? value);

    /// <summary>
    /// Validates a token. Tampered, malformed or wrong-purpose tokens all return <see cref="UnsubscribeTokenResult.IsValid"/> == false.
    /// </summary>
    UnsubscribeTokenResult Validate(string token);
}

/// <inheritdoc />
public sealed class UnsubscribeTokenService(IDataProtectionProvider protectionProvider) : IUnsubscribeTokenService
{
    private const string Purpose = "notification-unsubscribe-v1";

    private readonly IDataProtector _protector = protectionProvider.CreateProtector(Purpose);

    public string Generate(Guid userId, string scope, string? value)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new UnsubscribeTokenPayload(userId, scope, value));
        var protectedPayload = _protector.Protect(payload);
        return WebEncoders.Base64UrlEncode(protectedPayload);
    }

    public UnsubscribeTokenResult Validate(string token)
    {
        try
        {
            var protectedPayload = WebEncoders.Base64UrlDecode(token);
            var payload = _protector.Unprotect(protectedPayload);
            var parsed = JsonSerializer.Deserialize<UnsubscribeTokenPayload>(payload);
            if (parsed is null || parsed.UserId == Guid.Empty || string.IsNullOrEmpty(parsed.Scope))
            {
                return UnsubscribeTokenResult.Invalid();
            }

            return UnsubscribeTokenResult.Valid(parsed.UserId, parsed.Scope, parsed.Value);
        }
        catch
        {
            // FormatException (bad base64), CryptographicException (tampered/wrong purpose) and JSON errors
            // are deliberately indistinguishable: a single generic Invalid result, no detail leaked.
            return UnsubscribeTokenResult.Invalid();
        }
    }

    private sealed record UnsubscribeTokenPayload(Guid UserId, string Scope, string? Value);
}

/// <summary>
/// Result of validating an unsubscribe token. Failure has exactly one shape (Invalid) —
/// expired, tampered and malformed tokens cannot be distinguished by design.
/// </summary>
public sealed record UnsubscribeTokenResult
{
    private UnsubscribeTokenResult(bool isValid, Guid userId, string scope, string? value)
    {
        IsValid = isValid;
        UserId = userId;
        Scope = scope;
        Value = value;
    }

    /// <summary>Whether the token is valid and the payload was decoded.</summary>
    public bool IsValid { get; }

    /// <summary>User the token was issued for (Guid.Empty when invalid).</summary>
    public Guid UserId { get; }

    /// <summary>Unsubscribe scope: "type", "category" or "all" (empty when invalid).</summary>
    public string Scope { get; }

    /// <summary>Optional scope value (type or category name; null for scope "all").</summary>
    public string? Value { get; }

    public static UnsubscribeTokenResult Valid(Guid userId, string scope, string? value) =>
        new(true, userId, scope, value);

    public static UnsubscribeTokenResult Invalid() =>
        new(false, Guid.Empty, string.Empty, null);
}
