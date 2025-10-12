using GameGuild.CQRS;
namespace GameGuild.Modules.Users;

// ==================== CONSENT COMMANDS ====================

public sealed record GrantConsentCommand(
    Guid UserId,
    string ConsentType,
    string? FeatureId = null,
    string ConsentVersion = "1.0",
    DateTime? ExpiresAt = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string Source = "web"
) : IRequest<Result<ConsentRecordDto>>;

public sealed record RevokeConsentCommand(
    Guid UserId,
    string ConsentType,
    string? FeatureId = null,
    string? IpAddress = null,
    string? UserAgent = null
) : IRequest<Result>;

public sealed record GetUserConsentsQuery(
    Guid UserId
) : IRequest<Result<List<ConsentRecordDto>>>;

public sealed record HasActiveConsentQuery(
    Guid UserId,
    string ConsentType,
    string? FeatureId = null
) : IRequest<Result<bool>>;

// ==================== PRIVACY PREFERENCE COMMANDS ====================

public sealed record GetPrivacyPreferencesQuery(
    Guid UserId
) : IRequest<Result<PrivacyPreferenceDto>>;

public sealed record UpdatePrivacyPreferencesCommand(
    Guid UserId,
    DataVisibilityLevel? VisibilityLevel = null,
    bool? AllowSearchEngineIndexing = null,
    bool? ShowInPublicDirectory = null,
    bool? AllowAnalytics = null,
    bool? AllowPersonalization = null,
    bool? AllowThirdPartySharing = null,
    bool? AllowActivityTracking = null,
    bool? AllowLocationTracking = null,
    int? DataRetentionDays = null
) : IRequest<Result<PrivacyPreferenceDto>>;

// ==================== DTOs ====================

public sealed record ConsentRecordDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string ConsentType { get; init; } = string.Empty;
    public string? FeatureId { get; init; }
    public bool IsGranted { get; init; }
    public string ConsentVersion { get; init; } = string.Empty;
    public DateTime ConsentedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string Source { get; init; } = string.Empty;
    public bool IsExpired { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }

    public static ConsentRecordDto FromEntity(ConsentRecord record) => new()
    {
        Id = record.Id,
        UserId = record.UserId,
        ConsentType = record.ConsentType,
        FeatureId = record.FeatureId,
        IsGranted = record.IsGranted,
        ConsentVersion = record.ConsentVersion,
        ConsentedAt = record.ConsentedAt,
        ExpiresAt = record.ExpiresAt,
        Source = record.Source,
        IsExpired = record.IsExpired,
        IsActive = record.IsActive,
        CreatedAt = record.CreatedAt
    };
}

public sealed record PrivacyPreferenceDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public DataVisibilityLevel VisibilityLevel { get; init; }
    public bool AllowSearchEngineIndexing { get; init; }
    public bool ShowInPublicDirectory { get; init; }
    public bool AllowAnalytics { get; init; }
    public bool AllowPersonalization { get; init; }
    public bool AllowThirdPartySharing { get; init; }
    public bool AllowActivityTracking { get; init; }
    public bool AllowLocationTracking { get; init; }
    public int? DataRetentionDays { get; init; }
    public DateTime? LastReviewedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static PrivacyPreferenceDto FromEntity(PrivacyPreference pref) => new()
    {
        Id = pref.Id,
        UserId = pref.UserId,
        VisibilityLevel = pref.VisibilityLevel,
        AllowSearchEngineIndexing = pref.AllowSearchEngineIndexing,
        ShowInPublicDirectory = pref.ShowInPublicDirectory,
        AllowAnalytics = pref.AllowAnalytics,
        AllowPersonalization = pref.AllowPersonalization,
        AllowThirdPartySharing = pref.AllowThirdPartySharing,
        AllowActivityTracking = pref.AllowActivityTracking,
        AllowLocationTracking = pref.AllowLocationTracking,
        DataRetentionDays = pref.DataRetentionDays,
        LastReviewedAt = pref.LastReviewedAt,
        CreatedAt = pref.CreatedAt,
        UpdatedAt = pref.UpdatedAt
    };
}

// ==================== HANDLERS ====================

public sealed class GrantConsentHandler : IRequestHandler<GrantConsentCommand, Result<ConsentRecordDto>>
{
    private readonly IConsentService _consentService;

