using GameGuild.CQRS;
using GameGuild.Features.Abstractions;
using GameGuild.Features.Entities;

namespace GameGuild.Features.Queries.Handlers;

/// <summary>
///     Handler for validating feature flag key
/// </summary>
public sealed class ValidateFeatureFlagKeyQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<ValidateFeatureFlagKeyQuery, ValidationResult>
{
    private readonly IFeatureFlagQueryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<ValidationResult> Handle(ValidateFeatureFlagKeyQuery request, CancellationToken cancellationToken)
    {
        // Check if key already exists
        var existingFlag = await _repository.GetByKeyAsync(request.Key, cancellationToken);
        var exists = existingFlag != null && (request.ExcludeId == null || existingFlag.Id != request.ExcludeId);

        if (exists) { return ValidationResult.Failure(new CQRS.ValidationError("Key", $"Feature flag with key '{request.Key}' already exists.", request.Key)); }

        return ValidationResult.Success();
    }
}
