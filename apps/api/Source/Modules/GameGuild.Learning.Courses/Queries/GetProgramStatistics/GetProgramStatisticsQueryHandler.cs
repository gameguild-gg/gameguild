using GameGuild.Abstractions;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Query handler for GetProgramStatisticsQuery
/// Calculates statistics for a specific program
/// </summary>
public class GetProgramStatisticsQueryHandler(
    IApplicationDbContext context,
    ILogger<GetProgramStatisticsQueryHandler> logger)
    : IQueryHandler<GetProgramStatisticsQuery, ProgramStatistics>
{
    public async Task<ProgramStatistics> Handle(
        GetProgramStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting program statistics for program: {ProgramId}", request.ProgramId);

        // Get enrollment statistics
        var enrollmentsQuery = context.Set<ProgramEnrollment>()
            .AsNoTracking()
            .Where(e => e.ProgramId == request.ProgramId && e.DeletedAt == null);

        var totalEnrollments = await enrollmentsQuery.CountAsync(cancellationToken);
        var activeEnrollments = await enrollmentsQuery
            .CountAsync(e => e.EnrollmentStatus == EnrollmentStatus.Active, cancellationToken);
        var completedEnrollments = await enrollmentsQuery
            .CountAsync(e => e.CompletionStatus == CompletionStatus.Completed ||
                           e.CompletionStatus == CompletionStatus.CompletedWithCertificate, cancellationToken);

        // Calculate completion rate
        var completionRate = totalEnrollments > 0
            ? (decimal)completedEnrollments / totalEnrollments * 100
            : 0m;

        // Get rating statistics
        var ratingsQuery = context.Set<ProgramRating>()
            .AsNoTracking()
            .Where(r => r.ProgramId == request.ProgramId);

        var totalRatings = await ratingsQuery.CountAsync(cancellationToken);
        var averageRating = totalRatings > 0
            ? await ratingsQuery.AverageAsync(r => r.Rating, cancellationToken)
            : 0m;

        // Calculate average completion time for completed enrollments
        var completedWithTimes = await enrollmentsQuery
            .Where(e => e.CompletedAt.HasValue && e.EnrolledAt != default)
            .Where(e => e.CompletionStatus == CompletionStatus.Completed ||
                       e.CompletionStatus == CompletionStatus.CompletedWithCertificate)
            .Select(e => new { e.EnrolledAt, e.CompletedAt })
            .ToListAsync(cancellationToken);

        var averageCompletionTime = completedWithTimes.Count > 0
            ? TimeSpan.FromTicks((long)completedWithTimes
                .Average(e => (e.CompletedAt!.Value - e.EnrolledAt).Ticks))
            : TimeSpan.Zero;

        logger.LogInformation(
            "Program {ProgramId} statistics: {TotalEnrollments} enrollments, {CompletedEnrollments} completed",
            request.ProgramId, totalEnrollments, completedEnrollments);

        return new ProgramStatistics(
            ProgramId: request.ProgramId,
            TotalEnrollments: totalEnrollments,
            ActiveEnrollments: activeEnrollments,
            CompletedEnrollments: completedEnrollments,
            AverageRating: Math.Round(averageRating, 2),
            TotalRatings: totalRatings,
            CompletionRate: Math.Round(completionRate, 2),
            AverageCompletionTime: averageCompletionTime);
    }
}
