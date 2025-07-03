using GameGuild.Common.Enums;
using GameGuild.Modules.Contents.Models;
using GameGuild.Modules.Projects.Models;


namespace GameGuild.Modules.Projects.Services;

/// <summary>
/// Service interface for Project operations
/// Enhanced with comprehensive project management capabilities
/// </summary>
public interface IProjectService {
  #region Basic CRUD Operations

  /// <summary>
  /// Get all projects (non-deleted only)
  /// </summary>
  Task<IEnumerable<Models.Project>> GetAllProjectsAsync();

  /// <summary>
  /// Get projects with pagination
  /// </summary>
  Task<IEnumerable<Models.Project>> GetProjectsAsync(int skip = 0, int take = 50);

  /// <summary>
  /// Get a project by ID
  /// </summary>
  Task<Models.Project?> GetProjectByIdAsync(Guid id);

  /// <summary>
  /// Get a project by ID with all related details
  /// </summary>
  Task<Models.Project?> GetProjectByIdWithDetailsAsync(Guid id);

  /// <summary>
  /// Get a project by slug
  /// </summary>
  Task<Models.Project?> GetProjectBySlugAsync(string slug);

  #endregion

  #region Filtered Queries

  /// <summary>
  /// Get projects by category
  /// </summary>
  Task<IEnumerable<Models.Project>> GetProjectsByCategoryAsync(Guid categoryId);

  /// <summary>
  /// Get projects by creator
  /// </summary>
  Task<IEnumerable<Models.Project>> GetProjectsByCreatorAsync(Guid creatorId);

  /// <summary>
  /// Get projects with a specific status
  /// </summary>
  Task<IEnumerable<Models.Project>> GetProjectsByStatusAsync(ContentStatus status);

  /// <summary>
  /// Get projects by type
  /// </summary>
  Task<IEnumerable<Models.Project>> GetProjectsByTypeAsync(ProjectType type);

  /// <summary>
  /// Get projects by development status
  /// </summary>
  Task<IEnumerable<Models.Project>> GetProjectsByDevelopmentStatusAsync(DevelopmentStatus status);

  /// <summary>
  /// Get public projects with pagination
  /// </summary>
  Task<IEnumerable<Models.Project>> GetPublicProjectsAsync(int skip = 0, int take = 50);

  /// <summary>
  /// Search projects by term
  /// </summary>
  Task<IEnumerable<Models.Project>> SearchProjectsAsync(string searchTerm, int skip = 0, int take = 50);

  #endregion

  #region Create, Update, Delete

  /// <summary>
  /// Create a new project
  /// </summary>
  Task<Models.Project> CreateProjectAsync(Models.Project project);

  /// <summary>
  /// Update an existing project
  /// </summary>
  Task<Models.Project> UpdateProjectAsync(Models.Project project);

  /// <summary>
  /// Soft delete a project
  /// </summary>
  Task<bool> DeleteProjectAsync(Guid id);

  /// <summary>
  /// Restore a deleted project
  /// </summary>
  Task<bool> RestoreProjectAsync(Guid id);

  #endregion

  #region Deleted Projects

  /// <summary>
  /// Get all deleted projects
  /// </summary>
  Task<IEnumerable<Models.Project>> GetDeletedProjectsAsync();

  #endregion

  #region Team Integration

  /// <summary>
  /// Add a team to a project
  /// </summary>
  Task<ProjectTeam> AddTeamToProjectAsync(Guid projectId, Guid teamId, string role, string? permissions = null);

  /// <summary>
  /// Remove a team from a project
  /// </summary>
  Task<bool> RemoveTeamFromProjectAsync(Guid projectId, Guid teamId);

  /// <summary>
  /// Get all teams for a project
  /// </summary>
  Task<IEnumerable<ProjectTeam>> GetProjectTeamsAsync(Guid projectId);

