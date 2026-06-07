using GameGuild.Projects.UnitTests.Infrastructure;

namespace GameGuild.Projects.UnitTests.Entities;

public class ProjectCategoryTests
{
    private readonly TestDataBuilder _testDataBuilder;

    public ProjectCategoryTests()
    {
        _testDataBuilder = new TestDataBuilder();
    }

    [Fact]
    public void ProjectCategory_Creation_Should_Set_Default_Values()
    {
        // Arrange & Act
        var category = new ProjectCategory();

        // Assert
        category.Name.Should().BeEmpty();
        category.Id.Should().BeEmpty();
        category.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        category.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("Games")]
    [InlineData("Tools & Utilities")]
    [InlineData("Art & Design")]
    [InlineData("Music & Audio")]
    public void ProjectCategory_Should_Accept_Valid_Names(string name)
    {
        // Arrange & Act
        var category = _testDataBuilder.CreateProjectCategory(name);

        // Assert
        category.Name.Should().Be(name);
        category.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void ProjectCategory_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var category = _testDataBuilder.CreateProjectCategory("Test Category");

        // Assert
        category.Id.Should().NotBeEmpty();
        category.Name.Should().NotBeNullOrEmpty();
        category.CreatedAt.Should().BeAfter(DateTime.MinValue);
        category.UpdatedAt.Should().BeAfter(DateTime.MinValue);
    }

    [Fact]
    public void ProjectCategory_Should_Track_Audit_Information()
    {
        // Arrange
        var now = DateTime.UtcNow;
        
        // Act
        var category = new ProjectCategory
        {
            Name = "Test Category",
            CreatedAt = now,
            UpdatedAt = now
        };

        // Assert
        category.CreatedAt.Should().Be(now);
        category.UpdatedAt.Should().Be(now);
    }
}
