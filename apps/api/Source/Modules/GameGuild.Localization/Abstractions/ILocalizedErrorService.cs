using System.Globalization;

namespace GameGuild.Localization;

/// <summary>
/// Service for localizing error messages and system strings.
/// Provides a centralized way to get localized error messages for API responses.
/// </summary>
public interface ILocalizedErrorService
{
    /// <summary>
    /// Gets a localized error message by key.
    /// </summary>
    /// <param name="errorKey">The error message key (e.g., "validation.required", "auth.unauthorized")</param>
    /// <param name="args">Optional format arguments for the message</param>
    /// <returns>The localized error message</returns>
    string GetErrorMessage(string errorKey, params object[] args);

    /// <summary>
    /// Gets a localized error message for a specific culture.
    /// </summary>
    /// <param name="errorKey">The error message key</param>
    /// <param name="culture">The target culture</param>
    /// <param name="args">Optional format arguments for the message</param>
    /// <returns>The localized error message</returns>
    string GetErrorMessage(string errorKey, CultureInfo culture, params object[] args);

    /// <summary>
    /// Gets a localized validation error message.
    /// </summary>
    /// <param name="validationKey">The validation key (e.g., "required", "minLength", "email")</param>
    /// <param name="fieldName">The field name being validated</param>
    /// <param name="args">Optional format arguments</param>
    /// <returns>The localized validation message</returns>
    string GetValidationMessage(string validationKey, string fieldName, params object[] args);

    /// <summary>
    /// Gets a localized system message (non-error, informational).
    /// </summary>
    /// <param name="messageKey">The message key</param>
    /// <param name="args">Optional format arguments</param>
    /// <returns>The localized message</returns>
    string GetSystemMessage(string messageKey, params object[] args);

    /// <summary>
    /// Checks if a translation exists for the given key.
    /// </summary>
    /// <param name="key">The translation key</param>
    /// <returns>True if a translation exists</returns>
    bool HasTranslation(string key);
}

/// <summary>
/// Standard error message keys used throughout the system.
/// </summary>
public static class ErrorMessageKeys
{
    public static class Validation
    {
        public const string Required = "validation.required";
        public const string MinLength = "validation.minLength";
        public const string MaxLength = "validation.maxLength";
        public const string Email = "validation.email";
        public const string Range = "validation.range";
        public const string Regex = "validation.regex";
        public const string Comparison = "validation.comparison";
        public const string Unique = "validation.unique";
    }

    public static class Auth
    {
        public const string Unauthorized = "auth.unauthorized";
        public const string Forbidden = "auth.forbidden";
        public const string TokenExpired = "auth.tokenExpired";
        public const string InvalidCredentials = "auth.invalidCredentials";
        public const string AccountLocked = "auth.accountLocked";
        public const string AccountDisabled = "auth.accountDisabled";
        public const string SessionExpired = "auth.sessionExpired";
    }

    public static class Resource
    {
        public const string NotFound = "resource.notFound";
        public const string AlreadyExists = "resource.alreadyExists";
        public const string Conflict = "resource.conflict";
        public const string Deleted = "resource.deleted";
    }

    public static class Quota
    {
        public const string Exceeded = "quota.exceeded";
        public const string NearLimit = "quota.nearLimit";
        public const string StorageFull = "quota.storageFull";
    }

    public static class System
    {
        public const string InternalError = "system.internalError";
        public const string ServiceUnavailable = "system.serviceUnavailable";
        public const string MaintenanceMode = "system.maintenanceMode";
        public const string RateLimited = "system.rateLimited";
    }

    public static class Asset
    {
        public const string VirusDetected = "asset.virusDetected";
        public const string ModerationRejected = "asset.moderationRejected";
        public const string ContentWarning = "asset.contentWarning";
        public const string TokenExpired = "asset.tokenExpired";
        public const string TokenInvalid = "asset.tokenInvalid";
        public const string DownloadLimitExceeded = "asset.downloadLimitExceeded";
    }
}
