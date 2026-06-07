namespace GameGuild.Assets.Services;

/// <summary>
/// Provides localized messages for asset-related operations.
/// </summary>
public interface IAssetLocalizationService
{
    /// <summary>
    /// Gets a localized rejection reason for moderation labels.
    /// </summary>
    string GetModerationRejectionReason(string[] labels, string languageCode);

    /// <summary>
    /// Gets a localized access denied message based on policy.
    /// </summary>
    string GetAccessDeniedMessage(AssetAccessPolicy policy, string languageCode);

    /// <summary>
    /// Gets a localized quota exceeded message.
    /// </summary>
    string GetQuotaExceededMessage(string quotaType, long currentUsage, long limit, string languageCode);

    /// <summary>
    /// Gets a localized virus detected message.
    /// </summary>
    string GetVirusDetectedMessage(string fileName, string languageCode);

    /// <summary>
    /// Gets a localized upload failed message.
    /// </summary>
    string GetUploadFailedMessage(string reason, string languageCode);
}

/// <summary>
/// Implementation of asset localization using built-in fallback messages.
/// </summary>
public class AssetLocalizationService : IAssetLocalizationService
{
    // Fallback messages for different languages
    private static readonly Dictionary<string, Dictionary<string, string>> FallbackMessages = new()
    {
        ["en"] = new Dictionary<string, string>
        {
            ["moderation.rejected"] = "Content rejected due to policy violation: {0}",
            ["moderation.label.explicit"] = "explicit content",
            ["moderation.label.violence"] = "violent content",
            ["moderation.label.hate"] = "hateful content",
            ["moderation.label.spam"] = "spam",
            ["access.denied.private"] = "This asset is private and cannot be accessed.",
            ["access.denied.tenant"] = "This asset belongs to a different tenant.",
            ["access.denied.authenticated"] = "You must be authenticated to access this asset.",
            ["access.denied.owner"] = "Only the asset owner can access this resource.",
            ["quota.exceeded.assets"] = "You have reached your asset limit ({0}/{1} files).",
            ["quota.exceeded.storage"] = "You have exceeded your storage quota ({0}/{1} bytes).",
            ["virus.detected"] = "The file '{0}' contains malicious content and cannot be uploaded.",
            ["upload.failed.size"] = "File exceeds the maximum allowed size.",
            ["upload.failed.type"] = "This file type is not allowed.",
            ["upload.failed.generic"] = "Upload failed. Please try again."
        },
        ["es"] = new Dictionary<string, string>
        {
            ["moderation.rejected"] = "Contenido rechazado por violación de políticas: {0}",
            ["moderation.label.explicit"] = "contenido explícito",
            ["moderation.label.violence"] = "contenido violento",
            ["moderation.label.hate"] = "contenido de odio",
            ["moderation.label.spam"] = "spam",
            ["access.denied.private"] = "Este recurso es privado y no se puede acceder.",
            ["access.denied.tenant"] = "Este recurso pertenece a otro inquilino.",
            ["access.denied.authenticated"] = "Debe autenticarse para acceder a este recurso.",
            ["access.denied.owner"] = "Solo el propietario puede acceder a este recurso.",
            ["quota.exceeded.assets"] = "Ha alcanzado su límite de archivos ({0}/{1}).",
            ["quota.exceeded.storage"] = "Ha excedido su cuota de almacenamiento ({0}/{1} bytes).",
            ["virus.detected"] = "El archivo '{0}' contiene contenido malicioso.",
            ["upload.failed.size"] = "El archivo excede el tamaño máximo permitido.",
            ["upload.failed.type"] = "Este tipo de archivo no está permitido.",
            ["upload.failed.generic"] = "La carga falló. Por favor, intente de nuevo."
        },
        ["pt-BR"] = new Dictionary<string, string>
        {
            ["moderation.rejected"] = "Conteúdo rejeitado por violação de política: {0}",
            ["moderation.label.explicit"] = "conteúdo explícito",
            ["moderation.label.violence"] = "conteúdo violento",
            ["moderation.label.hate"] = "conteúdo de ódio",
            ["moderation.label.spam"] = "spam",
            ["access.denied.private"] = "Este recurso é privado e não pode ser acessado.",
            ["access.denied.tenant"] = "Este recurso pertence a outro inquilino.",
            ["access.denied.authenticated"] = "Você precisa estar autenticado para acessar este recurso.",
            ["access.denied.owner"] = "Somente o proprietário pode acessar este recurso.",
            ["quota.exceeded.assets"] = "Você atingiu o limite de arquivos ({0}/{1}).",
            ["quota.exceeded.storage"] = "Você excedeu sua cota de armazenamento ({0}/{1} bytes).",
            ["virus.detected"] = "O arquivo '{0}' contém conteúdo malicioso.",
            ["upload.failed.size"] = "O arquivo excede o tamanho máximo permitido.",
            ["upload.failed.type"] = "Este tipo de arquivo não é permitido.",
            ["upload.failed.generic"] = "O upload falhou. Por favor, tente novamente."
        }
    };

