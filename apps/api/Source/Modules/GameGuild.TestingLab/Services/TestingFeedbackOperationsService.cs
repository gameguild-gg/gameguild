namespace GameGuild.TestingLab;

/// <summary>
/// Service implementation for feedback, statistics, and reporting operations.
/// Extracted from the monolithic TestService for focused responsibility.
/// </summary>
public class TestingFeedbackOperationsService(IApplicationDbContext context) : ITestingFeedbackOperations
{
    #region Feedback CRUD

    public async Task<TestingFeedback> AddFeedbackAsync(Guid testingRequestId, Guid userId, Guid feedbackFormId, string feedbackData, TestingContext context1, Guid? sessionId = null, string? additionalNotes = null)
    {
        var feedback = new TestingFeedback
        {
            Id = Guid.NewGuid(),
            TestingRequestId = testingRequestId,
            UserId = userId,
            FeedbackFormId = feedbackFormId,
            SessionId = sessionId,
            TestingContext = context1,
            FeedbackData = feedbackData,
            AdditionalNotes = additionalNotes,
        };

        context.Set<TestingFeedback>().Add(feedback);
        await context.SaveChangesAsync().ConfigureAwait(false);

        return await context.Set<TestingFeedback>()
            .Include(tf => tf.TestingRequest)
            .Include(tf => tf.User)
            .Include(tf => tf.FeedbackForm)
            .Include(tf => tf.Session)
            .FirstAsync(tf => tf.Id == feedback.Id);
    }

    public async Task<IEnumerable<TestingFeedback>> GetTestingRequestFeedbackAsync(Guid testingRequestId)
    {
        return await context.Set<TestingFeedback>()
            .Where(tf => tf.TestingRequestId == testingRequestId)
            .Include(tf => tf.User)
            .Include(tf => tf.FeedbackForm)
            .Include(tf => tf.Session)
            .OrderByDescending(tf => tf.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestingFeedback>> GetFeedbackByUserAsync(Guid userId)
    {
        return await context.Set<TestingFeedback>()
            .Where(tf => tf.UserId == userId)
            .Include(tf => tf.TestingRequest)
            .Include(tf => tf.FeedbackForm)
            .Include(tf => tf.Session)
            .OrderByDescending(tf => tf.CreatedAt)
            .ToListAsync();
    }

    #endregion

    #region Simplified Feedback

    public async Task SubmitFeedbackAsync(SubmitFeedbackDto feedbackDto, Guid userId)
    {
        var existingForm = await context.Set<TestingFeedbackForm>()
            .FirstOrDefaultAsync(f => f.TestingRequestId == feedbackDto.TestingRequestId);

        Guid feedbackFormId;

        if (existingForm == null)
        {
            var feedbackForm = new TestingFeedbackForm
            {
                Id = Guid.NewGuid(),
                TestingRequestId = feedbackDto.TestingRequestId,
                FormSchema = "{ \"type\": \"simple\", \"questions\": [] }",
                IsForOnline = true,
                IsForSessions = true,
            };

            context.Set<TestingFeedbackForm>().Add(feedbackForm);
            await context.SaveChangesAsync().ConfigureAwait(false);
            feedbackFormId = feedbackForm.Id;
        }
        else
        {
            feedbackFormId = existingForm.Id;
        }

        var feedback = new TestingFeedback
        {
            Id = Guid.NewGuid(),
            TestingRequestId = feedbackDto.TestingRequestId,
            FeedbackFormId = feedbackFormId,
            UserId = userId,
            SessionId = feedbackDto.SessionId,
            TestingContext = TestingContext.Online,
            FeedbackData = feedbackDto.FeedbackResponses,
            OverallRating = feedbackDto.OverallRating,
            WouldRecommend = feedbackDto.WouldRecommend,
            AdditionalNotes = feedbackDto.AdditionalNotes,
        };

        context.Set<TestingFeedback>().Add(feedback);

        var testingRequest = await context.Set<TestingRequest>().FindAsync(feedbackDto.TestingRequestId).ConfigureAwait(false);

        if (testingRequest != null)
        {
            testingRequest.CurrentTesterCount = await context.Set<TestingFeedback>().CountAsync(f => f.TestingRequestId == feedbackDto.TestingRequestId);
            testingRequest.Touch();
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    #endregion

    #region Statistics

    public async Task<object> GetTestingRequestStatisticsAsync(Guid testingRequestId)
    {
        var participantCount = await context.Set<TestingParticipant>().CountAsync(tp => tp.TestingRequestId == testingRequestId);
        var sessionCount = await context.Set<TestingSession>().CountAsync(ts => ts.TestingRequestId == testingRequestId && ts.DeletedAt == null);
        var feedbackCount = await context.Set<TestingFeedback>().CountAsync(tf => tf.TestingRequestId == testingRequestId);
        var completedSessionCount = await context.Set<TestingSession>().CountAsync(ts => ts.TestingRequestId == testingRequestId && ts.Status == SessionStatus.Completed && ts.DeletedAt == null);

        return new
        {
            ParticipantCount = participantCount,
            SessionCount = sessionCount,
            CompletedSessionCount = completedSessionCount,
            FeedbackCount = feedbackCount
        };
    }

    #endregion

    #region Feedback Reporting & Quality

    public async Task ReportFeedbackAsync(Guid feedbackId, string reason, Guid reportedByUserId)
    {
        var feedback = await context.Set<TestingFeedback>().FirstOrDefaultAsync(tf => tf.Id == feedbackId);

        if (feedback == null) { throw new ArgumentException("Feedback not found"); }

        feedback.IsReported = true;
        feedback.ReportReason = reason;
        feedback.ReportedByUserId = reportedByUserId;
        feedback.ReportedAt = SystemClock.UtcNow;

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task RateFeedbackQualityAsync(Guid feedbackId, FeedbackQuality quality, Guid ratedByUserId)
    {
        var feedback = await context.Set<TestingFeedback>().FirstOrDefaultAsync(tf => tf.Id == feedbackId);

        if (feedback == null) { throw new ArgumentException("Feedback not found"); }

        feedback.QualityRating = quality;

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    #endregion
}
