using GameGuild.Common;
using GameGuild.Modules.Programs.Interfaces;
using GameGuild.Modules.Programs.Models;
using HotChocolate;

namespace GameGuild.Modules.Programs.GraphQL;

/// <summary>
/// GraphQL mutations for content progress tracking
/// </summary>
[ExtendObjectType<Mutation>]
public class ContentProgressMutations
{
    /// <summary>
    /// Track user access to content
    /// </summary>
    public async Task<ContentProgressResult> TrackContentAccess(
        Guid contentId,
        Guid programEnrollmentId,
        [Service] IContentProgressService progressService)
    {
        try
        {
            // TODO: Implement proper current user service
            var currentUserId = GetCurrentUserId();

            var progress = await progressService.StartContentAsync(programEnrollmentId, contentId);

            return new ContentProgressResult
            {
                Success = true,
                Progress = progress
            };
        }
        catch (Exception ex)
        {
            return new ContentProgressResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Update content progress
    /// </summary>
    public async Task<ContentProgressResult> UpdateContentProgress(
        Guid contentId,
        decimal progressPercentage,
        int? timeSpentSeconds,
        [Service] IContentProgressService progressService)
    {
        try
        {
            // TODO: Implement proper current user service
            var currentUserId = GetCurrentUserId();

            // Find active enrollment and content progress
            var enrollments = await progressService.GetUserActiveEnrollmentsAsync(currentUserId);
            var contentProgress = enrollments.SelectMany(e => e.ContentProgress ?? new List<ContentProgress>())
                .FirstOrDefault(cp => cp.ContentId == contentId);
                
            if (contentProgress == null)
            {
                return new ContentProgressResult
                {
                    Success = false,
                    ErrorMessage = "Content progress not found"
                };
            }

            var progress = await progressService.UpdateProgressAsync(
                contentProgress.Id, (int)progressPercentage, timeSpentSeconds ?? 0);

            return new ContentProgressResult
            {
                Success = true,
                Progress = progress
            };
        }
        catch (Exception ex)
        {
            return new ContentProgressResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Mark content as completed
    /// </summary>
    public async Task<ContentProgressResult> CompleteContent(
        Guid contentId,
        decimal? score,
        decimal? maxScore,
        [Service] IContentProgressService progressService)
    {
        try
        {
            // TODO: Implement proper current user service
            var currentUserId = GetCurrentUserId();

            // Find active enrollment and content progress
            var enrollments = await progressService.GetUserActiveEnrollmentsAsync(currentUserId);
            var contentProgress = enrollments.SelectMany(e => e.ContentProgress ?? new List<ContentProgress>())
                .FirstOrDefault(cp => cp.ContentId == contentId);
                
            if (contentProgress == null)
            {
                return new ContentProgressResult
                {
                    Success = false,
                    ErrorMessage = "Content progress not found"
                };
            }

            var progress = await progressService.CompleteContentAsync(contentProgress.Id, score ?? 0, maxScore ?? 100);

            return new ContentProgressResult
            {
                Success = true,
                Progress = progress
            };
        }
        catch (Exception ex)
        {
            return new ContentProgressResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    
    // Helper method to get current user ID (temporary implementation)
    private static Guid GetCurrentUserId()
    {
        // TODO: Implement proper current user service with JWT token extraction
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }
}

/// <summary>
/// Result type for content progress operations
/// </summary>
public class ContentProgressResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ContentProgress? Progress { get; set; }
}
