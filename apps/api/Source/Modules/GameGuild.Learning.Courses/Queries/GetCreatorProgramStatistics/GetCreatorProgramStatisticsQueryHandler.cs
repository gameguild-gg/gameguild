using GameGuild.Abstractions;
using GameGuild.CQRS;
using GameGuild.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Query handler for GetCreatorProgramStatisticsQuery
/// Calculates aggregate statistics for all programs created by a specific user
/// </summary>
public class GetCreatorProgramStatisticsQueryHandler(
    IApplicationDbContext context,
    ILogger<GetCreatorProgramStatisticsQueryHandler> logger)
    : IQueryHandler<GetCreatorProgramStatisticsQuery, CreatorProgramStatistics>
{
    public async Task<CreatorProgramStatistics> Handle(
        GetCreatorProgramStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting creator program statistics for creator: {CreatorId}", request.CreatorId);

        // Get all programs by this creator
        var programsQuery = context.Set<Program>()
            .AsNoTracking()
            .Where(p => p.CreatorId == request.CreatorId && p.DeletedAt == null);

        // Apply date filters if provided
        if (request.FromDate.HasValue)
            programsQuery = programsQuery.Where(p => p.CreatedAt >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            programsQuery = programsQuery.Where(p => p.CreatedAt <= request.ToDate.Value);

        var programIds = await programsQuery.Select(p => p.Id).ToListAsync(cancellationToken);

        var totalPrograms = programIds.Count;
        var publishedPrograms = await programsQuery
            .CountAsync(p => p.Status == ContentStatus.Published, cancellationToken);

        // Get enrollment statistics
        var enrollmentsQuery = context.Set<ProgramEnrollment>()
            .AsNoTracking()
            .Where(e => programIds.Contains(e.ProgramId) && e.DeletedAt == null);

        var totalEnrollments = await enrollmentsQuery.CountAsync(cancellationToken);
        var activeEnrollments = await enrollmentsQuery
            .CountAsync(e => e.EnrollmentStatus == EnrollmentStatus.Active, cancellationToken);

        // Get rating statistics
        var ratingsQuery = context.Set<ProgramRating>()
            .AsNoTracking()
            .Where(r => programIds.Contains(r.ProgramId));

        var totalRatings = await ratingsQuery.CountAsync(cancellationToken);
        var averageRating = totalRatings > 0
            ? await ratingsQuery.AverageAsync(r => r.Rating, cancellationToken)
            : 0m;

        // Calculate average completion rate
        var completedEnrollments = await enrollmentsQuery
            .CountAsync(e => e.CompletionStatus == CompletionStatus.Completed ||
                           e.CompletionStatus == CompletionStatus.CompletedWithCertificate, cancellationToken);
        var averageCompletionRate = totalEnrollments > 0
            ? (decimal)completedEnrollments / totalEnrollments * 100
            : 0m;

        logger.LogInformation(
            "Creator {CreatorId} statistics: {TotalPrograms} programs, {TotalEnrollments} enrollments",
            request.CreatorId, totalPrograms, totalEnrollments);

        return new CreatorProgramStatistics(
            CreatorId: request.CreatorId,
            TotalPrograms: totalPrograms,
            PublishedPrograms: publishedPrograms,
            TotalEnrollments: totalEnrollments,
            ActiveEnrollments: activeEnrollments,
            AverageRating: Math.Round(averageRating, 2),
            TotalRatings: totalRatings,
            AverageCompletionRate: Math.Round(averageCompletionRate, 2));
    }
}
