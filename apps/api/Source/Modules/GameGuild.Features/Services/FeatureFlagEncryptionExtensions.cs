namespace GameGuild.Features;

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

        if (!string.IsNullOrEmpty(flag.DefaultValue)) { flag.DefaultValue = await encryptionService.EncryptAsync(flag.DefaultValue).ConfigureAwait(false); }

        if (!string.IsNullOrEmpty(flag.EnabledValue)) { flag.EnabledValue = await encryptionService.EncryptAsync(flag.EnabledValue).ConfigureAwait(false); }
    }

    /// <summary>
    ///     Decrypts flag values if RequiresEncryption is true
    /// </summary>
    public static async Task DecryptSensitiveDataAsync(this FeatureFlag flag, IFeatureFlagEncryptionService encryptionService)
    {
        if (!flag.RequiresEncryption) return;

        if (!string.IsNullOrEmpty(flag.DefaultValue) && encryptionService.IsEncrypted(flag.DefaultValue)) { flag.DefaultValue = await encryptionService.DecryptAsync(flag.DefaultValue).ConfigureAwait(false); }

        if (!string.IsNullOrEmpty(flag.EnabledValue) && encryptionService.IsEncrypted(flag.EnabledValue)) { flag.EnabledValue = await encryptionService.DecryptAsync(flag.EnabledValue).ConfigureAwait(false); }
    }
}
