using GameGuild.CQRS;
using GameGuild.Modules.Tenants;

namespace GameGuild.Modules.Tenants.Commands;

// Generate Key Command
public record GenerateTenantEncryptionKeyCommand(
    Guid TenantId,
    string KeyName,
    TenantKeyPurpose KeyPurpose
) : IRequest<Result<TenantEncryptionKey>>;

// Rotate Key Command
public record RotateTenantEncryptionKeyCommand(
    Guid KeyId
) : IRequest<Result<TenantEncryptionKey>>;

// Deactivate Key Command
public record DeactivateTenantEncryptionKeyCommand(
    Guid KeyId
) : IRequest<Result>;

// Encrypt Data Command
public record EncryptTenantDataCommand(
    Guid TenantId,
    string Data,
    TenantKeyPurpose KeyPurpose
) : IRequest<Result<string>>;

// Decrypt Data Command
public record DecryptTenantDataCommand(
    Guid TenantId,
    string EncryptedData,
    TenantKeyPurpose KeyPurpose
) : IRequest<Result<string>>;

// Get Active Key Query
public record GetActiveTenantEncryptionKeyQuery(
    Guid TenantId,
    TenantKeyPurpose KeyPurpose
) : IRequest<Result<TenantEncryptionKey>>;

// Get Key History Query
public record GetTenantEncryptionKeyHistoryQuery(
    Guid TenantId,
    TenantKeyPurpose KeyPurpose
) : IRequest<Result<List<TenantEncryptionKey>>>;
