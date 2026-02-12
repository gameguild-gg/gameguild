namespace GameGuild.Assets.UnitTests;

public class AssetReferenceTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var reference = new AssetReference(
            contentId,
            userId,
            "test-image.jpg",
            AssetAccessPolicy.Public,
            "Course",
            Guid.NewGuid());

        // Assert
        reference.AssetContentId.Should().Be(contentId);
        reference.CreatedByUserId.Should().Be(userId);
        reference.DisplayName.Should().Be("test-image.jpg");
        reference.AccessPolicy.Should().Be(AssetAccessPolicy.Public);
        reference.ParentResourceType.Should().Be("Course");
        reference.ParentResourceId.Should().NotBeNull();
        reference.AccessCount.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithMinimalParameters_ShouldCreateInstance()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var reference = new AssetReference(
            contentId,
            userId,
            "document.pdf",
            AssetAccessPolicy.Private,
            null,
            null);

        // Assert
        reference.ParentResourceType.Should().BeNull();
        reference.ParentResourceId.Should().BeNull();
    }

    [Fact]
    public void UpdateDisplayName_ShouldUpdateName()
    {
        // Arrange
        var reference = CreateTestReference();

        // Act
        reference.UpdateDisplayName("new-name.jpg");

        // Assert
        reference.DisplayName.Should().Be("new-name.jpg");
    }

    [Fact]
    public void UpdateAccessPolicy_ShouldUpdatePolicy()
    {
        // Arrange
        var reference = CreateTestReference();

        // Act
        reference.UpdateAccessPolicy(AssetAccessPolicy.OwnerOnly);

        // Assert
        reference.AccessPolicy.Should().Be(AssetAccessPolicy.OwnerOnly);
    }

    [Theory]
    [InlineData(AssetAccessPolicy.Public)]
    [InlineData(AssetAccessPolicy.Private)]
    [InlineData(AssetAccessPolicy.Unlisted)]
    [InlineData(AssetAccessPolicy.Authenticated)]
    [InlineData(AssetAccessPolicy.OwnerOnly)]
    [InlineData(AssetAccessPolicy.Inherited)]
    public void Constructor_AllAccessPolicies_ShouldWork(AssetAccessPolicy policy)
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var reference = new AssetReference(
            contentId,
            userId,
            "test.jpg",
            policy,
            null,
            null);

        // Assert
        reference.AccessPolicy.Should().Be(policy);
    }

    [Fact]
    public void SoftDelete_ShouldMarkAsDeleted()
    {
        // Arrange
        var reference = CreateTestReference();
        // Simulate persisted entity (Version must be > 0 for SoftDelete)
        typeof(EntityBase<Guid>).GetProperty(nameof(EntityBase.Version))!
            .SetValue(reference, 1);

        // Act
        reference.SoftDelete();

        // Assert
        reference.IsDeleted.Should().BeTrue();
        reference.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordAccess_ShouldIncrementCount()
    {
        // Arrange
        var reference = CreateTestReference();
        reference.AccessCount.Should().Be(0);

        // Act
        reference.RecordAccess();

        // Assert
        reference.AccessCount.Should().Be(1);
        reference.LastAccessedAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordAccess_MultipleTimes_ShouldTrackCount()
    {
        // Arrange
        var reference = CreateTestReference();

        // Act
        reference.RecordAccess();
        reference.RecordAccess();
        reference.RecordAccess();

        // Assert
        reference.AccessCount.Should().Be(3);
    }

    private static AssetReference CreateTestReference()
    {
        return new AssetReference(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test-file.jpg",
            AssetAccessPolicy.Private,
            null,
            null);
    }
}
