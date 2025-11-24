using GameGuild.Features.Entities;

namespace GameGuild.Features.Services;

/// <summary>
///     Extension methods for automatic encryption/decryption
/// </summary>
public static class FeatureFlagEncryptionExtensions
{
    /// <summary>
    ///     Encrypts flag values if RequiresEncryption is true
    /// </summary>
    public static async Task EncryptSensitiveDataAsync(this FeatureFlag flag, IFeatureFlagEncryptionService encryptionService)
    {
        if (!flag.RequiresEncryption) return;

        if (!string.IsNullOrEmpty(flag.DefaultValue)) { flag.DefaultValue = await encryptionService.EncryptAsync(flag.DefaultValue); }

        if (!string.IsNullOrEmpty(flag.EnabledValue)) { flag.EnabledValue = await encryptionService.EncryptAsync(flag.EnabledValue); }
    }

    /// <summary>
    ///     Decrypts flag values if RequiresEncryption is true
    /// </summary>
    public static async Task DecryptSensitiveDataAsync(this FeatureFlag flag, IFeatureFlagEncryptionService encryptionService)
    {
        if (!flag.RequiresEncryption) return;

        if (!string.IsNullOrEmpty(flag.DefaultValue) && encryptionService.IsEncrypted(flag.DefaultValue)) { flag.DefaultValue = await encryptionService.DecryptAsync(flag.DefaultValue); }

        if (!string.IsNullOrEmpty(flag.EnabledValue) && encryptionService.IsEncrypted(flag.EnabledValue)) { flag.EnabledValue = await encryptionService.DecryptAsync(flag.EnabledValue); }
    }
}
