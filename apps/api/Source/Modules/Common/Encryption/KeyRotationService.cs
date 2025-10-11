using System.Diagnostics;


namespace GameGuild.Modules.Common.Encryption;

/// <summary>
/// Key rotation service for automated master key rotation.
/// </summary>
public sealed class KeyRotationService : IKeyRotationService
{
    private readonly ILogger<KeyRotationService> _logger;
    private readonly IMasterKeyProvider _masterKeyProvider;
    private readonly IEnvelopeEncryptionService _encryptionService;
    private readonly IEncryptedDataRepository _dataRepository;
    private Timer? _rotationTimer;

    public KeyRotationService(
        ILogger<KeyRotationService> logger,
        IMasterKeyProvider masterKeyProvider,
        IEnvelopeEncryptionService encryptionService,
        IEncryptedDataRepository dataRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _masterKeyProvider = masterKeyProvider ?? throw new ArgumentNullException(nameof(masterKeyProvider));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _dataRepository = dataRepository ?? throw new ArgumentNullException(nameof(dataRepository));
    }

    /// <summary>
    /// Rotates the master encryption key.
    /// </summary>
    public async Task<KeyRotationResult> RotateMasterKeyAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Get current active key ID
            var oldKeyId = await _masterKeyProvider.GetActiveKeyIdAsync(cancellationToken);

            _logger.LogInformation("Starting master key rotation (current key: {OldKeyId})", oldKeyId);

            // 2. Create new master key
            var newKeyId = await _masterKeyProvider.CreateKeyAsync(cancellationToken);

            _logger.LogInformation("Created new master key: {NewKeyId}", newKeyId);

            // 3. Re-encrypt all data with new key
            var reEncryptionResult = await ReEncryptDataAsync(newKeyId, cancellationToken);

            stopwatch.Stop();

            if (reEncryptionResult.Success)
            {
                _logger.LogInformation(
                    "Master key rotation completed successfully: {OldKeyId} → {NewKeyId} " +
                    "({ReEncrypted} records re-encrypted in {Duration}ms)",
                    oldKeyId, newKeyId, reEncryptionResult.ReEncryptedRecords, stopwatch.ElapsedMilliseconds);

                return new KeyRotationResult
                {
                    OldKeyId = oldKeyId,
                    NewKeyId = newKeyId,
                    RotatedAt = DateTime.UtcNow,
                    Success = true
                };
            }
            else
            {
                _logger.LogError(
                    "Master key rotation failed: {FailedRecords} records could not be re-encrypted",
                    reEncryptionResult.FailedRecords);

                return new KeyRotationResult
                {
                    OldKeyId = oldKeyId,
                    NewKeyId = newKeyId,
                    RotatedAt = DateTime.UtcNow,
                    Success = false,
                    ErrorMessage = $"{reEncryptionResult.FailedRecords} records failed to re-encrypt"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Master key rotation failed with exception");
            return new KeyRotationResult
            {
                OldKeyId = string.Empty,
                NewKeyId = string.Empty,
                RotatedAt = DateTime.UtcNow,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Re-encrypts all data with the new master key.
    /// </summary>
    public async Task<ReEncryptionResult> ReEncryptDataAsync(string newKeyId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var totalRecords = 0;
        var reEncryptedRecords = 0;
        var failedRecords = 0;
        var errors = new List<string>();

        try
        {
            // Get all encrypted data records
            var encryptedRecords = await _dataRepository.GetAllAsync(cancellationToken);
            totalRecords = encryptedRecords.Count;

            _logger.LogInformation("Re-encrypting {TotalRecords} records with new key: {NewKeyId}", totalRecords, newKeyId);

            foreach (var record in encryptedRecords)
            {
                try
                {
                    // Re-encrypt with new key
                    var reEncryptedData = await _encryptionService.ReEncryptAsync(
                        record.EncryptedData,
                        newKeyId,
                        cancellationToken);

                    // Update record in repository
                    await _dataRepository.UpdateAsync(record.Id, reEncryptedData, cancellationToken);

                    reEncryptedRecords++;

                    if (reEncryptedRecords % 100 == 0)
                    {
                        _logger.LogInformation("Re-encryption progress: {Completed}/{Total}", reEncryptedRecords, totalRecords);
                    }
                }
                catch (Exception ex)
                {
                    failedRecords++;
                    var errorMessage = $"Record {record.Id}: {ex.Message}";
                    errors.Add(errorMessage);
                    _logger.LogError(ex, "Failed to re-encrypt record {RecordId}", record.Id);
                }
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Re-encryption completed: {Success}/{Total} records ({Failed} failed) in {Duration}ms",
                reEncryptedRecords, totalRecords, failedRecords, stopwatch.ElapsedMilliseconds);

            return new ReEncryptionResult
            {
                TotalRecords = totalRecords,
                ReEncryptedRecords = reEncryptedRecords,
                FailedRecords = failedRecords,
                Duration = stopwatch.Elapsed,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Re-encryption process failed");
            return new ReEncryptionResult
            {
                TotalRecords = totalRecords,
                ReEncryptedRecords = reEncryptedRecords,
                FailedRecords = failedRecords + (totalRecords - reEncryptedRecords),
                Duration = stopwatch.Elapsed,
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Gets the current active master key ID.
    /// </summary>
    public Task<string> GetActiveKeyIdAsync(CancellationToken cancellationToken = default)
    {
        return _masterKeyProvider.GetActiveKeyIdAsync(cancellationToken);
    }

    /// <summary>
    /// Schedules automatic key rotation at the specified interval.
    /// </summary>
    public Task ScheduleRotationAsync(TimeSpan interval, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scheduling automatic key rotation every {Interval}", interval);

        _rotationTimer = new Timer(
            async _ => await RotateMasterKeyAsync(cancellationToken),
            null,
            interval,
            interval);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _rotationTimer?.Dispose();
    }
}

/// <summary>
/// Repository interface for encrypted data records.
/// </summary>
public interface IEncryptedDataRepository
{
    Task<List<EncryptedDataRecord>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(string id, EncryptedData encryptedData, CancellationToken cancellationToken = default);
}

/// <summary>
/// Encrypted data record.
/// </summary>
public sealed class EncryptedDataRecord
{
    public required string Id { get; init; }
    public required EncryptedData EncryptedData { get; init; }
}
