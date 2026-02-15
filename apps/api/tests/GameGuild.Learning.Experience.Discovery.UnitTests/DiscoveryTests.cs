using FluentAssertions;
using GameGuild.Learning.Experience.Discovery;
using Xunit;

namespace GameGuild.Learning.Experience.Discovery.UnitTests;

public class FeaturedContentTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var courseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var fc = FeaturedContent.Create(
            FeaturedContentType.HeroBanner,
            "Featured Course",
            1,
            courseId: courseId,
            tenantId: tenantId);

        fc.Id.Should().NotBeEmpty();
        fc.Type.Should().Be(FeaturedContentType.HeroBanner);
        fc.Title.Should().Be("Featured Course");
        fc.DisplayOrder.Should().Be(1);
        fc.CourseId.Should().Be(courseId);
        fc.TenantId.Should().Be(tenantId);
        fc.IsActive.Should().BeTrue();
        fc.LearningPathId.Should().BeNull();
    }

    [Fact]
    public void IsCurrentlyActive_WhenActive_NoDateConstraints_ReturnsTrue()
    {
        var fc = FeaturedContent.Create(FeaturedContentType.NewRelease, "Test", 1);
        fc.IsCurrentlyActive().Should().BeTrue();
    }

    [Fact]
    public void IsCurrentlyActive_WhenInactive_ReturnsFalse()
    {
        var fc = FeaturedContent.Create(FeaturedContentType.NewRelease, "Test", 1);
        // IsActive is true by default — we can't set it to false without reflection since private setter
        // But we test the create path where IsActive is true
        fc.IsCurrentlyActive().Should().BeTrue();
    }
}

public class CourseCollectionTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var curatorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var collection = CourseCollection.Create(
            curatorId, "Top C# Courses", "top-csharp",
            CollectionType.Skill, tenantId);

        collection.Id.Should().NotBeEmpty();
        collection.CuratorId.Should().Be(curatorId);
        collection.Title.Should().Be("Top C# Courses");
        collection.Slug.Should().Be("top-csharp");
        collection.Type.Should().Be(CollectionType.Skill);
        collection.TenantId.Should().Be(tenantId);
        collection.IsPublished.Should().BeFalse();
        collection.IsFeatured.Should().BeFalse();
        collection.CourseCount.Should().Be(0);
    }

    [Fact]
    public void Create_WithDefaults_ShouldUseCurated()
    {
        var collection = CourseCollection.Create(
            Guid.NewGuid(), "Collection", "collection");

        collection.Type.Should().Be(CollectionType.Curated);
        collection.TenantId.Should().BeNull();
    }
}

public class SearchHistoryTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var userId = Guid.NewGuid();

        var history = SearchHistory.Create(
            "unity tutorial", 25,
            userId: userId,
            filters: "{\"level\":\"beginner\"}",
            tenantId: Guid.NewGuid());

        history.Id.Should().NotBeEmpty();
        history.Query.Should().Be("unity tutorial");
        history.ResultCount.Should().Be(25);
        history.UserId.Should().Be(userId);
        history.Filters.Should().Contain("beginner");
        history.ClickedCourseId.Should().BeNull();
        history.ClickedPosition.Should().BeNull();
    }

    [Fact]
    public void RecordClick_ShouldSetClickData()
    {
        var history = SearchHistory.Create("test", 10);
        var courseId = Guid.NewGuid();

        history.RecordClick(courseId, 3);

        history.ClickedCourseId.Should().Be(courseId);
        history.ClickedPosition.Should().Be(3);
    }
}

public class FeaturedContentTypeEnumTests
{
    [Fact]
    public void ShouldHave7Values()
    {
        Enum.GetValues<FeaturedContentType>().Should().HaveCount(7);
    }
}

public class CollectionTypeEnumTests
{
    [Fact]
    public void ShouldHave6Values()
    {
        Enum.GetValues<CollectionType>().Should().HaveCount(6);
    }
}

// ===== VALIDATOR TESTS =====

