namespace GameGuild.Projects.UnitTests.Entities;

public class ProjectStatisticsTests
{
    [Fact]
    public void ProjectStatistics_Creation_Should_Set_Default_Values()
    {
        var stats = new ProjectStatistics();

        stats.ProjectId.Should().BeEmpty();
        stats.FollowerCount.Should().Be(0);
        stats.FeedbackCount.Should().Be(0);
        stats.AverageRating.Should().BeNull();
        stats.TotalDownloads.Should().Be(0);
        stats.ActiveTeamCount.Should().Be(0);
        stats.CollaboratorCount.Should().Be(0);
        stats.ReleaseCount.Should().Be(0);
        stats.JamSubmissionCount.Should().Be(0);
        stats.AwardCount.Should().Be(0);
        stats.ViewsLast30Days.Should().Be(0);
        stats.DownloadsLast30Days.Should().Be(0);
        stats.NewFollowersLast30Days.Should().Be(0);
        stats.CalculatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        stats.TrendingScore.Should().Be(0);
        stats.PopularityRank.Should().BeNull();
    }

    [Fact]
    public void ProjectStatistics_Should_Accept_Current_Analytics_Fields()
    {
        var projectId = Guid.NewGuid();
        var calculatedAt = DateTime.UtcNow.AddMinutes(-5);

        var stats = new ProjectStatistics
        {
            ProjectId = projectId,
            FollowerCount = 10,
            FeedbackCount = 4,
            AverageRating = 4.5m,
            TotalDownloads = 50,
            ActiveTeamCount = 2,
            CollaboratorCount = 3,
            ReleaseCount = 1,
            JamSubmissionCount = 5,
            AwardCount = 1,
            ViewsLast30Days = 100,
            DownloadsLast30Days = 25,
            NewFollowersLast30Days = 8,
            CalculatedAt = calculatedAt,
            TrendingScore = 9.75m,
            PopularityRank = 12
        };

        stats.ProjectId.Should().Be(projectId);
        stats.FollowerCount.Should().Be(10);
        stats.FeedbackCount.Should().Be(4);
        stats.AverageRating.Should().Be(4.5m);
        stats.TotalDownloads.Should().Be(50);
        stats.ActiveTeamCount.Should().Be(2);
        stats.CollaboratorCount.Should().Be(3);
        stats.ReleaseCount.Should().Be(1);
        stats.JamSubmissionCount.Should().Be(5);
        stats.AwardCount.Should().Be(1);
        stats.ViewsLast30Days.Should().Be(100);
        stats.DownloadsLast30Days.Should().Be(25);
        stats.NewFollowersLast30Days.Should().Be(8);
        stats.CalculatedAt.Should().Be(calculatedAt);
        stats.TrendingScore.Should().Be(9.75m);
        stats.PopularityRank.Should().Be(12);
    }
}
