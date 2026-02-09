namespace GameGuild.Projects;

/// <summary>
/// Service interface for project engagement: teams, followers, feedback, jams, and analytics.
/// </summary>
public interface IProjectEngagementService
{
    #region Team Integration

    /// <summary> Add a team to a project </summary>
    Task<ProjectTeam> AddTeamToProjectAsync(Guid projectId, Guid teamId, string role, string? permissions = null);

    /// <summary> Remove a team from a project </summary>
    Task<bool> RemoveTeamFromProjectAsync(Guid projectId, Guid teamId);

    /// <summary> Get all teams for a project </summary>
    Task<IEnumerable<ProjectTeam>> GetProjectTeamsAsync(Guid projectId);

    /// <summary> Get all projects for a team </summary>
    Task<IEnumerable<Project>> GetProjectsByTeamAsync(Guid teamId);

    #endregion

    #region Follower Integration

    /// <summary> Follow a project </summary>
    Task<ProjectFollower> FollowProjectAsync(Guid projectId, Guid userId, bool emailNotifications = true, bool pushNotifications = true);

    /// <summary> Unfollow a project </summary>
    Task<bool> UnfollowProjectAsync(Guid projectId, Guid userId);

    /// <summary> Check if a user is following a project </summary>
    Task<bool> IsUserFollowingProjectAsync(Guid projectId, Guid userId);

    /// <summary> Get all followers of a project </summary>
    Task<IEnumerable<ProjectFollower>> GetProjectFollowersAsync(Guid projectId);

    /// <summary> Get all projects followed by a user </summary>
    Task<IEnumerable<Project>> GetProjectsFollowedByUserAsync(Guid userId);

    #endregion

    #region Feedback Integration

    /// <summary> Add feedback to a project </summary>
    Task<ProjectFeedback> AddProjectFeedbackAsync(Guid projectId, Guid userId, int rating, string title, string? content = null);

    /// <summary> Update project feedback </summary>
    Task<ProjectFeedback> UpdateProjectFeedbackAsync(Guid feedbackId, int rating, string title, string? content = null);

    /// <summary> Delete project feedback </summary>
    Task<bool> DeleteProjectFeedbackAsync(Guid feedbackId);

    /// <summary> Get all feedback for a project </summary>
    Task<IEnumerable<ProjectFeedback>> GetProjectFeedbackAsync(Guid projectId, int skip = 0, int take = 50);

    /// <summary> Get feedback by user for a project </summary>
    Task<ProjectFeedback?> GetUserFeedbackForProjectAsync(Guid projectId, Guid userId);

    #endregion

    #region Jam Integration

    /// <summary> Submit a project to a jam </summary>
    Task<ProjectJamSubmission> SubmitProjectToJamAsync(Guid projectId, Guid jamId, string? submissionNotes = null);

    /// <summary> Remove a project from a game jam </summary>
    Task<bool> RemoveProjectFromJamAsync(Guid projectId, Guid jamId);

    /// <summary> Get all jam submissions for a project </summary>
    Task<IEnumerable<ProjectJamSubmission>> GetProjectJamSubmissionsAsync(Guid projectId);

    /// <summary> Get all projects submitted to a game jam </summary>
    Task<IEnumerable<Project>> GetProjectsByJamAsync(Guid jamId);

    #endregion

    #region Analytics and Statistics

    /// <summary> Get project statistics </summary>
    Task<ProjectStatistics> GetProjectStatisticsAsync(Guid projectId);

    /// <summary> Get trending projects </summary>
    Task<IEnumerable<Project>> GetTrendingProjectsAsync(int take = 10, TimeSpan? timeWindow = null);

    /// <summary> Get popular projects by downloads </summary>
    Task<IEnumerable<Project>> GetPopularProjectsAsync(int take = 10);

    #endregion
}
