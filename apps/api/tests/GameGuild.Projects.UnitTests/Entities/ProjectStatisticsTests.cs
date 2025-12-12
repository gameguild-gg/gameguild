namespace GameGuild.Projects.UnitTests.Entities;

public class ProjectStatisticsTests
{
    [Fact]
    public void ProjectStatistics_Creation_Should_Set_Default_Values()
    {
        // Arrange & Act
        var stats = new ProjectStatistics();

        // Assert
        stats.ViewCount.Should().Be(0);
        stats.DownloadCount.Should().Be(0);
        stats.LikeCount.Should().Be(0);
        stats.FollowerCount.Should().Be(0);
        stats.Id.Should().NotBeEmpty();
        stats.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        stats.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(100, 50, 25, 10)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1000000, 500000, 250000, 100000)]
    public void ProjectStatistics_Should_Accept_Valid_Counts(
        int viewCount, 
        int downloadCount, 
        int likeCount, 
        int followerCount)
    {
        // Arrange & Act
        var stats = new ProjectStatistics
        {
            ProjectId = Guid.NewGuid(),
            ViewCount = viewCount,
            DownloadCount = downloadCount,
            LikeCount = likeCount,
            FollowerCount = followerCount
        };

        // Assert
        stats.ViewCount.Should().Be(viewCount);
        stats.DownloadCount.Should().Be(downloadCount);
        stats.LikeCount.Should().Be(likeCount);
        stats.FollowerCount.Should().Be(followerCount);
    }

    [Fact]
    public void ProjectStatistics_Should_Have_Required_Project_Relationship()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        
        // Act
        var stats = new ProjectStatistics
        {
            ProjectId = projectId,
            ViewCount = 100
        };

        // Assert
        stats.ProjectId.Should().Be(projectId);
    }

    [Fact]
    public void ProjectStatistics_Should_Support_Incrementing_Counts()
    {
        // Arrange
        var stats = new ProjectStatistics
        {
            ProjectId = Guid.NewGuid(),
            ViewCount = 10,
            LikeCount = 5
        };

        // Act
        stats.ViewCount += 1;
        stats.LikeCount += 1;

        // Assert
        stats.ViewCount.Should().Be(11);
        stats.LikeCount.Should().Be(6);
    }

    [Fact]
    public void ProjectStatistics_Should_Track_Timestamps()
    {
        // Arrange
        var lastViewedAt = DateTime.UtcNow.AddHours(-1);
        var lastDownloadedAt = DateTime.UtcNow.AddMinutes(-30);
        
        // Act
        var stats = new ProjectStatistics
        {
            ProjectId = Guid.NewGuid(),
            LastViewedAt = lastViewedAt,
            LastDownloadedAt = lastDownloadedAt
        };

        // Assert
        stats.LastViewedAt.Should().Be(lastViewedAt);
        stats.LastDownloadedAt.Should().Be(lastDownloadedAt);
    }

    [Fact]
    public void ProjectStatistics_Should_Allow_Null_Timestamps()
    {
        // Arrange & Act
        var stats = new ProjectStatistics
        {
            ProjectId = Guid.NewGuid(),
            ViewCount = 0
        };

        // Assert
        stats.LastViewedAt.Should().BeNull();
        stats.LastDownloadedAt.Should().BeNull();
    }

    [Fact]
    public void ProjectStatistics_Should_Track_Audit_Information()
    {
        // Arrange
        var now = DateTime.UtcNow;
        
        // Act
        var stats = new ProjectStatistics
        {
            ProjectId = Guid.NewGuid(),
            ViewCount = 100,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Assert
        stats.CreatedAt.Should().Be(now);
        stats.UpdatedAt.Should().Be(now);
        stats.Id.Should().NotBeEmpty();
    }
}