    public string GetModerationRejectionReason(string[] labels, string languageCode)
    {
        var localizedLabels = labels.Select(l => GetLocalizedLabel(l, languageCode));
        var labelsText = string.Join(", ", localizedLabels);
        var template = GetMessage("moderation.rejected", languageCode);
        return string.Format(template, labelsText);
    }

    public string GetAccessDeniedMessage(AssetAccessPolicy policy, string languageCode)
    {
        var key = policy switch
        {
            AssetAccessPolicy.Private => "access.denied.private",
            AssetAccessPolicy.TenantPublic => "access.denied.tenant",
            AssetAccessPolicy.Authenticated => "access.denied.authenticated",
            AssetAccessPolicy.OwnerOnly => "access.denied.owner",
            _ => "access.denied.private"
        };
        return GetMessage(key, languageCode);
    }

    public string GetQuotaExceededMessage(string quotaType, long currentUsage, long limit, string languageCode)
    {
        var key = quotaType.ToLowerInvariant() switch
        {
            "assets" => "quota.exceeded.assets",
            "storage" or "assetstorage" => "quota.exceeded.storage",
            _ => "quota.exceeded.assets"
        };
        var template = GetMessage(key, languageCode);
        return string.Format(template, currentUsage, limit);
    }

    public string GetVirusDetectedMessage(string fileName, string languageCode)
    {
        var template = GetMessage("virus.detected", languageCode);
        return string.Format(template, fileName);
    }

    public string GetUploadFailedMessage(string reason, string languageCode)
    {
        var key = reason.ToLowerInvariant() switch
        {
            "size" or "filetoobig" => "upload.failed.size",
            "type" or "mimetype" => "upload.failed.type",
            _ => "upload.failed.generic"
        };
        return GetMessage(key, languageCode);
    }

    private string GetLocalizedLabel(string label, string languageCode)
    {
        var key = $"moderation.label.{label.ToLowerInvariant()}";
        return GetMessage(key, languageCode, fallback: label);
    }

    private static string GetMessage(string key, string languageCode, string? fallback = null)
    {
        // Normalize language code
        var normalizedLang = NormalizeLanguageCode(languageCode);

        // Try exact match
        if (FallbackMessages.TryGetValue(normalizedLang, out var messages) && 
            messages.TryGetValue(key, out var message))
        {
            return message;
        }

        // Try base language (e.g., "pt-BR" -> "pt")
        var baseLang = normalizedLang.Split('-')[0];
        if (baseLang != normalizedLang && 
            FallbackMessages.TryGetValue(baseLang, out messages) && 
            messages.TryGetValue(key, out message))
        {
            return message;
        }

        // Fall back to English
        if (FallbackMessages["en"].TryGetValue(key, out message))
        {
            return message;
        }

        return fallback ?? key;
    }

    private static string NormalizeLanguageCode(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
            return "en";

        // Handle common variants
        return languageCode.ToLowerInvariant() switch
        {
            "pt" => "pt-BR",
            "es-mx" or "es-ar" or "es-co" => "es",
            _ => languageCode
        };
    }
}
