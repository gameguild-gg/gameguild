using GameGuild.CQRS;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Enrollments;
using GameGuild.TestingLab;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.TestingLab;

public sealed class TestingLabLearningEvidenceHandler(
    IApplicationDbContext context,
    ILogger<TestingLabLearningEvidenceHandler> logger)
    : INotificationHandler<TestingLearningEvidenceCompletedEvent>
{
    public async Task Handle(
        TestingLearningEvidenceCompletedEvent notification,
        CancellationToken cancellationToken)
    {
        if (await context.Set<TestingLabLearningEvidenceReceipt>().AnyAsync(
                receipt => receipt.EvidenceId == notification.EvidenceId,
                cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var activityExists = await context.Set<ProgramContent>().AnyAsync(activity =>
            activity.Id == notification.LearningActivityId &&
            activity.ProgramId == notification.CourseId &&
            activity.DeletedAt == null &&
            (activity.TenantId == null || activity.TenantId == notification.TenantId),
            cancellationToken).ConfigureAwait(false);
        if (!activityExists)
        {
            throw new InvalidOperationException(
                "The linked Learning activity does not belong to the configured course.");
        }

        var programUser = await context.Set<ProgramUser>().FirstOrDefaultAsync(enrollment =>
            enrollment.ProgramId == notification.CourseId &&
            enrollment.UserId == notification.UserId &&
            enrollment.IsActive &&
            enrollment.DeletedAt == null &&
            (enrollment.TenantId == null || enrollment.TenantId == notification.TenantId),
            cancellationToken).ConfigureAwait(false);
        if (programUser == null)
        {
            throw new InvalidOperationException(
                "An active course enrollment is required before Testing Lab evidence can complete Learning activity.");
        }

        if (notification.CohortId.HasValue)
        {
            var cohortEnrollmentExists = await context.Set<Enrollment>().AnyAsync(enrollment =>
                enrollment.CourseId == notification.CourseId &&
                enrollment.UserId == notification.UserId &&
                enrollment.CohortId == notification.CohortId &&
                enrollment.Status != GameGuild.Learning.Enrollments.EnrollmentStatus.Dropped &&
                enrollment.Status != GameGuild.Learning.Enrollments.EnrollmentStatus.Expired &&
                enrollment.DeletedAt == null &&
                (enrollment.TenantId == null || enrollment.TenantId == notification.TenantId),
                cancellationToken).ConfigureAwait(false);
            if (!cohortEnrollmentExists)
            {
                throw new InvalidOperationException(
                    "A matching active cohort enrollment is required for this Testing Lab evidence.");
            }
        }

        var interaction = await context.Set<ContentInteraction>().FirstOrDefaultAsync(candidate =>
            candidate.UserId == notification.UserId &&
            candidate.ContentId == notification.LearningActivityId &&
            candidate.SubmittedAt == null &&
            candidate.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
        if (interaction == null)
        {
            interaction = new ContentInteraction
            {
                UserId = notification.UserId,
                ContentId = notification.LearningActivityId,
                ProgramUserId = programUser.Id,
                TenantId = notification.TenantId
            };
            context.Set<ContentInteraction>().Add(interaction);
        }

        interaction.Complete();
        context.Set<TestingLabLearningEvidenceReceipt>()
            .Add(TestingLabLearningEvidenceReceipt.Consume(notification));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Consumed Testing Lab evidence {EvidenceId} for Learning activity {ActivityId} and user {UserId}",
            notification.EvidenceId,
            notification.LearningActivityId,
            notification.UserId);
    }
}