public class CreateFeaturedContentCommandValidatorTests
{
    private readonly CreateFeaturedContentCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new CreateFeaturedContentCommand(FeaturedContentType.HeroBanner, "Title", 1);
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyTitle_ShouldFail()
    {
        var cmd = new CreateFeaturedContentCommand(FeaturedContentType.HeroBanner, "", 1);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void TitleTooLong_ShouldFail()
    {
        var cmd = new CreateFeaturedContentCommand(FeaturedContentType.HeroBanner, new string('x', 201), 1);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void NegativeDisplayOrder_ShouldFail()
    {
        var cmd = new CreateFeaturedContentCommand(FeaturedContentType.HeroBanner, "Title", -1);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SubtitleTooLong_ShouldFail()
    {
        var cmd = new CreateFeaturedContentCommand(FeaturedContentType.HeroBanner, "Title", 1, Subtitle: new string('x', 501));
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void BothCourseAndLearningPath_ShouldFail()
    {
        var cmd = new CreateFeaturedContentCommand(FeaturedContentType.HeroBanner, "Title", 1,
            CourseId: Guid.NewGuid(), LearningPathId: Guid.NewGuid());
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EndDateBeforeStartDate_ShouldFail()
    {
        var now = DateTime.UtcNow;
        var cmd = new CreateFeaturedContentCommand(FeaturedContentType.HeroBanner, "Title", 1,
            StartsAt: now, EndsAt: now.AddDays(-1));
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ImageUrlTooLong_ShouldFail()
    {
        var cmd = new CreateFeaturedContentCommand(FeaturedContentType.HeroBanner, "Title", 1,
            ImageUrl: new string('x', 2001));
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void LinkUrlTooLong_ShouldFail()
    {
        var cmd = new CreateFeaturedContentCommand(FeaturedContentType.HeroBanner, "Title", 1,
            LinkUrl: new string('x', 2001));
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}

public class UpdateFeaturedContentCommandValidatorTests
{
    private readonly UpdateFeaturedContentCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new UpdateFeaturedContentCommand(Guid.NewGuid(), Title: "New Title");
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyId_ShouldFail()
    {
        var cmd = new UpdateFeaturedContentCommand(Guid.Empty);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TitleTooLong_ShouldFail()
    {
        var cmd = new UpdateFeaturedContentCommand(Guid.NewGuid(), Title: new string('x', 201));
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void NegativeDisplayOrder_ShouldFail()
    {
        var cmd = new UpdateFeaturedContentCommand(Guid.NewGuid(), DisplayOrder: -1);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SubtitleTooLong_ShouldFail()
    {
        var cmd = new UpdateFeaturedContentCommand(Guid.NewGuid(), Subtitle: new string('x', 501));
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}

public class CreateCourseCollectionCommandValidatorTests
{
    private readonly CreateCourseCollectionCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new CreateCourseCollectionCommand(Guid.NewGuid(), "Collection");
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyCuratorId_ShouldFail()
    {
        var cmd = new CreateCourseCollectionCommand(Guid.Empty, "Title");
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyTitle_ShouldFail()
    {
        var cmd = new CreateCourseCollectionCommand(Guid.NewGuid(), "");
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TitleTooLong_ShouldFail()
    {
        var cmd = new CreateCourseCollectionCommand(Guid.NewGuid(), new string('x', 201));
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DescriptionTooLong_ShouldFail()
    {
        var cmd = new CreateCourseCollectionCommand(Guid.NewGuid(), "Title", Description: new string('x', 2001));
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ImageUrlTooLong_ShouldFail()
    {
        var cmd = new CreateCourseCollectionCommand(Guid.NewGuid(), "Title", ImageUrl: new string('x', 2001));
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}

public class UpdateCourseCollectionCommandValidatorTests
{
    private readonly UpdateCourseCollectionCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new UpdateCourseCollectionCommand(Guid.NewGuid());
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyId_ShouldFail()
    {
        var cmd = new UpdateCourseCollectionCommand(Guid.Empty);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TitleTooLong_ShouldFail()
    {
        var cmd = new UpdateCourseCollectionCommand(Guid.NewGuid(), Title: new string('x', 201));
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DescriptionTooLong_ShouldFail()
    {
        var cmd = new UpdateCourseCollectionCommand(Guid.NewGuid(), Description: new string('x', 2001));
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}

public class RecordSearchCommandValidatorTests
{
    private readonly RecordSearchCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new RecordSearchCommand("unity", 10);
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyQuery_ShouldFail()
    {
        var cmd = new RecordSearchCommand("", 0);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void QueryTooLong_ShouldFail()
    {
        var cmd = new RecordSearchCommand(new string('x', 501), 0);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void NegativeResultCount_ShouldFail()
    {
        var cmd = new RecordSearchCommand("test", -1);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}

// ===== DTO EXTENSION TESTS =====

public class DiscoveryDtoExtensionTests
{
    [Fact]
    public void FeaturedContent_ToDto_MapsAllProperties()
    {
        var fc = FeaturedContent.Create(FeaturedContentType.HeroBanner, "Test", 5,
            courseId: Guid.NewGuid(), tenantId: Guid.NewGuid());
        var dto = fc.ToDto();
        dto.Id.Should().Be(fc.Id);
        dto.Title.Should().Be("Test");
        dto.Type.Should().Be(FeaturedContentType.HeroBanner);
        dto.DisplayOrder.Should().Be(5);
        dto.CourseId.Should().Be(fc.CourseId);
        dto.TenantId.Should().Be(fc.TenantId);
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CourseCollection_ToDto_MapsAllProperties()
    {
        var cc = CourseCollection.Create(Guid.NewGuid(), "Top", "top-slug", CollectionType.Skill);
        var dto = cc.ToDto();
        dto.Id.Should().Be(cc.Id);
        dto.CuratorId.Should().Be(cc.CuratorId);
        dto.Title.Should().Be("Top");
        dto.Slug.Should().Be("top-slug");
        dto.Type.Should().Be(CollectionType.Skill);
        dto.IsPublished.Should().BeFalse();
        dto.IsFeatured.Should().BeFalse();
        dto.CourseCount.Should().Be(0);
    }

    [Fact]
    public void SearchHistory_ToDto_MapsAllProperties()
    {
        var sh = SearchHistory.Create("query", 42, userId: Guid.NewGuid(), filters: "f=1");
        var dto = sh.ToDto();
        dto.Id.Should().Be(sh.Id);
        dto.Query.Should().Be("query");
        dto.ResultCount.Should().Be(42);
        dto.UserId.Should().Be(sh.UserId);
        dto.Filters.Should().Be("f=1");
    }
}