  /// <summary>
  /// Get all projects for a team
  /// </summary>
  Task<IEnumerable<Models.Project>> GetProjectsByTeamAsync(Guid teamId);

  #endregion

  #region Follower Integration

  /// <summary>
  /// Follow a project
  /// </summary>
  Task<ProjectFollower> FollowProjectAsync(
    Guid projectId, Guid userId, bool emailNotifications = true,
    bool pushNotifications = true
  );

  /// <summary>
  /// Unfollow a project
  /// </summary>
  Task<bool> UnfollowProjectAsync(Guid projectId, Guid userId);

  /// <summary>
  /// Check if a user is following a project
  /// </summary>
  Task<bool> IsUserFollowingProjectAsync(Guid projectId, Guid userId);

  /// <summary>
  /// Get all followers of a project
  /// </summary>
  Task<IEnumerable<ProjectFollower>> GetProjectFollowersAsync(Guid projectId);

  /// <summary>
  /// Get all projects followed by a user
  /// </summary>
  Task<IEnumerable<Models.Project>> GetProjectsFollowedByUserAsync(Guid userId);

  #endregion

  #region Feedback Integration

  /// <summary>
  /// Add feedback to a project
  /// </summary>
  Task<ProjectFeedback> AddProjectFeedbackAsync(
    Guid projectId, Guid userId, int rating, string title,
    string? content = null
  );

  /// <summary>
  /// Update project feedback
  /// </summary>
  Task<ProjectFeedback> UpdateProjectFeedbackAsync(Guid feedbackId, int rating, string title, string? content = null);

  /// <summary>
  /// Delete project feedback
  /// </summary>
  Task<bool> DeleteProjectFeedbackAsync(Guid feedbackId);

  /// <summary>
  /// Get all feedback for a project
  /// </summary>
  Task<IEnumerable<ProjectFeedback>> GetProjectFeedbackAsync(Guid projectId, int skip = 0, int take = 50);

  /// <summary>
  /// Get feedback by user for a project
  /// </summary>
  Task<ProjectFeedback?> GetUserFeedbackForProjectAsync(Guid projectId, Guid userId);

  #endregion

  #region Jam Integration

  /// <summary>
  /// Submit a project to a jam
  /// </summary>
  Task<ProjectJamSubmission> SubmitProjectToJamAsync(Guid projectId, Guid jamId, string? submissionNotes = null);

  /// <summary>
  /// Remove a project from a game jam
  /// </summary>
  Task<bool> RemoveProjectFromJamAsync(Guid projectId, Guid jamId);

  /// <summary>
  /// Get all jam submissions for a project
  /// </summary>
  Task<IEnumerable<ProjectJamSubmission>> GetProjectJamSubmissionsAsync(Guid projectId);

  /// <summary>
  /// Get all projects submitted to a game jam
  /// </summary>
  Task<IEnumerable<Models.Project>> GetProjectsByJamAsync(Guid jamId);

  #endregion

  #region Tenant Integration

  /// <summary>
  /// Get projects by tenant
  /// </summary>
  Task<IEnumerable<Models.Project>> GetProjectsByTenantAsync(Guid tenantId, int skip = 0, int take = 50);

  /// <summary>
  /// Move project to different tenant
  /// </summary>
  Task<bool> MoveProjectToTenantAsync(Guid projectId, Guid? tenantId);

  #endregion

  #region Analytics and Statistics

  /// <summary>
  /// Get project statistics
  /// </summary>
  Task<ProjectStatistics> GetProjectStatisticsAsync(Guid projectId);

  /// <summary>
  /// Get trending projects
  /// </summary>
  Task<IEnumerable<Models.Project>> GetTrendingProjectsAsync(int take = 10, TimeSpan? timeWindow = null);

  /// <summary>
  /// Get popular projects by downloads
  /// </summary>
  Task<IEnumerable<Models.Project>> GetPopularProjectsAsync(int take = 10);

  #endregion
}
