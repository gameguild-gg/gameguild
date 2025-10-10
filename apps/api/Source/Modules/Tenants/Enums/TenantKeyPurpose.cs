using System.ComponentModel;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// Purpose/use case for tenant encryption keys
/// </summary>
public enum TenantKeyPurpose
{
    [Description("General data encryption")]
    DataEncryption,

    [Description("Database field encryption")]
    DatabaseEncryption,

    [Description("File storage encryption")]
    FileEncryption,

    [Description("Communication encryption")]
    CommunicationEncryption,

    [Description("API token encryption")]
    TokenEncryption,

    [Description("Backup data encryption")]
    BackupEncryption
}
