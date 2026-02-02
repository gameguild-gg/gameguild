namespace GameGuild.Learning.Abstractions;

/// <summary>
/// Service interface for checking learning-related capabilities and entitlements.
/// Wraps the capability service with learning-specific convenience methods.
/// </summary>
public interface ILearningCapabilityService
{
    /// <summary>
    /// Checks if the discovery feature is enabled for the tenant
    /// </summary>
    Task<bool> IsDiscoveryEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if learning paths feature is enabled for the tenant
    /// </summary>
    Task<bool> IsLearningPathsEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if basic recommendations are enabled for the tenant
    /// </summary>
    Task<bool> IsRecommendationsEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if AI-powered recommendations are enabled for the tenant
    /// </summary>
    Task<bool> IsAiRecommendationsEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if skills tracking is enabled for the tenant
    /// </summary>
    Task<bool> IsSkillsEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if assessments are enabled for the tenant
    /// </summary>
    Task<bool> IsAssessmentsEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if certificates are enabled for the tenant
    /// </summary>
    Task<bool> IsCertificatesEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all learning capabilities for a tenant
    /// </summary>
    Task<LearningCapabilities> GetLearningCapabilitiesAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the learning capabilities enabled for a tenant
/// </summary>
public record LearningCapabilities
{
    public bool CoursesBasic { get; init; } = true;
    public bool Enrollments { get; init; } = true;
    public bool Certificates { get; init; }
    public bool Assessments { get; init; }
    public bool Discovery { get; init; }
    public bool LearningPaths { get; init; }
    public bool RecommendationsBasic { get; init; }
    public bool RecommendationsAi { get; init; }
    public bool Skills { get; init; }

    /// <summary>
    /// Creates capabilities for a free tier tenant
    /// </summary>
    public static LearningCapabilities Free => new()
    {
        CoursesBasic = true,
        Enrollments = true
    };

    /// <summary>
    /// Creates capabilities for a starter tier tenant
    /// </summary>
    public static LearningCapabilities Starter => new()
    {
        CoursesBasic = true,
        Enrollments = true,
        Certificates = true,
        Discovery = true
    };

    /// <summary>
    /// Creates capabilities for a pro tier tenant
    /// </summary>
    public static LearningCapabilities Pro => new()
    {
        CoursesBasic = true,
        Enrollments = true,
        Certificates = true,
        Assessments = true,
        Discovery = true,
        LearningPaths = true,
        RecommendationsBasic = true,
        Skills = true
    };

    /// <summary>
    /// Creates capabilities for an enterprise tier tenant
    /// </summary>
    public static LearningCapabilities Enterprise => new()
    {
        CoursesBasic = true,
        Enrollments = true,
        Certificates = true,
        Assessments = true,
        Discovery = true,
        LearningPaths = true,
        RecommendationsBasic = true,
        RecommendationsAi = true,
        Skills = true
    };
}
