using Microsoft.EntityFrameworkCore;
using GameGuild.Data;
using GameGuild.Modules.Project.Models;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;

namespace GameGuild.Modules.Project.Services;

/// <summary>
/// Service implementation for Project business logic
/// Provides operations for managing projects, collaborators, releases, and access control
/// </summary>
public class ProjectService(ApplicationDbContext context) : IProjectService {
  #region Basic CRUD Operations

  public async Task<Models.Project?> GetProjectByIdAsync(Guid id) { return await context.Projects.Include(p => p.CreatedBy).Include(p => p.Category).Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Id == id); }

  public async Task<Models.Project?> GetProjectByIdWithDetailsAsync(Guid id) {
    return await context.Projects.Include(p => p.CreatedBy)
                        .Include(p => p.Category)
                        .Include(p => p.Tenant)
                        .Include(p => p.Collaborators)
                        .ThenInclude(c => c.User)
                        .Include(p => p.Releases)
                        .Include(p => p.Versions)
                        .Include(p => p.ProjectMetadata)
                        .Include(p => p.Teams)
                        .ThenInclude(t => t.Team)
                        .ThenInclude(t => t.Members)
                        .Include(p => p.Followers)
                        .ThenInclude(f => f.User)
                        .Include(p => p.Feedbacks.Where(f => f.Status == ContentStatus.Published))
                        .ThenInclude(f => f.User)
                        .Include(p => p.JamSubmissions)
                        .ThenInclude(js => js.Jam)
                        .Where(p => p.DeletedAt == null)
                        .FirstOrDefaultAsync(p => p.Id == id);
  }

  public async Task<Models.Project?> GetProjectBySlugAsync(string slug) { return await context.Projects.Include(p => p.CreatedBy).Include(p => p.Category).Where(p => p.DeletedAt == null).FirstOrDefaultAsync(p => p.Slug == slug); }

  public async Task<IEnumerable<Models.Project>> GetProjectsAsync(int skip = 0, int take = 50) {
    return await context.Projects.Include(p => p.CreatedBy).Include(p => p.Category).Where(p => p.DeletedAt == null).OrderByDescending(p => p.CreatedAt).Skip(skip).Take(take).ToListAsync();
  }

  public async Task<IEnumerable<Models.Project>> GetAllProjectsAsync() { return await context.Projects.Include(p => p.CreatedBy).Include(p => p.Category).Where(p => p.DeletedAt == null).OrderByDescending(p => p.CreatedAt).ToListAsync(); }

  #endregion

  #region Filtered Queries

  public async Task<IEnumerable<Models.Project>> GetProjectsByCategoryAsync(Guid categoryId) {
    return await context.Projects.Include(p => p.CreatedBy).Include(p => p.Category).Where(p => p.CategoryId == categoryId && p.DeletedAt == null).OrderByDescending(p => p.CreatedAt).ToListAsync();
  }

  public async Task<IEnumerable<Models.Project>> GetProjectsByCreatorAsync(Guid creatorId) {
    return await context.Projects.Include(p => p.CreatedBy).Include(p => p.Category).Where(p => p.CreatedById == creatorId && p.DeletedAt == null).OrderByDescending(p => p.CreatedAt).ToListAsync();
  }

  public async Task<IEnumerable<Models.Project>> GetProjectsByStatusAsync(ContentStatus status) {
    return await context.Projects.Include(p => p.CreatedBy).Include(p => p.Category).Where(p => p.Status == status && p.DeletedAt == null).OrderByDescending(p => p.CreatedAt).ToListAsync();
  }

  public async Task<IEnumerable<Models.Project>> GetProjectsByTypeAsync(ProjectType type) {
    return await context.Projects.Include(p => p.CreatedBy).Include(p => p.Category).Where(p => p.Type == type && p.DeletedAt == null).OrderByDescending(p => p.CreatedAt).ToListAsync();
  }

  public async Task<IEnumerable<Models.Project>> GetProjectsByDevelopmentStatusAsync(DevelopmentStatus status) {
    return await context.Projects.Include(p => p.CreatedBy).Include(p => p.Category).Where(p => p.DevelopmentStatus == status && p.DeletedAt == null).OrderByDescending(p => p.CreatedAt).ToListAsync();
  }

  public async Task<IEnumerable<Models.Project>> GetPublicProjectsAsync(int skip = 0, int take = 50) {
    // Explicitly ignore global query filters for public projects
    // This is necessary because we need to show public projects regardless of tenant context
    return await context.Projects.IgnoreQueryFilters() // Bypass global tenant and other filters
                        .Include(p => p.CreatedBy)
                        .Include(p => p.Category)
                        .Where(p => p.Status == ContentStatus.Published && p.Visibility == AccessLevel.Public && p.DeletedAt == null)
                        .OrderByDescending(p => p.CreatedAt)
                        .Skip(skip)
                        .Take(take)
                        .ToListAsync();
  }

  public async Task<IEnumerable<Models.Project>> SearchProjectsAsync(string searchTerm, int skip = 0, int take = 50) {
    string lowerSearchTerm = searchTerm.ToLower();

    return await context.Projects.Include(p => p.CreatedBy)
                        .Include(p => p.Category)
                        .Where(p => p.DeletedAt == null &&
                                    (p.Title.ToLower().Contains(lowerSearchTerm) ||
                                     (p.Description != null && p.Description.ToLower().Contains(lowerSearchTerm)) ||
                                     (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(lowerSearchTerm)))
                        )
                        .OrderByDescending(p => p.CreatedAt)
                        .Skip(skip)
                        .Take(take)
                        .ToListAsync();
  }

  #endregion

  #region Create, Update, Delete

  public async Task<Models.Project> CreateProjectAsync(Models.Project project) {
    // Set timestamps
    project.CreatedAt = DateTime.UtcNow;
    project.UpdatedAt = DateTime.UtcNow;

    // Generate slug if not provided
    if (string.IsNullOrEmpty(project.Slug)) { project.Slug = Models.Project.GenerateSlug(project.Title); }

    // Set default values
    if (project.Status == default) project.Status = ContentStatus.Draft;

    if (project.Visibility == default) project.Visibility = AccessLevel.Private;

    context.Projects.Add(project);
    await context.SaveChangesAsync();

    return project;
  }

  public async Task<Models.Project> UpdateProjectAsync(Models.Project project) {
    Models.Project? existingProject = await GetProjectByIdAsync(project.Id);

    if (existingProject == null) throw new InvalidOperationException($"Project with ID {project.Id} not found");

    // Update properties
    existingProject.Title = project.Title;
    existingProject.Description = project.Description;
    existingProject.ShortDescription = project.ShortDescription;
    existingProject.ImageUrl = project.ImageUrl;
    existingProject.Type = project.Type;
    existingProject.DevelopmentStatus = project.DevelopmentStatus;
    existingProject.CategoryId = project.CategoryId;
    existingProject.WebsiteUrl = project.WebsiteUrl;
    existingProject.RepositoryUrl = project.RepositoryUrl;
    existingProject.DownloadUrl = project.DownloadUrl;
    existingProject.SocialLinks = project.SocialLinks;
    existingProject.Tags = project.Tags;
    existingProject.Status = project.Status;
    existingProject.Visibility = project.Visibility;
    existingProject.UpdatedAt = DateTime.UtcNow;

    // Update slug if title changed
    if (existingProject.Title != project.Title) {
      // Slug is computed from Title automatically
    }

    await context.SaveChangesAsync();

    return existingProject;
  }

  public async Task<bool> DeleteProjectAsync(Guid id) {
    Models.Project? project = await GetProjectByIdAsync(id);

    if (project == null) return false;

    // Softly delete
    project.DeletedAt = DateTime.UtcNow;
    project.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync();

    return true;
  }

  public async Task<bool> RestoreProjectAsync(Guid id) {
    Models.Project? project = await context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt != null);

    if (project == null) return false;

    project.DeletedAt = null;
    project.DeletedAt = null;
    project.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync();

    return true;
  }

  #endregion

  #region Deleted Projects

  public async Task<IEnumerable<Models.Project>> GetDeletedProjectsAsync() {
    return await context.Projects.Include(p => p.CreatedBy).Include(p => p.Category).Where(p => p.DeletedAt != null).OrderByDescending(p => p.DeletedAt).ToListAsync();
  }

  #endregion

  #region Helper Methods

  /*
      private async Task<string> GenerateSlugAsync(string title)
      {
          if (string.IsNullOrWhiteSpace(title))
              return Guid.NewGuid().ToString();

          var baseSlug = GenerateSlugFromTitle(title);
          var slug = baseSlug;
          var counter = 1;

          // Ensure slug is unique
          while (await context.Projects.AnyAsync(p => p.Slug == slug))
          {
              slug = $"{baseSlug}-{counter}";
              counter++;
          }

          return slug;
      }
  */

  /*
      private static string GenerateSlugFromTitle(string title)
      {
          return title.ToLowerInvariant()
              .Replace(" ", "-")
              .Replace("_", "-")
              .Replace(".", "-")
              .Replace("(", "")
              .Replace(")", "")
              .Replace("[", "")
              .Replace("]", "")
              .Replace("{", "")
              .Replace("}", "")
              .Replace(",", "")
              .Replace(";", "")
              .Replace(":", "")
              .Replace("'", "")
              .Replace("\"", "")
              .Replace("!", "")
              .Replace("?", "")
              .Replace("@", "")
              .Replace("#", "")
              .Replace("$", "")
              .Replace("%", "")
              .Replace("^", "")
              .Replace("&", "")
              .Replace("*", "")
              .Replace("+", "")
              .Replace("=", "")
              .Replace("|", "")
              .Replace("\\", "")
              .Replace("/", "")
              .Replace("<", "")
              .Replace(">", "");
      }
  */

  #endregion

  #region Team Integration

  public async Task<ProjectTeam> AddTeamToProjectAsync(Guid projectId, Guid teamId, string role, string? permissions = null) {
    var projectTeam = new ProjectTeam {
      ProjectId = projectId,
      TeamId = teamId,
      Role = role,
      Permissions = permissions,
      AssignedAt = DateTime.UtcNow,
      IsActive = true
    };

    context.ProjectTeams.Add(projectTeam);
    await context.SaveChangesAsync();

    return projectTeam;
  }

  public async Task<bool> RemoveTeamFromProjectAsync(Guid projectId, Guid teamId) {
    ProjectTeam? projectTeam = await context.ProjectTeams.FirstOrDefaultAsync(pt => pt.ProjectId == projectId && pt.TeamId == teamId);

    if (projectTeam == null) return false;

    projectTeam.IsActive = false;
    projectTeam.EndedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<IEnumerable<ProjectTeam>> GetProjectTeamsAsync(Guid projectId) { return await context.ProjectTeams.Include(pt => pt.Team).ThenInclude(t => t.Members).Where(pt => pt.ProjectId == projectId && pt.IsActive).ToListAsync(); }

  public async Task<IEnumerable<Models.Project>> GetProjectsByTeamAsync(Guid teamId) {
    return await context.ProjectTeams.Include(pt => pt.Project)
                        .ThenInclude(p => p.CreatedBy)
                        .Include(pt => pt.Project)
                        .ThenInclude(p => p.Category)
                        .Where(pt => pt.TeamId == teamId && pt.IsActive && pt.Project.DeletedAt == null)
                        .Select(pt => pt.Project)
                        .ToListAsync();
  }

  #endregion

  #region Follower Integration

  public async Task<ProjectFollower> FollowProjectAsync(Guid projectId, Guid userId, bool emailNotifications = true, bool pushNotifications = true) {
    // Check if already following
    ProjectFollower? existing = await context.ProjectFollowers.FirstOrDefaultAsync(pf => pf.ProjectId == projectId && pf.UserId == userId);

    if (existing != null) return existing;

    var follower = new ProjectFollower {
      ProjectId = projectId,
      UserId = userId,
      FollowedAt = DateTime.UtcNow,
      EmailNotifications = emailNotifications,
      PushNotifications = pushNotifications
    };

    context.ProjectFollowers.Add(follower);
    await context.SaveChangesAsync();

    return follower;
  }

  public async Task<bool> UnfollowProjectAsync(Guid projectId, Guid userId) {
    ProjectFollower? follower = await context.ProjectFollowers.FirstOrDefaultAsync(pf => pf.ProjectId == projectId && pf.UserId == userId);

    if (follower == null) return false;

    context.ProjectFollowers.Remove(follower);
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<bool> IsUserFollowingProjectAsync(Guid projectId, Guid userId) { return await context.ProjectFollowers.AnyAsync(pf => pf.ProjectId == projectId && pf.UserId == userId); }

  public async Task<IEnumerable<ProjectFollower>> GetProjectFollowersAsync(Guid projectId) { return await context.ProjectFollowers.Include(pf => pf.User).Where(pf => pf.ProjectId == projectId).OrderBy(pf => pf.FollowedAt).ToListAsync(); }

  public async Task<IEnumerable<Models.Project>> GetProjectsFollowedByUserAsync(Guid userId) {
    return await context.ProjectFollowers.Include(pf => pf.Project)
                        .ThenInclude(p => p.CreatedBy)
                        .Include(pf => pf.Project)
                        .ThenInclude(p => p.Category)
                        .Where(pf => pf.UserId == userId && pf.Project.DeletedAt == null)
                        .Select(pf => pf.Project)
                        .ToListAsync();
  }

  #endregion

  #region Feedback Integration

  public async Task<ProjectFeedback> AddProjectFeedbackAsync(Guid projectId, Guid userId, int rating, string title, string? content = null) {
    // Check if the user already has feedback for this project
    ProjectFeedback? existing = await context.ProjectFeedbacks.FirstOrDefaultAsync(pf => pf.ProjectId == projectId && pf.UserId == userId);

    if (existing != null) {
      // Update existing feedback
      existing.Rating = rating;
      existing.Title = title;
      existing.Content = content;
      existing.UpdatedAt = DateTime.UtcNow;
      await context.SaveChangesAsync();

      return existing;
    }

    var feedback = new ProjectFeedback {
      ProjectId = projectId,
      UserId = userId,
      Rating = rating,
      Title = title,
      Content = content,
      Status = ContentStatus.Published
    };

    context.ProjectFeedbacks.Add(feedback);
    await context.SaveChangesAsync();

    return feedback;
  }

  public async Task<ProjectFeedback> UpdateProjectFeedbackAsync(Guid feedbackId, int rating, string title, string? content = null) {
    ProjectFeedback? feedback = await context.ProjectFeedbacks.FindAsync(feedbackId);

    if (feedback == null) throw new ArgumentException("Feedback not found", nameof(feedbackId));

    feedback.Rating = rating;
    feedback.Title = title;
    feedback.Content = content;
    feedback.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync();

    return feedback;
  }

  public async Task<bool> DeleteProjectFeedbackAsync(Guid feedbackId) {
    ProjectFeedback? feedback = await context.ProjectFeedbacks.FindAsync(feedbackId);

    if (feedback == null) return false;

    context.ProjectFeedbacks.Remove(feedback);
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<IEnumerable<ProjectFeedback>> GetProjectFeedbackAsync(Guid projectId, int skip = 0, int take = 50) {
    return await context.ProjectFeedbacks.Include(pf => pf.User).Where(pf => pf.ProjectId == projectId && pf.Status == ContentStatus.Published).OrderByDescending(pf => pf.CreatedAt).Skip(skip).Take(take).ToListAsync();
  }

  public async Task<ProjectFeedback?> GetUserFeedbackForProjectAsync(Guid projectId, Guid userId) { return await context.ProjectFeedbacks.Include(pf => pf.User).FirstOrDefaultAsync(pf => pf.ProjectId == projectId && pf.UserId == userId); }

  #endregion

  #region Jam Integration

  public async Task<ProjectJamSubmission> SubmitProjectToJamAsync(Guid projectId, Guid jamId, string? submissionNotes = null) {
    // Check if already submitted
    ProjectJamSubmission? existing = await context.ProjectJamSubmissions.FirstOrDefaultAsync(pjs => pjs.ProjectId == projectId && pjs.JamId == jamId);

    if (existing != null) return existing;

    var submission = new ProjectJamSubmission {
      ProjectId = projectId,
      JamId = jamId,
      SubmittedAt = DateTime.UtcNow,
      SubmissionNotes = submissionNotes,
      IsEligible = true
    };

    context.ProjectJamSubmissions.Add(submission);
    await context.SaveChangesAsync();

    return submission;
  }

  public async Task<bool> RemoveProjectFromJamAsync(Guid projectId, Guid jamId) {
    ProjectJamSubmission? submission = await context.ProjectJamSubmissions.FirstOrDefaultAsync(pjs => pjs.ProjectId == projectId && pjs.JamId == jamId);

    if (submission == null) return false;

    context.ProjectJamSubmissions.Remove(submission);
    await context.SaveChangesAsync();

    return true;
  }

  public async Task<IEnumerable<ProjectJamSubmission>> GetProjectJamSubmissionsAsync(Guid projectId) {
    return await context.ProjectJamSubmissions.Include(pjs => pjs.Jam).Include(pjs => pjs.Scores).Where(pjs => pjs.ProjectId == projectId).ToListAsync();
  }

  public async Task<IEnumerable<Models.Project>> GetProjectsByJamAsync(Guid jamId) {
    return await context.ProjectJamSubmissions.Include(pjs => pjs.Project)
                        .ThenInclude(p => p.CreatedBy)
                        .Include(pjs => pjs.Project)
                        .ThenInclude(p => p.Category)
                        .Where(pjs => pjs.JamId == jamId && pjs.Project.DeletedAt == null)
                        .Select(pjs => pjs.Project)
                        .ToListAsync();
  }

  #endregion

  #region Tenant Integration

  public async Task<IEnumerable<Models.Project>> GetProjectsByTenantAsync(Guid tenantId, int skip = 0, int take = 50) {
    return await context.Projects.Include(p => p.CreatedBy).Include(p => p.Category).Where(p => p.TenantId == tenantId && p.DeletedAt == null).OrderByDescending(p => p.CreatedAt).Skip(skip).Take(take).ToListAsync();
  }

  public async Task<bool> MoveProjectToTenantAsync(Guid projectId, Guid? tenantId) {
    Models.Project? project = await context.Projects.FindAsync(projectId);

    if (project == null) return false;

    project.TenantId = tenantId;
    project.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();

    return true;
  }

  #endregion

  #region Analytics and Statistics

  public async Task<ProjectStatistics> GetProjectStatisticsAsync(Guid projectId) {
    Models.Project? project = await context.Projects.Include(p => p.Followers)
                                           .Include(p => p.Feedbacks)
                                           .Include(p => p.Teams)
                                           .Include(p => p.Collaborators)
                                           .Include(p => p.Releases)
                                           .Include(p => p.JamSubmissions)
                                           .FirstOrDefaultAsync(p => p.Id == projectId);

    if (project == null) throw new ArgumentException("Project not found", nameof(projectId));

    DateTime thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

    return new ProjectStatistics {
      ProjectId = projectId,
      FollowerCount = project.Followers.Count,
      FeedbackCount = project.Feedbacks.Count(f => f.Status == ContentStatus.Published),
      AverageRating = project.Feedbacks.Any(f => f.Status == ContentStatus.Published) ? (decimal?)project.Feedbacks.Where(f => f.Status == ContentStatus.Published).Average(f => f.Rating) : null,
      TotalDownloads = project.Releases.Sum(r => r.DownloadCount),
      ActiveTeamCount = project.Teams.Count(t => t.IsActive),
      CollaboratorCount = project.Collaborators.Count,
      ReleaseCount = project.Releases.Count,
      JamSubmissionCount = project.JamSubmissions.Count,
      AwardCount = project.JamSubmissions.Count(js => js.HasAward),
      NewFollowersLast30Days = project.Followers.Count(f => f.FollowedAt >= thirtyDaysAgo),
      CalculatedAt = DateTime.UtcNow,
      TrendingScore = CalculateTrendingScore(project, thirtyDaysAgo)
    };
  }

  public async Task<IEnumerable<Models.Project>> GetTrendingProjectsAsync(int take = 10, TimeSpan? timeWindow = null) {
    DateTime cutoffDate = DateTime.UtcNow.Subtract(timeWindow ?? TimeSpan.FromDays(7));

    var projects = await context.Projects.Include(p => p.CreatedBy)
                                .Include(p => p.Category)
                                .Include(p => p.Followers)
                                .Include(p => p.Feedbacks)
                                .Include(p => p.Releases)
                                .Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published)
                                .ToListAsync();

    return projects.OrderByDescending(p => CalculateTrendingScore(p, cutoffDate)).Take(take).ToList();
  }

  public async Task<IEnumerable<Models.Project>> GetPopularProjectsAsync(int take = 10) {
    return await context.Projects.Include(p => p.CreatedBy)
                        .Include(p => p.Category)
                        .Include(p => p.Releases)
                        .Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published)
                        .OrderByDescending(p => p.Releases.Sum(r => r.DownloadCount))
                        .ThenByDescending(p => p.Followers.Count)
                        .Take(take)
                        .ToListAsync();
  }

  private static decimal CalculateTrendingScore(Models.Project project, DateTime cutoffDate) {
    int recentFollowers = project.Followers.Count(f => f.FollowedAt >= cutoffDate);
    int recentFeedback = project.Feedbacks.Count(f => f.CreatedAt >= cutoffDate);
    int totalDownloads = project.Releases.Sum(r => r.DownloadCount);
    double averageRating = project.Feedbacks.Any() ? project.Feedbacks.Where(f => f.Status == ContentStatus.Published).Average(f => f.Rating) : 0;

    // Weighted trending score
    return (decimal)((recentFollowers * 2.0) + (recentFeedback * 1.5) + (totalDownloads * 0.001) + (averageRating * 0.5));
  }

  #endregion
}
