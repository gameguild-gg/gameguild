using GameGuild.CQRS;
using GameGuild.Modules.Tenants.Commands;
using GameGuild.Modules.Tenants.Repositories;
using GameGuild.Modules.Tenants.Services;


namespace GameGuild.Modules.Tenants.Handlers;

// Generate Key Handler
public class GenerateTenantEncryptionKeyHandler : IRequestHandler<GenerateTenantEncryptionKeyCommand, Result<TenantEncryptionKey>>
{
    private readonly ITenantEncryptionService _encryptionService;
    private readonly ITenantEncryptionKeyRepository _repository;
    private readonly ILogger<GenerateTenantEncryptionKeyHandler> _logger;

    public GenerateTenantEncryptionKeyHandler(
        ITenantEncryptionService encryptionService,
        ITenantEncryptionKeyRepository repository,
        ILogger<GenerateTenantEncryptionKeyHandler> logger)
    {
        _encryptionService = encryptionService;
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<TenantEncryptionKey>> Handle(GenerateTenantEncryptionKeyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if key name already exists
            if (await _repository.KeyNameExistsAsync(request.TenantId, request.KeyName, cancellationToken))
            {
                return Result<TenantEncryptionKey>.Failure($"Key with name '{request.KeyName}' already exists for tenant");
            }

            var key = await _encryptionService.GenerateKeyAsync(request.TenantId, request.KeyName, request.KeyPurpose);

            _logger.LogInformation("Generated encryption key {KeyId} for tenant {TenantId} with purpose {Purpose}",
                key.Id, request.TenantId, request.KeyPurpose);

            return Result<TenantEncryptionKey>.Success(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating encryption key for tenant {TenantId}", request.TenantId);
            return Result<TenantEncryptionKey>.Failure($"Error generating encryption key: {ex.Message}");
        }
    }
}

// Rotate Key Handler
public class RotateTenantEncryptionKeyHandler : IRequestHandler<RotateTenantEncryptionKeyCommand, Result<TenantEncryptionKey>>
{
    private readonly ITenantEncryptionService _encryptionService;
    private readonly ILogger<RotateTenantEncryptionKeyHandler> _logger;

    public RotateTenantEncryptionKeyHandler(
        ITenantEncryptionService encryptionService,
        ILogger<RotateTenantEncryptionKeyHandler> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<Result<TenantEncryptionKey>> Handle(RotateTenantEncryptionKeyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var newKey = await _encryptionService.RotateKeyAsync(request.KeyId);

            _logger.LogInformation("Rotated encryption key {OldKeyId} to new key {NewKeyId}",
                request.KeyId, newKey.Id);

            return Result<TenantEncryptionKey>.Success(newKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rotating encryption key {KeyId}", request.KeyId);
            return Result<TenantEncryptionKey>.Failure($"Error rotating encryption key: {ex.Message}");
        }
    }
}

// Deactivate Key Handler
public class DeactivateTenantEncryptionKeyHandler : IRequestHandler<DeactivateTenantEncryptionKeyCommand, Result>
{
    private readonly ITenantEncryptionService _encryptionService;
    private readonly ILogger<DeactivateTenantEncryptionKeyHandler> _logger;

    public DeactivateTenantEncryptionKeyHandler(
        ITenantEncryptionService encryptionService,
        ILogger<DeactivateTenantEncryptionKeyHandler> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<Result> Handle(DeactivateTenantEncryptionKeyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _encryptionService.DeactivateKeyAsync(request.KeyId);

            _logger.LogInformation("Deactivated encryption key {KeyId}", request.KeyId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating encryption key {KeyId}", request.KeyId);
            return Result.Failure($"Error deactivating encryption key: {ex.Message}");
        }
    }
}

// Encrypt Data Handler
public class EncryptTenantDataHandler : IRequestHandler<EncryptTenantDataCommand, Result<string>>
{
    private readonly ITenantEncryptionService _encryptionService;
    private readonly ILogger<EncryptTenantDataHandler> _logger;

    public EncryptTenantDataHandler(
        ITenantEncryptionService encryptionService,
        ILogger<EncryptTenantDataHandler> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(EncryptTenantDataCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var encryptedData = await _encryptionService.EncryptDataAsync(request.TenantId, request.Data, request.KeyPurpose);

            _logger.LogDebug("Encrypted data for tenant {TenantId} with purpose {Purpose}",
                request.TenantId, request.KeyPurpose);

            return Result<string>.Success(encryptedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting data for tenant {TenantId}", request.TenantId);
            return Result<string>.Failure($"Error encrypting data: {ex.Message}");
        }
    }
}

// Decrypt Data Handler
public class DecryptTenantDataHandler : IRequestHandler<DecryptTenantDataCommand, Result<string>>
{
    private readonly ITenantEncryptionService _encryptionService;
    private readonly ILogger<DecryptTenantDataHandler> _logger;

    public DecryptTenantDataHandler(
        ITenantEncryptionService encryptionService,
        ILogger<DecryptTenantDataHandler> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(DecryptTenantDataCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var decryptedData = await _encryptionService.DecryptDataAsync(request.TenantId, request.EncryptedData, request.KeyPurpose);

            _logger.LogDebug("Decrypted data for tenant {TenantId} with purpose {Purpose}",
                request.TenantId, request.KeyPurpose);

            return Result<string>.Success(decryptedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting data for tenant {TenantId}", request.TenantId);
            return Result<string>.Failure($"Error decrypting data: {ex.Message}");
        }
    }
}

// Get Active Key Handler
public class GetActiveTenantEncryptionKeyHandler : IRequestHandler<GetActiveTenantEncryptionKeyQuery, Result<TenantEncryptionKey>>
{
    private readonly ITenantEncryptionService _encryptionService;
    private readonly ILogger<GetActiveTenantEncryptionKeyHandler> _logger;

    public GetActiveTenantEncryptionKeyHandler(
        ITenantEncryptionService encryptionService,
        ILogger<GetActiveTenantEncryptionKeyHandler> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<Result<TenantEncryptionKey>> Handle(GetActiveTenantEncryptionKeyQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var key = await _encryptionService.GetActiveKeyAsync(request.TenantId, request.KeyPurpose);

            if (key == null)
            {
                return Result<TenantEncryptionKey>.Failure($"No active key found for tenant {request.TenantId} with purpose {request.KeyPurpose}");
            }

            return Result<TenantEncryptionKey>.Success(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active key for tenant {TenantId}", request.TenantId);
            return Result<TenantEncryptionKey>.Failure($"Error getting active key: {ex.Message}");
        }
    }
}

// Get Key History Handler
public class GetTenantEncryptionKeyHistoryHandler : IRequestHandler<GetTenantEncryptionKeyHistoryQuery, Result<List<TenantEncryptionKey>>>
{
    private readonly ITenantEncryptionService _encryptionService;
    private readonly ILogger<GetTenantEncryptionKeyHistoryHandler> _logger;

    public GetTenantEncryptionKeyHistoryHandler(
        ITenantEncryptionService encryptionService,
        ILogger<GetTenantEncryptionKeyHistoryHandler> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<Result<List<TenantEncryptionKey>>> Handle(GetTenantEncryptionKeyHistoryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var keys = await _encryptionService.GetKeyHistoryAsync(request.TenantId, request.KeyPurpose);

            return Result<List<TenantEncryptionKey>>.Success(keys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting key history for tenant {TenantId}", request.TenantId);
            return Result<List<TenantEncryptionKey>>.Failure($"Error getting key history: {ex.Message}");
        }
    }
}
