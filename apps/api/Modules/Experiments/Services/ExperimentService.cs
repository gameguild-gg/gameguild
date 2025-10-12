#pragma warning disable IDE0005 // Using directive is unnecessary
using GameGuild.Modules.Experiments.Entities;
using GameGuild.Modules.Experiments.Repositories;
#pragma warning restore IDE0005

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace GameGuild.Modules.Experiments.Services;
#pragma warning restore IDE0130
#pragma warning restore IDE0005

public class ExperimentService(
    IExperimentRepository experimentRepository,
    IVariantRepository variantRepository,
    IAssignmentRepository assignmentRepository) : IExperimentService {
  private readonly IExperimentRepository _experimentRepository = experimentRepository;
  private readonly IVariantRepository _variantRepository = variantRepository;
  private readonly IAssignmentRepository _assignmentRepository = assignmentRepository;
  private readonly Random _random = new();

  public async Task<Result<ExperimentDto>> CreateExperimentAsync(CreateExperimentRequest request, CancellationToken cancellationToken = default) {
    if (!Enum.TryParse<ExperimentType>(request.Type, out var type)) {
      return Result.Failure<ExperimentDto>(Error.Validation("ExperimentType.Invalid", $"Invalid experiment type: {request.Type}"));
    }

    var experiment = new PricingExperiment {
      Name = request.Name,
      Description = request.Description,
      Type = type,
      TargetPlanId = request.TargetPlanId,
      TargetSampleSize = request.TargetSampleSize,
      ConfidenceLevel = request.ConfidenceLevel,
      Hypothesis = request.Hypothesis,
      Status = ExperimentStatus.Draft,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    await _experimentRepository.CreateAsync(experiment, cancellationToken);
    return Result<ExperimentDto>.Success(MapToDto(experiment));
  }

  public async Task<Result<ExperimentDto>> GetExperimentAsync(Guid experimentId, CancellationToken cancellationToken = default) {
    var experiment = await _experimentRepository.GetByIdAsync(experimentId, cancellationToken);
    return experiment != null
        ? Result<ExperimentDto>.Success(MapToDto(experiment))
        : Result.Failure<ExperimentDto>(Error.NotFound("Experiment.NotFound", "Experiment not found"));
  }

  public async Task<Result<List<ExperimentDto>>> GetExperimentsAsync(Guid? tenantId, string? status, CancellationToken cancellationToken = default) {
    ExperimentStatus? statusEnum = null;
    if (status != null && Enum.TryParse<ExperimentStatus>(status, out var parsed)) {
      statusEnum = parsed;
    }

    var experiments = await _experimentRepository.GetAllAsync(tenantId, statusEnum, cancellationToken);
    var dtos = experiments.Select(MapToDto).ToList();
    return Result<List<ExperimentDto>>.Success(dtos);
  }

  public async Task<Result> StartExperimentAsync(Guid experimentId, CancellationToken cancellationToken = default) {
    var experiment = await _experimentRepository.GetByIdAsync(experimentId, cancellationToken);
    if (experiment == null) {
      return Result.Failure(Error.NotFound("Experiment.NotFound", "Experiment not found"));
    }

    try {
      experiment.Start();
      await _experimentRepository.UpdateAsync(experiment, cancellationToken);
      return Result.Success();
    }
    catch (InvalidOperationException ex) {
      return Result.Failure(Error.Problem("Experiment.Start.Failed", ex.Message));
    }
  }

  public async Task<Result> PauseExperimentAsync(Guid experimentId, CancellationToken cancellationToken = default) {
    var experiment = await _experimentRepository.GetByIdAsync(experimentId, cancellationToken);
    if (experiment == null) {
      return Result.Failure(Error.NotFound("Experiment.NotFound", "Experiment not found"));
    }

    try {
      experiment.Pause();
      await _experimentRepository.UpdateAsync(experiment, cancellationToken);
      return Result.Success();
    }
    catch (InvalidOperationException ex) {
      return Result.Failure(Error.Problem("Experiment.Pause.Failed", ex.Message));
    }
  }

  public async Task<Result> CompleteExperimentAsync(Guid experimentId, CancellationToken cancellationToken = default) {
    var experiment = await _experimentRepository.GetByIdAsync(experimentId, cancellationToken);
    if (experiment == null) {
      return Result.Failure(Error.NotFound("Experiment.NotFound", "Experiment not found"));
    }

    try {
      experiment.Complete();
      await _experimentRepository.UpdateAsync(experiment, cancellationToken);
      return Result.Success();
    }
    catch (InvalidOperationException ex) {
      return Result.Failure(Error.Problem("Experiment.Complete.Failed", ex.Message));
    }
  }

  public async Task<Result<VariantDto>> AddVariantAsync(Guid experimentId, AddVariantRequest request, CancellationToken cancellationToken = default) {
    var experiment = await _experimentRepository.GetByIdAsync(experimentId, cancellationToken);
    if (experiment == null) {
      return Result.Failure<VariantDto>(Error.NotFound("Experiment.NotFound", "Experiment not found"));
    }

    if (experiment.Status != ExperimentStatus.Draft) {
      return Result.Failure<VariantDto>(Error.Problem("Variant.Add.InvalidStatus", "Can only add variants to draft experiments"));
    }

    var variant = new ExperimentVariant {
      ExperimentId = experimentId,
      Name = request.Name,
      Description = request.Description,
      IsControl = request.IsControl,
      TrafficAllocation = request.TrafficAllocation,
      PriceOverride = request.PriceOverride,
      PricingConfiguration = request.PricingConfiguration,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    await _variantRepository.CreateAsync(variant, cancellationToken);
    return Result<VariantDto>.Success(MapToDto(variant));
  }

  public async Task<Result<AssignmentDto>> AssignUserAsync(Guid experimentId, Guid userId, CancellationToken cancellationToken = default) {
    var experiment = await _experimentRepository.GetByIdAsync(experimentId, cancellationToken);
    if (experiment == null || !experiment.IsActive()) {
      return Result.Failure<AssignmentDto>(Error.NotFound("Experiment.NotFoundOrInactive", "Experiment not found or not active"));
    }

    // Check if user already assigned
    var existing = await _assignmentRepository.GetByUserAndExperimentAsync(userId, experimentId, cancellationToken);
    if (existing != null) {
      return Result<AssignmentDto>.Success(MapToDto(existing));
    }

    // Select variant based on traffic allocation
    var variant = SelectVariant(experiment.Variants.ToList());
    if (variant == null) {
      return Result.Failure<AssignmentDto>(Error.Problem("Variant.NoVariantsAvailable", "No variants available"));
    }

    var assignment = new UserAssignment {
      ExperimentId = experimentId,
      VariantId = variant.Id,
      UserId = userId,
      AssignedAt = DateTime.UtcNow,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    await _assignmentRepository.CreateAsync(assignment, cancellationToken);
    variant.RecordImpression();
    await _variantRepository.UpdateAsync(variant, cancellationToken);

    return Result<AssignmentDto>.Success(MapToDto(assignment));
  }

  public async Task<Result> RecordConversionAsync(Guid assignmentId, decimal revenue, CancellationToken cancellationToken = default) {
    var assignment = await _assignmentRepository.GetByIdAsync(assignmentId, cancellationToken);
    if (assignment == null) {
      return Result.Failure(Error.NotFound("Assignment.NotFound", "Assignment not found"));
    }

    try {
      assignment.RecordConversion(revenue);
      await _assignmentRepository.UpdateAsync(assignment, cancellationToken);

      var variant = await _variantRepository.GetByIdAsync(assignment.VariantId, cancellationToken);
      if (variant != null) {
        variant.RecordConversion(revenue);
        await _variantRepository.UpdateAsync(variant, cancellationToken);
      }

      return Result.Success();
    }
    catch (InvalidOperationException ex) {
      return Result.Failure(Error.Problem("Conversion.Record.Failed", ex.Message));
    }
  }

  public async Task<Result<ExperimentAnalyticsDto>> GetAnalyticsAsync(Guid experimentId, CancellationToken cancellationToken = default) {
    var experiment = await _experimentRepository.GetByIdAsync(experimentId, cancellationToken);
    if (experiment == null) {
      return Result.Failure<ExperimentAnalyticsDto>(Error.NotFound("Experiment.NotFound", "Experiment not found"));
    }

    var control = experiment.Variants.FirstOrDefault(v => v.IsControl);
    var variantResults = experiment.Variants.Select(v => {
      var result = ExperimentResult.Calculate(v, control, experiment.ConfidenceLevel);
      return new VariantResultDto(
          v.Id,
          v.Name,
          v.IsControl,
          v.ImpressionCount,
          v.ConversionRate,
          result.PValue,
          result.IsStatisticallySignificant,
          result.Lift,
          result.GetResultSummary());
    }).ToList();

    var analytics = new ExperimentAnalyticsDto(
        experimentId,
        experiment.Status.ToString(),
        experiment.UserAssignments.Count,
        experiment.UserAssignments.Count(a => a.HasConverted),
        experiment.Variants.Sum(v => v.Revenue),
        variantResults,
        experiment.GetWinningVariant() != null ? MapToDto(experiment.GetWinningVariant()!) : null,
        variantResults.Any(r => !r.IsControl && r.IsSignificant));

    return Result<ExperimentAnalyticsDto>.Success(analytics);
  }

  private ExperimentVariant? SelectVariant(List<ExperimentVariant> variants) {
    var totalAllocation = variants.Sum(v => v.TrafficAllocation);
    if (totalAllocation == 0) {
      return variants.FirstOrDefault();
    }

    var roll = _random.Next(0, totalAllocation);
    var cumulative = 0;

    foreach (var variant in variants) {
      cumulative += variant.TrafficAllocation;
      if (roll < cumulative) {
        return variant;
      }
    }

    return variants.LastOrDefault();
  }

  private static ExperimentDto MapToDto(PricingExperiment experiment) {
    return new(
        experiment.Id,
        experiment.TenantId,
        experiment.Name,
        experiment.Description,
        experiment.Status.ToString(),
        experiment.Type.ToString(),
        experiment.StartDate,
        experiment.EndDate,
        experiment.TargetSampleSize,
        experiment.UserAssignments.Count,
        experiment.ConfidenceLevel,
        experiment.Variants.Select(MapToDto).ToList());
  }

  private static VariantDto MapToDto(ExperimentVariant variant) {
    return new(
        variant.Id,
        variant.Name,
        variant.IsControl,
        variant.TrafficAllocation,
        variant.ImpressionCount,
        variant.ConversionCount,
        variant.ConversionRate,
        variant.Revenue,
        variant.AverageRevenuePerUser);
  }

  private static AssignmentDto MapToDto(UserAssignment assignment) {
    return new(
        assignment.Id,
        assignment.ExperimentId,
        assignment.VariantId,
        assignment.UserId,
        assignment.AssignedAt);
  }
}
