using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.API.UnitTests.Database;

using CourseProgram = GameGuild.Learning.Courses.Program;

public sealed class SnapshotCourseSeederTests
{
    [Fact]
    public async Task SeedAsync_When_Not_Seeded_Should_Import_Courses()
    {
        // Arrange
        var (provider, dbContext) = CreateSeederServices();
        await using (provider)
        {
            // Act
            var result = await SnapshotCourseSeeder.SeedAsync(provider, force: false);

            // Assert
            result.CreatedPrograms.Should().BeGreaterThan(0);
            result.CreatedContents.Should().BeGreaterThan(0);

            var programCount = await dbContext.Set<CourseProgram>().CountAsync();
            programCount.Should().Be(result.CreatedPrograms);
        }
    }

    [Fact]
    public async Task SeedAsync_When_Already_Seeded_And_Force_False_Should_Not_Overwrite_Courses()
    {
        // Arrange
        var (provider, dbContext) = CreateSeederServices();
        await using (provider)
        {
            // Initial seed
            var initialResult = await SnapshotCourseSeeder.SeedAsync(provider, force: false);
            initialResult.CreatedPrograms.Should().BeGreaterThan(0);

            // Mutate a program title in DB to simulate user edit
            var program = await dbContext.Set<CourseProgram>().FirstAsync();
            var originalTitle = program.Title;
            program.Title = "Custom Modified Course Title";
            await dbContext.SaveChangesAsync();

            // Act - second seed with force = false
            var secondResult = await SnapshotCourseSeeder.SeedAsync(provider, force: false);

            // Assert - no new programs created and custom title preserved
            secondResult.CreatedPrograms.Should().Be(0);

            var reloadedProgram = await dbContext.Set<CourseProgram>().FindAsync(program.Id);
            reloadedProgram.Should().NotBeNull();
            reloadedProgram!.Title.Should().Be("Custom Modified Course Title");
            reloadedProgram.Title.Should().NotBe(originalTitle);
        }
    }

    [Fact]
    public async Task SeedAsync_When_Already_Seeded_And_Force_True_Should_Overwrite_Courses()
    {
        // Arrange
        var (provider, dbContext) = CreateSeederServices();
        await using (provider)
        {
            // Initial seed
            var initialResult = await SnapshotCourseSeeder.SeedAsync(provider, force: false);
            initialResult.CreatedPrograms.Should().BeGreaterThan(0);

            // Mutate a program title in DB
            var program = await dbContext.Set<CourseProgram>().FirstAsync();
            var originalTitle = program.Title;
            program.Title = "Custom Modified Course Title";
            await dbContext.SaveChangesAsync();

            // Act - second seed with force = true
            var secondResult = await SnapshotCourseSeeder.SeedAsync(provider, force: true);

            // Assert - title is overwritten back to definition title
            var reloadedProgram = await dbContext.Set<CourseProgram>().FindAsync(program.Id);
            reloadedProgram.Should().NotBeNull();
            reloadedProgram!.Title.Should().Be(originalTitle);
        }
    }

    [Fact]
    public async Task SeedAsync_When_Program_Is_SoftDeleted_Should_Not_Resurrect_It_Even_With_Force()
    {
        // Arrange
        var (provider, dbContext) = CreateSeederServices();
        await using (provider)
        {
            await SnapshotCourseSeeder.SeedAsync(provider, force: false);

            var program = await dbContext.Set<CourseProgram>().FirstAsync();
            program.SoftDelete();
            await dbContext.SaveChangesAsync();

            // Act - re-seed with force = true
            var secondResult = await SnapshotCourseSeeder.SeedAsync(provider, force: true);

            // Assert - program stays deleted and is not re-created
            var reloadedProgram = await dbContext.Set<CourseProgram>()
                .IgnoreQueryFilters()
                .FirstAsync(candidate => candidate.Id == program.Id);
            reloadedProgram.DeletedAt.Should().NotBeNull();
            secondResult.CreatedPrograms.Should().Be(0);
        }
    }

    [Fact]
    public async Task SeedAsync_When_Content_Is_SoftDeleted_Should_Not_Resurrect_It_Even_With_Force()
    {
        // Arrange
        var (provider, dbContext) = CreateSeederServices();
        await using (provider)
        {
            await SnapshotCourseSeeder.SeedAsync(provider, force: false);

            var content = await dbContext.Set<ProgramContent>().IgnoreQueryFilters().FirstAsync();
            var programId = content.ProgramId;
            var totalBefore = await dbContext.Set<ProgramContent>()
                .IgnoreQueryFilters()
                .CountAsync(candidate => candidate.ProgramId == programId);
            content.SoftDelete();
            await dbContext.SaveChangesAsync();

            // Act - re-seed with force = true
            await SnapshotCourseSeeder.SeedAsync(provider, force: true);

            // Assert - content stays deleted and is not re-created or duplicated
            var reloadedContent = await dbContext.Set<ProgramContent>()
                .IgnoreQueryFilters()
                .FirstAsync(candidate => candidate.Id == content.Id);
            reloadedContent.DeletedAt.Should().NotBeNull();

            var totalAfter = await dbContext.Set<ProgramContent>()
                .IgnoreQueryFilters()
                .CountAsync(candidate => candidate.ProgramId == programId);
            totalAfter.Should().Be(totalBefore);

            var activeAfter = await dbContext.Set<ProgramContent>()
                .IgnoreQueryFilters()
                .CountAsync(candidate => candidate.ProgramId == programId && candidate.DeletedAt == null);
            activeAfter.Should().Be(totalBefore - 1);
        }
    }

    private static (ServiceProvider Provider, ApplicationDbContext DbContext) CreateSeederServices()
    {
        var services = new ServiceCollection();
        var databaseName = $"snapshot-seeder-tests-{Guid.NewGuid()}";

        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("Development"));
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));

        var provider = services.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<ApplicationDbContext>();

        return (provider, dbContext);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "GameGuild.API.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
