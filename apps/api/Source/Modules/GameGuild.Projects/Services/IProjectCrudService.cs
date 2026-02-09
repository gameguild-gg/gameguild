namespace GameGuild.Projects;

/// <summary>
/// Service interface for core Project CRUD operations, queries, and tenant management.
/// </summary>
public interface IProjectCrudService
{
    #region Deleted Projects

    /// <summary> Get all deleted projects </summary>
    Task<IEnumerable<Project>> GetDeletedProjectsAsync();

    #endregion

    #region Basic CRUD Operations

    /// <summary> Get all projects (non-deleted only) </summary>
    Task<IEnumerable<Project>> GetAllProjectsAsync();

    /// <summary> Get projects with pagination </summary>
    Task<IEnumerable<Project>> GetProjectsAsync(int skip = 0, int take = 50);

    /// <summary> Get projects with pagination without loading related entities for performance-critical scenarios </summary>
    Task<IEnumerable<Project>> GetProjectsOptimizedAsync(int skip = 0, int take = 50);

    /// <summary> Get a project by ID </summary>
    Task<Project?> GetProjectByIdAsync(Guid id);

    /// <summary> Get a project by ID with all related details </summary>
    Task<Project?> GetProjectByIdWithDetailsAsync(Guid id);

    /// <summary> Get a project by slug </summary>
    Task<Project?> GetProjectBySlugAsync(string slug);

    /// <summary> Create a new project </summary>
    Task<Project> CreateProjectAsync(Project project);

    /// <summary> Update an existing project </summary>
    Task<Project> UpdateProjectAsync(Project project);

    /// <summary> Soft delete a project </summary>
    Task<bool> DeleteProjectAsync(Guid id);

    /// <summary> Restore a deleted project </summary>
    Task<bool> RestoreProjectAsync(Guid id);

    #endregion

    #region Filtered Queries

    /// <summary> Get projects by category </summary>
    Task<IEnumerable<Project>> GetProjectsByCategoryAsync(Guid categoryId);

    /// <summary> Get projects by creator </summary>
    Task<IEnumerable<Project>> GetProjectsByCreatorAsync(Guid creatorId);

    /// <summary> Get projects with a specific status </summary>
    Task<IEnumerable<Project>> GetProjectsByStatusAsync(ContentStatus status);

    /// <summary> Get projects by type </summary>
    Task<IEnumerable<Project>> GetProjectsByTypeAsync(ProjectType type);

    /// <summary> Get projects by development status </summary>
    Task<IEnumerable<Project>> GetProjectsByDevelopmentStatusAsync(DevelopmentStatus status);

    /// <summary> Get public projects with pagination </summary>
    Task<IEnumerable<Project>> GetPublicProjectsAsync(int skip = 0, int take = 50);

    /// <summary> Search projects by term </summary>
    Task<IEnumerable<Project>> SearchProjectsAsync(string searchTerm, int skip = 0, int take = 50);

    #endregion

    #region Tenant Integration

    /// <summary> Get projects by tenant </summary>
    Task<IEnumerable<Project>> GetProjectsByTenantAsync(Guid tenantId, int skip = 0, int take = 50);

    /// <summary> Move project to different tenant </summary>
    Task<bool> MoveProjectToTenantAsync(Guid projectId, Guid? tenantId);

    #endregion
}
