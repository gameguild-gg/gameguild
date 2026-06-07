namespace GameGuild.Projects.UnitTests.Entities;

public class ProjectMetadataTests
{
    [Fact]
    public void ProjectMetadata_Creation_Should_Set_Default_Values()
    {
        var metadata = new ProjectMetadata();

        metadata.ViewCount.Should().Be(0);
        metadata.DownloadCount.Should().Be(0);
        metadata.FollowerCount.Should().Be(0);
        metadata.Id.Should().BeEmpty();
        metadata.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        metadata.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(100, 50, 10)]
    [InlineData(0, 0, 0)]
    [InlineData(1000000, 500000, 100000)]
    public void ProjectMetadata_Should_Accept_Valid_Counts(int viewCount, int downloadCount, int followerCount)
    {
        var metadata = new ProjectMetadata
        {
            ProjectId = Guid.NewGuid(),
            ViewCount = viewCount,
            DownloadCount = downloadCount,
            FollowerCount = followerCount
        };

        metadata.ViewCount.Should().Be(viewCount);
        metadata.DownloadCount.Should().Be(downloadCount);
        metadata.FollowerCount.Should().Be(followerCount);
        metadata.ProjectId.Should().NotBeEmpty();
    }

    [Fact]
    public void ProjectMetadata_Should_Have_Required_Project_Relationship()
    {
        var projectId = Guid.NewGuid();

        var metadata = new ProjectMetadata
        {
            ProjectId = projectId,
            ViewCount = 12
        };

        metadata.ProjectId.Should().Be(projectId);
    }

    [Fact]
    public void ProjectMetadata_Should_Track_Audit_Information()
    {
        var now = DateTime.UtcNow;

        var metadata = new ProjectMetadata
        {
            ProjectId = Guid.NewGuid(),
            ViewCount = 5,
            CreatedAt = now,
            UpdatedAt = now
        };

        metadata.CreatedAt.Should().Be(now);
        metadata.UpdatedAt.Should().Be(now);
        metadata.Id.Should().BeEmpty();
    }
}
