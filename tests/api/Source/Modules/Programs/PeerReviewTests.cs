using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Programs;
using GameGuild.Modules.Programs.Services;
using GameGuild.Modules.Contents;g GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Programs;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.Tests.Modules.Programs;

/// <summary>
/// Comprehensive tests for Peer Review system functionality
/// </summary>
public class PeerReviewTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPeerReviewService _peerReviewService;

    public PeerReviewTests()
    {
        var services = new ServiceCollection();
        
        // Add database context
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add Peer Review service
        services.AddScoped<IPeerReviewService, PeerReviewService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _peerReviewService = _serviceProvider.GetRequiredService<IPeerReviewService>();
    }

    [Fact]
    public async Task PeerReview_Entity_Can_Be_Created_And_Saved()
    {
        // Arrange
        var reviewerId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        await SeedTestData(reviewerId, submissionId);

        var review = new PeerReview
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            ReviewerId = reviewerId,
            Status = PeerReviewStatus.Pending,
            AssignedDate = DateTime.UtcNow,
            RequiredReviewCount = 3,
            CurrentReviewCount = 0
        };

        // Act
        _context.PeerReviews.Add(review);
        await _context.SaveChangesAsync();

        // Assert
        var savedReview = await _context.PeerReviews.FindAsync(review.Id);
        Assert.NotNull(savedReview);
        Assert.Equal(submissionId, savedReview.SubmissionId);
        Assert.Equal(reviewerId, savedReview.ReviewerId);
        Assert.Equal(PeerReviewStatus.Pending, savedReview.Status);
        Assert.Equal(3, savedReview.RequiredReviewCount);
        Assert.Equal(0, savedReview.CurrentReviewCount);
    }

    [Fact]
    public async Task PeerReviewService_Can_Create_Review_Assignment()
    {
        // Arrange
        var reviewerId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        await SeedTestData(reviewerId, submissionId);

        // Act
        var review = await _peerReviewService.CreateReviewAssignmentAsync(
            submissionId, 
            reviewerId, 
            requiredReviews: 2
        );

        // Assert
        Assert.NotNull(review);
        Assert.Equal(submissionId, review.SubmissionId);
        Assert.Equal(reviewerId, review.ReviewerId);
        Assert.Equal(PeerReviewStatus.Pending, review.Status);
        Assert.Equal(2, review.RequiredReviewCount);
        Assert.True(review.AssignedDate <= DateTime.UtcNow);
    }

    [Fact]
    public async Task PeerReviewService_Can_Submit_Review()
    {
        // Arrange
        var reviewerId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        await SeedTestData(reviewerId, submissionId);

        var review = await _peerReviewService.CreateReviewAssignmentAsync(submissionId, reviewerId, 3);

        var reviewSubmission = new PeerReviewSubmission
        {
            Score = 85.5,
            Feedback = "Great work! The solution demonstrates a solid understanding of the concepts.",
            CriteriaScores = new Dictionary<string, double>
            {
                { "Technical Accuracy", 90.0 },
                { "Code Quality", 85.0 },
                { "Documentation", 80.0 }
            },
            QualityRating = ReviewQualityRating.High
        };

        // Act
        var submittedReview = await _peerReviewService.SubmitReviewAsync(review.Id, reviewSubmission);

        // Assert
        Assert.NotNull(submittedReview);
        Assert.Equal(PeerReviewStatus.Completed, submittedReview.Status);
        Assert.Equal(85.5, submittedReview.Score);
        Assert.Equal("Great work! The solution demonstrates a solid understanding of the concepts.", submittedReview.Feedback);
        Assert.True(submittedReview.SubmittedDate.HasValue);
        Assert.Equal(ReviewQualityRating.High, submittedReview.QualityRating);
    }

    [Fact]
    public async Task PeerReviewService_Can_Get_Reviews_For_Submission()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var reviewer1Id = Guid.NewGuid();
        var reviewer2Id = Guid.NewGuid();
        var reviewer3Id = Guid.NewGuid();

        await SeedTestData(reviewer1Id, submissionId);
        await SeedTestUser(reviewer2Id, "Reviewer 2");
        await SeedTestUser(reviewer3Id, "Reviewer 3");

        // Create multiple reviews
        var review1 = await _peerReviewService.CreateReviewAssignmentAsync(submissionId, reviewer1Id, 3);
        var review2 = await _peerReviewService.CreateReviewAssignmentAsync(submissionId, reviewer2Id, 3);
        var review3 = await _peerReviewService.CreateReviewAssignmentAsync(submissionId, reviewer3Id, 3);

        // Submit some reviews
        await _peerReviewService.SubmitReviewAsync(review1.Id, new PeerReviewSubmission
        {
            Score = 90.0,
            Feedback = "Excellent work",
            QualityRating = ReviewQualityRating.High
        });

        await _peerReviewService.SubmitReviewAsync(review2.Id, new PeerReviewSubmission
        {
            Score = 78.5,
            Feedback = "Good effort with room for improvement",
            QualityRating = ReviewQualityRating.Medium
        });

        // Act
        var reviews = await _peerReviewService.GetReviewsForSubmissionAsync(submissionId);

        // Assert
        Assert.NotNull(reviews);
        var reviewList = reviews.ToList();
        Assert.Equal(3, reviewList.Count);
        
        var completedReviews = reviewList.Where(r => r.Status == PeerReviewStatus.Completed).ToList();
        var pendingReviews = reviewList.Where(r => r.Status == PeerReviewStatus.Pending).ToList();
        
        Assert.Equal(2, completedReviews.Count);
        Assert.Single(pendingReviews);
    }

    [Fact]
    public async Task PeerReviewService_Can_Calculate_Consensus_Score()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        await SeedCompletedReviews(submissionId, new[] { 85.0, 92.0, 78.0 });

        // Act
        var consensusScore = await _peerReviewService.CalculateConsensusScoreAsync(submissionId);

        // Assert
        // Expected average: (85.0 + 92.0 + 78.0) / 3 = 85.0
        Assert.Equal(85.0, consensusScore, 1); // Allow 1 point tolerance for rounding
    }

    [Fact]
    public async Task PeerReviewService_Can_Detect_Review_Conflicts()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        // Create reviews with significant score differences (conflict threshold typically 15-20 points)
        await SeedCompletedReviews(submissionId, new[] { 95.0, 60.0, 45.0 });

        // Act
        var hasConflict = await _peerReviewService.HasReviewConflictAsync(submissionId, conflictThreshold: 20.0);

        // Assert
        Assert.True(hasConflict); // 95.0 - 45.0 = 50.0, which exceeds threshold of 20.0
    }

    [Fact]
    public async Task PeerReviewService_Can_Request_Additional_Review()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var additionalReviewerId = Guid.NewGuid();
        
        await SeedCompletedReviews(submissionId, new[] { 95.0, 45.0 }); // Conflicting scores
        await SeedTestUser(additionalReviewerId, "Additional Reviewer");

        // Act
        var additionalReview = await _peerReviewService.RequestAdditionalReviewAsync(
            submissionId, 
            additionalReviewerId, 
            "Conflicting peer review scores"
        );

        // Assert
        Assert.NotNull(additionalReview);
        Assert.Equal(submissionId, additionalReview.SubmissionId);
        Assert.Equal(additionalReviewerId, additionalReview.ReviewerId);
        Assert.Equal(PeerReviewStatus.Pending, additionalReview.Status);
        Assert.Equal("Conflicting peer review scores", additionalReview.AdditionalReviewReason);
    }

    [Fact]
    public async Task PeerReviewService_Can_Get_Reviewer_Assignment_History()
    {
        // Arrange
        var reviewerId = Guid.NewGuid();
        await SeedTestUser(reviewerId, "Active Reviewer");

        // Create multiple review assignments
        for (int i = 0; i < 5; i++)
        {
            var submissionId = Guid.NewGuid();
            await SeedTestSubmission(submissionId, $"Submission {i + 1}");
            await _peerReviewService.CreateReviewAssignmentAsync(submissionId, reviewerId, 3);
        }

        // Act
        var reviewHistory = await _peerReviewService.GetReviewerHistoryAsync(reviewerId);

        // Assert
        Assert.NotNull(reviewHistory);
        var historyList = reviewHistory.ToList();
        Assert.Equal(5, historyList.Count);
        Assert.All(historyList, r => Assert.Equal(reviewerId, r.ReviewerId));
    }

    [Fact]
    public async Task PeerReviewService_Can_Calculate_Reviewer_Statistics()
    {
        // Arrange
        var reviewerId = Guid.NewGuid();
        await SeedTestUser(reviewerId, "Productive Reviewer");

        // Create and complete multiple reviews
        for (int i = 0; i < 10; i++)
        {
            var submissionId = Guid.NewGuid();
            await SeedTestSubmission(submissionId, $"Submission {i + 1}");
            var review = await _peerReviewService.CreateReviewAssignmentAsync(submissionId, reviewerId, 3);
            
            // Complete some reviews (7 out of 10)
            if (i < 7)
            {
                await _peerReviewService.SubmitReviewAsync(review.Id, new PeerReviewSubmission
                {
                    Score = 80.0 + i, // Varying scores
                    Feedback = $"Review feedback {i + 1}",
                    QualityRating = ReviewQualityRating.Medium
                });
            }
        }

        // Act
        var stats = await _peerReviewService.GetReviewerStatisticsAsync(reviewerId);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(10, stats.TotalAssigned);
        Assert.Equal(7, stats.TotalCompleted);
        Assert.Equal(3, stats.TotalPending);
        Assert.Equal(70.0, stats.CompletionRate, 1); // 7/10 * 100 = 70%
        Assert.True(stats.AverageScore > 80.0); // Should be around 83.0
    }

    [Fact]
    public async Task PeerReviewService_Can_Escalate_Review_For_Moderation()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        
        await SeedTestData(reviewerId, submissionId);
        
        var review = new PeerReview
        {
            Id = reviewId,
            SubmissionId = submissionId,
            ReviewerId = reviewerId,
            Status = PeerReviewStatus.Completed,
            Score = 45.0,
            Feedback = "This work is unacceptable",
            QualityRating = ReviewQualityRating.Low,
            AssignedDate = DateTime.UtcNow.AddDays(-2),
            SubmittedDate = DateTime.UtcNow.AddDays(-1)
        };

        _context.PeerReviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var escalatedReview = await _peerReviewService.EscalateForModerationAsync(
            reviewId, 
            "Inappropriately harsh feedback"
        );

        // Assert
        Assert.NotNull(escalatedReview);
        Assert.Equal(PeerReviewStatus.UnderModeration, escalatedReview.Status);
        Assert.Equal("Inappropriately harsh feedback", escalatedReview.ModerationReason);
        Assert.True(escalatedReview.EscalatedDate.HasValue);
    }

    [Fact]
    public async Task PeerReviewService_Can_Accept_Moderated_Review()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        
        await SeedModeratedReview(reviewId, moderatorId);

        // Act
        var acceptedReview = await _peerReviewService.AcceptModeratedReviewAsync(
            reviewId, 
            moderatorId, 
            "Review is appropriate after discussion"
        );

        // Assert
        Assert.NotNull(acceptedReview);
        Assert.Equal(PeerReviewStatus.Completed, acceptedReview.Status);
        Assert.Equal(moderatorId, acceptedReview.ModeratedBy);
        Assert.Equal("Review is appropriate after discussion", acceptedReview.ModerationNotes);
        Assert.True(acceptedReview.ModeratedDate.HasValue);
    }

    [Fact]
    public async Task PeerReviewService_Can_Reject_Moderated_Review()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        
        await SeedModeratedReview(reviewId, moderatorId);

        // Act
        var rejectedReview = await _peerReviewService.RejectModeratedReviewAsync(
            reviewId, 
            moderatorId, 
            "Review violates community guidelines"
        );

        // Assert
        Assert.NotNull(rejectedReview);
        Assert.Equal(PeerReviewStatus.Rejected, rejectedReview.Status);
        Assert.Equal(moderatorId, rejectedReview.ModeratedBy);
        Assert.Equal("Review violates community guidelines", rejectedReview.ModerationNotes);
        Assert.True(rejectedReview.ModeratedDate.HasValue);
    }

    #region Helper Methods

    private async Task SeedTestData(Guid reviewerId, Guid submissionId)
    {
        await SeedTestUser(reviewerId, "Test Reviewer");
        await SeedTestSubmission(submissionId, "Test Submission");
    }

    private async Task SeedTestUser(Guid userId, string name)
    {
        var user = new User
        {
            Id = userId,
            Name = name,
            Email = $"test_{userId:N}@example.com",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    private async Task SeedTestSubmission(Guid submissionId, string title)
    {
        // Note: This would typically be an Assignment Submission entity
        // For now, we'll use a placeholder approach
        var submission = new ActivityGrade
        {
            Id = submissionId,
            UserId = Guid.NewGuid(),
            ActivityId = Guid.NewGuid(),
            Grade = 0, // Will be updated by peer review
            GradedDate = DateTime.UtcNow,
            GradingMethod = GradingMethod.Peer
        };

        _context.ActivityGrades.Add(submission);
        await _context.SaveChangesAsync();
    }

    private async Task SeedCompletedReviews(Guid submissionId, double[] scores)
    {
        await SeedTestSubmission(submissionId, "Test Submission");

        for (int i = 0; i < scores.Length; i++)
        {
            var reviewerId = Guid.NewGuid();
            await SeedTestUser(reviewerId, $"Reviewer {i + 1}");

            var review = new PeerReview
            {
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                ReviewerId = reviewerId,
                Status = PeerReviewStatus.Completed,
                Score = scores[i],
                Feedback = $"Review feedback for score {scores[i]}",
                QualityRating = ReviewQualityRating.Medium,
                AssignedDate = DateTime.UtcNow.AddDays(-2),
                SubmittedDate = DateTime.UtcNow.AddDays(-1),
                RequiredReviewCount = scores.Length,
                CurrentReviewCount = i + 1
            };

            _context.PeerReviews.Add(review);
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedModeratedReview(Guid reviewId, Guid moderatorId)
    {
        var reviewerId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        
        await SeedTestData(reviewerId, submissionId);
        await SeedTestUser(moderatorId, "Moderator");

        var review = new PeerReview
        {
            Id = reviewId,
            SubmissionId = submissionId,
            ReviewerId = reviewerId,
            Status = PeerReviewStatus.UnderModeration,
            Score = 30.0,
            Feedback = "Questionable feedback requiring moderation",
            QualityRating = ReviewQualityRating.Low,
            AssignedDate = DateTime.UtcNow.AddDays(-3),
            SubmittedDate = DateTime.UtcNow.AddDays(-2),
            EscalatedDate = DateTime.UtcNow.AddDays(-1),
            ModerationReason = "Inappropriate language"
        };

        _context.PeerReviews.Add(review);
        await _context.SaveChangesAsync();
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}