    public GrantConsentHandler(IConsentService consentService) => _consentService = consentService;

    public async Task<Result<ConsentRecordDto>> Handle(GrantConsentCommand request, CancellationToken cancellationToken)
    {
        var result = await _consentService.GrantConsentAsync(request.UserId, request.ConsentType,
            request.FeatureId, request.ConsentVersion, request.ExpiresAt, request.IpAddress,
            request.UserAgent, request.Source, cancellationToken);

        return result.IsSuccess
            ? Result<ConsentRecordDto>.Success(ConsentRecordDto.FromEntity(result.Value!))
            : Result<ConsentRecordDto>.Failure(result.Error);
    }
}

public sealed class RevokeConsentHandler : IRequestHandler<RevokeConsentCommand, Result>
{
    private readonly IConsentService _consentService;

    public RevokeConsentHandler(IConsentService consentService) => _consentService = consentService;

    public async Task<Result> Handle(RevokeConsentCommand request, CancellationToken cancellationToken) =>
        await _consentService.RevokeConsentAsync(request.UserId, request.ConsentType,
            request.FeatureId, request.IpAddress, request.UserAgent, cancellationToken);
}

public sealed class GetUserConsentsHandler : IRequestHandler<GetUserConsentsQuery, Result<List<ConsentRecordDto>>>
{
    private readonly IConsentService _consentService;

    public GetUserConsentsHandler(IConsentService consentService) => _consentService = consentService;

    public async Task<Result<List<ConsentRecordDto>>> Handle(GetUserConsentsQuery request, CancellationToken cancellationToken)
    {
        var result = await _consentService.GetUserConsentsAsync(request.UserId, cancellationToken);
        return result.IsSuccess
            ? Result<List<ConsentRecordDto>>.Success(result.Value!.Select(ConsentRecordDto.FromEntity).ToList())
            : Result<List<ConsentRecordDto>>.Failure(result.Error);
    }
}

public sealed class HasActiveConsentHandler : IRequestHandler<HasActiveConsentQuery, Result<bool>>
{
    private readonly IConsentService _consentService;

    public HasActiveConsentHandler(IConsentService consentService) => _consentService = consentService;

    public async Task<Result<bool>> Handle(HasActiveConsentQuery request, CancellationToken cancellationToken) =>
        await _consentService.HasActiveConsentAsync(request.UserId, request.ConsentType, request.FeatureId, cancellationToken);
}

public sealed class GetPrivacyPreferencesHandler : IRequestHandler<GetPrivacyPreferencesQuery, Result<PrivacyPreferenceDto>>
{
    private readonly IConsentService _consentService;

    public GetPrivacyPreferencesHandler(IConsentService consentService) => _consentService = consentService;

    public async Task<Result<PrivacyPreferenceDto>> Handle(GetPrivacyPreferencesQuery request, CancellationToken cancellationToken)
    {
        var result = await _consentService.GetPrivacyPreferencesAsync(request.UserId, cancellationToken);
        return result.IsSuccess
            ? Result<PrivacyPreferenceDto>.Success(PrivacyPreferenceDto.FromEntity(result.Value!))
            : Result<PrivacyPreferenceDto>.Failure(result.Error);
    }
}

public sealed class UpdatePrivacyPreferencesHandler : IRequestHandler<UpdatePrivacyPreferencesCommand, Result<PrivacyPreferenceDto>>
{
    private readonly IConsentService _consentService;

    public UpdatePrivacyPreferencesHandler(IConsentService consentService) => _consentService = consentService;

    public async Task<Result<PrivacyPreferenceDto>> Handle(UpdatePrivacyPreferencesCommand request, CancellationToken cancellationToken)
    {
        var result = await _consentService.UpdatePrivacyPreferencesAsync(request.UserId,
            request.VisibilityLevel, request.AllowSearchEngineIndexing, request.ShowInPublicDirectory,
            request.AllowAnalytics, request.AllowPersonalization, request.AllowThirdPartySharing,
            request.AllowActivityTracking, request.AllowLocationTracking, request.DataRetentionDays,
            cancellationToken);

        return result.IsSuccess
            ? Result<PrivacyPreferenceDto>.Success(PrivacyPreferenceDto.FromEntity(result.Value!))
            : Result<PrivacyPreferenceDto>.Failure(result.Error);
    }
}
