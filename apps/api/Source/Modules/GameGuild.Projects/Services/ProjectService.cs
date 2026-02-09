using Microsoft.EntityFrameworkCore;

namespace GameGuild.Projects;

/// <summary>
/// Thin facade that delegates to <see cref="IProjectCrudService"/> and <see cref="IProjectEngagementService"/>.
/// Kept for backward compatibility — all existing consumers of <see cref="IProjectService"/> continue to work.
/// </summary>
public class ProjectService(
    IProjectCrudService crudService,
    IProjectEngagementService engagementService) : IProjectService
{
    // ── Deleted Projects ──────────────────────────────────────────────

    public Task<IEnumerable<Project>> GetDeletedProjectsAsync()
        => crudService.GetDeletedProjectsAsync();

    // ── Basic CRUD ────────────────────────────────────────────────────

    public Task<IEnumerable<Project>> GetAllProjectsAsync()
        => crudService.GetAllProjectsAsync();

    public Task<IEnumerable<Project>> GetProjectsAsync(int skip = 0, int take = 50)
        => crudService.GetProjectsAsync(skip, take);

    public Task<IEnumerable<Project>> GetProjectsOptimizedAsync(int skip = 0, int take = 50)
        => crudService.GetProjectsOptimizedAsync(skip, take);

    public Task<Project?> GetProjectByIdAsync(Guid id)
        => crudService.GetProjectByIdAsync(id);

    public Task<Project?> GetProjectByIdWithDetailsAsync(Guid id)
        => crudService.GetProjectByIdWithDetailsAsync(id);

    public Task<Project?> GetProjectBySlugAsync(string slug)
        => crudService.GetProjectBySlugAsync(slug);

    public Task<Project> CreateProjectAsync(Project project)
        => crudService.CreateProjectAsync(project);

    public Task<Project> UpdateProjectAsync(Project project)
        => crudService.UpdateProjectAsync(project);

    public Task<bool> DeleteProjectAsync(Guid id)
        => crudService.DeleteProjectAsync(id);

    public Task<bool> RestoreProjectAsync(Guid id)
        => crudService.RestoreProjectAsync(id);

    // ── Filtered Queries ──────────────────────────────────────────────

    public Task<IEnumerable<Project>> GetProjectsByCategoryAsync(Guid categoryId)
        => crudService.GetProjectsByCategoryAsync(categoryId);

    public Task<IEnumerable<Project>> GetProjectsByCreatorAsync(Guid creatorId)
        => crudService.GetProjectsByCreatorAsync(creatorId);

    public Task<IEnumerable<Project>> GetProjectsByStatusAsync(ContentStatus status)
        => crudService.GetProjectsByStatusAsync(status);

    public Task<IEnumerable<Project>> GetProjectsByTypeAsync(ProjectType type)
        => crudService.GetProjectsByTypeAsync(type);

    public Task<IEnumerable<Project>> GetProjectsByDevelopmentStatusAsync(DevelopmentStatus status)
        => crudService.GetProjectsByDevelopmentStatusAsync(status);

    public Task<IEnumerable<Project>> GetPublicProjectsAsync(int skip = 0, int take = 50)
        => crudService.GetPublicProjectsAsync(skip, take);

    public Task<IEnumerable<Project>> SearchProjectsAsync(string searchTerm, int skip = 0, int take = 50)
        => crudService.SearchProjectsAsync(searchTerm, skip, take);

    // ── Tenant Integration ────────────────────────────────────────────

    public Task<IEnumerable<Project>> GetProjectsByTenantAsync(Guid tenantId, int skip = 0, int take = 50)
        => crudService.GetProjectsByTenantAsync(tenantId, skip, take);

    public Task<bool> MoveProjectToTenantAsync(Guid projectId, Guid? tenantId)
        => crudService.MoveProjectToTenantAsync(projectId, tenantId);

    // ── Team Integration ──────────────────────────────────────────────

    public Task<ProjectTeam> AddTeamToProjectAsync(Guid projectId, Guid teamId, string role, string? permissions = null)
        => engagementService.AddTeamToProjectAsync(projectId, teamId, role, permissions);

    public Task<bool> RemoveTeamFromProjectAsync(Guid projectId, Guid teamId)
        => engagementService.RemoveTeamFromProjectAsync(projectId, teamId);

    public Task<IEnumerable<ProjectTeam>> GetProjectTeamsAsync(Guid projectId)
        => engagementService.GetProjectTeamsAsync(projectId);

    public Task<IEnumerable<Project>> GetProjectsByTeamAsync(Guid teamId)
        => engagementService.GetProjectsByTeamAsync(teamId);

    // ── Follower Integration ──────────────────────────────────────────

    public Task<ProjectFollower> FollowProjectAsync(Guid projectId, Guid userId, bool emailNotifications = true, bool pushNotifications = true)
        => engagementService.FollowProjectAsync(projectId, userId, emailNotifications, pushNotifications);

    public Task<bool> UnfollowProjectAsync(Guid projectId, Guid userId)
        => engagementService.UnfollowProjectAsync(projectId, userId);

    public Task<bool> IsUserFollowingProjectAsync(Guid projectId, Guid userId)
        => engagementService.IsUserFollowingProjectAsync(projectId, userId);

    public Task<IEnumerable<ProjectFollower>> GetProjectFollowersAsync(Guid projectId)
        => engagementService.GetProjectFollowersAsync(projectId);

    public Task<IEnumerable<Project>> GetProjectsFollowedByUserAsync(Guid userId)
        => engagementService.GetProjectsFollowedByUserAsync(userId);

    // ── Feedback Integration ──────────────────────────────────────────

    public Task<ProjectFeedback> AddProjectFeedbackAsync(Guid projectId, Guid userId, int rating, string title, string? content = null)
        => engagementService.AddProjectFeedbackAsync(projectId, userId, rating, title, content);

    public Task<ProjectFeedback> UpdateProjectFeedbackAsync(Guid feedbackId, int rating, string title, string? content = null)
        => engagementService.UpdateProjectFeedbackAsync(feedbackId, rating, title, content);

    public Task<bool> DeleteProjectFeedbackAsync(Guid feedbackId)
        => engagementService.DeleteProjectFeedbackAsync(feedbackId);

    public Task<IEnumerable<ProjectFeedback>> GetProjectFeedbackAsync(Guid projectId, int skip = 0, int take = 50)
        => engagementService.GetProjectFeedbackAsync(projectId, skip, take);

    public Task<ProjectFeedback?> GetUserFeedbackForProjectAsync(Guid projectId, Guid userId)
        => engagementService.GetUserFeedbackForProjectAsync(projectId, userId);

    // ── Jam Integration ───────────────────────────────────────────────

    public Task<ProjectJamSubmission> SubmitProjectToJamAsync(Guid projectId, Guid jamId, string? submissionNotes = null)
        => engagementService.SubmitProjectToJamAsync(projectId, jamId, submissionNotes);

    public Task<bool> RemoveProjectFromJamAsync(Guid projectId, Guid jamId)
        => engagementService.RemoveProjectFromJamAsync(projectId, jamId);

    public Task<IEnumerable<ProjectJamSubmission>> GetProjectJamSubmissionsAsync(Guid projectId)
        => engagementService.GetProjectJamSubmissionsAsync(projectId);

    public Task<IEnumerable<Project>> GetProjectsByJamAsync(Guid jamId)
        => engagementService.GetProjectsByJamAsync(jamId);

    // ── Analytics & Statistics ─────────────────────────────────────────

    public Task<ProjectStatistics> GetProjectStatisticsAsync(Guid projectId)
        => engagementService.GetProjectStatisticsAsync(projectId);

    public Task<IEnumerable<Project>> GetTrendingProjectsAsync(int take = 10, TimeSpan? timeWindow = null)
        => engagementService.GetTrendingProjectsAsync(take, timeWindow);

    public Task<IEnumerable<Project>> GetPopularProjectsAsync(int take = 10)
        => engagementService.GetPopularProjectsAsync(take);
}
