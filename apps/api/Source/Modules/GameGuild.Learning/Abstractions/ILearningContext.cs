namespace GameGuild.Learning.Abstractions;

/// <summary>
/// Shared context for tenant and user information across all Learning modules.
/// Provides a consistent way to access context without tight coupling to specific implementations.
/// </summary>
public interface ILearningContext
{
    /// <summary>
    /// The current user's ID
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// The current tenant's ID
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Whether the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// The user's roles within the learning context
    /// </summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>
    /// Checks if the user has a specific role
    /// </summary>
    bool HasRole(string role);

    /// <summary>
    /// Gets a required user ID or throws if not authenticated
    /// </summary>
    Guid GetRequiredUserId();

    /// <summary>
    /// Gets a required tenant ID or throws if not in tenant context
    /// </summary>
    Guid GetRequiredTenantId();
}

/// <summary>
/// Common learning context roles
/// </summary>
public static class LearningRoles
{
    public const string Student = "student";
    public const string Instructor = "instructor";
    public const string CourseAdmin = "course_admin";
    public const string ContentCreator = "content_creator";
    public const string LearningPathCurator = "learning_path_curator";
}
