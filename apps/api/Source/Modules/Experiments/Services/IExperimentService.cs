using GameGuild.Core.Results;

namespace GameGuild.Modules.Experiments.Services;

public interface IExperimentService
{
    Task<Result<ExperimentDto>> CreateExperimentAsync(CreateExperimentRequest request, CancellationToken cancellationToken = default);
    Task<Result<ExperimentDto>> GetExperimentAsync(Guid experimentId, CancellationToken cancellationToken = default);
    Task<Result<List<ExperimentDto>>> GetExperimentsAsync(Guid? tenantId, string? status, CancellationToken cancellationToken = default);
    Task<Result> StartExperimentAsync(Guid experimentId, CancellationToken cancellationToken = default);
    Task<Result> PauseExperimentAsync(Guid experimentId, CancellationToken cancellationToken = default);
    Task<Result> CompleteExperimentAsync(Guid experimentId, CancellationToken cancellationToken = default);
    Task<Result<VariantDto>> AddVariantAsync(Guid experimentId, AddVariantRequest request, CancellationToken cancellationToken = default);
    Task<Result<AssignmentDto>> AssignUserAsync(Guid experimentId, Guid userId, CancellationToken cancellationToken = default);
    Task<Result> RecordConversionAsync(Guid assignmentId, decimal revenue, CancellationToken cancellationToken = default);
    Task<Result<ExperimentAnalyticsDto>> GetAnalyticsAsync(Guid experimentId, CancellationToken cancellationToken = default);
}

public record CreateExperimentRequest(
    Guid? TenantId,
    string Name,
    string Description,
    string Type,
    Guid? TargetPlanId,
    int TargetSampleSize,
    double ConfidenceLevel,
    string? Hypothesis);

public record AddVariantRequest(
    string Name,
    string Description,
    bool IsControl,
    int TrafficAllocation,
    decimal? PriceOverride,
    string? PricingConfiguration);

public record ExperimentDto(
    Guid Id,
    Guid? TenantId,
    string Name,
    string Description,
    string Status,
    string Type,
    DateTime? StartDate,
    DateTime? EndDate,
    int TargetSampleSize,
    int CurrentSampleSize,
    double ConfidenceLevel,
    List<VariantDto> Variants);

public record VariantDto(
    Guid Id,
    string Name,
    bool IsControl,
    int TrafficAllocation,
    int ImpressionCount,
    int ConversionCount,
    double ConversionRate,
    decimal Revenue,
    decimal AverageRevenuePerUser);

public record AssignmentDto(
    Guid Id,
    Guid ExperimentId,
    Guid VariantId,
    Guid UserId,
    DateTime AssignedAt);

public record ExperimentAnalyticsDto(
    Guid ExperimentId,
    string Status,
    int TotalAssignments,
    int TotalConversions,
    decimal TotalRevenue,
    List<VariantResultDto> VariantResults,
    VariantDto? WinningVariant,
    bool HasStatisticalSignificance);

public record VariantResultDto(
    Guid VariantId,
    string Name,
    bool IsControl,
    int SampleSize,
    double ConversionRate,
    double PValue,
    bool IsSignificant,
    double Lift,
    string Summary);
