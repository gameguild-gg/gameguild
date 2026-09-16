using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Entities;

public sealed class ProgramEntityCoverageTests
{
    [Fact]
    public void SkillsMetadata_SettersTrimPreserveAndRemoveValues()
    {
        var program = new Program();

        program.SkillsRequired = "  C#, Unity  ";
        program.SkillsProvided = "  Gameplay architecture  ";

        program.SkillsRequired.Should().Be("C#, Unity");
        program.SkillsProvided.Should().Be("Gameplay architecture");

        program.SkillsRequired = null;
        program.SkillsRequired.Should().BeNull();
        program.SkillsProvided.Should().Be("Gameplay architecture");
        program.Metadata.Should().NotBeNull();

        program.SkillsProvided = " ";
        program.Metadata.Should().BeNull();
    }

    [Theory]
    [InlineData("{\"skillsRequired\":\"C++\"}", "C++")]
    [InlineData("{\"skillsRequired\":42}", "42")]
    [InlineData("{\"skillsRequired\":null}", null)]
    [InlineData("{\"other\":\"value\"}", null)]
    [InlineData("invalid-json", null)]
    [InlineData("null", null)]
    public void SkillsMetadata_GetterReadsLegacyAndDefensiveJson(string metadata, string? expected)
    {
        new Program { Metadata = metadata }.SkillsRequired.Should().Be(expected);
    }

    [Fact]
    public void EnrollmentAndRatingAggregates_HandleNullAndPopulatedCollections()
    {
        var program = new Program
        {
            ProgramUsers = null!,
            ProgramRatings = null!
        };

        program.CurrentEnrollments.Should().Be(0);
        program.AverageRating.Should().Be(0m);
        program.TotalRatings.Should().Be(0);

        program.ProgramUsers =
        [
            new ProgramUser { IsActive = true },
            new ProgramUser { IsActive = false }
        ];
        program.ProgramRatings =
        [
            new ProgramRating { Rating = 4m },
            new ProgramRating { Rating = 2m }
        ];

        program.CurrentEnrollments.Should().Be(1);
        program.AverageRating.Should().Be(3m);
        program.TotalRatings.Should().Be(2);
    }

    [Fact]
    public void IsEnrollmentOpen_RequiresOpenStatusFutureDeadlineAndCapacity()
    {
        var program = new Program();
        program.IsEnrollmentOpen.Should().BeTrue();

        program.EnrollmentDeadline = SystemClock.UtcNow.AddDays(1);
        program.MaxEnrollments = 2;
        program.ProgramUsers = [new ProgramUser { IsActive = true }];
        program.IsEnrollmentOpen.Should().BeTrue();

        program.ProgramUsers.Add(new ProgramUser { IsActive = true });
        program.IsEnrollmentOpen.Should().BeFalse();

        program.MaxEnrollments = null;
        program.EnrollmentDeadline = SystemClock.UtcNow.AddMinutes(-1);
        program.IsEnrollmentOpen.Should().BeFalse();

        program.EnrollmentDeadline = null;
        program.CloseEnrollment();
        program.IsEnrollmentOpen.Should().BeFalse();
    }

    [Fact]
    public void CalculateEstimatedHours_OnlyUpdatesWhenContentExistsAndRoundsUp()
    {
        var program = new Program { EstimatedHours = 10, ProgramContents = null! };
        program.CalculateEstimatedHours();
        program.EstimatedHours.Should().Be(10);

        program.ProgramContents = [];
        program.CalculateEstimatedHours();
        program.EstimatedHours.Should().Be(10);

        program.ProgramContents =
        [
            new ProgramContent { EstimatedMinutes = 61 },
            new ProgramContent { EstimatedMinutes = null }
        ];
        program.CalculateEstimatedHours();

        program.EstimatedHours.Should().Be(2);
        program.UpdatedAt.Should().NotBe(default);
    }

    [Fact]
    public void CanUserEnroll_RejectsClosedOrAlreadyActiveEnrollment()
    {
        var userId = Guid.NewGuid();
        var program = new Program { ProgramUsers = null! };
        program.CanUserEnroll(userId).Should().BeTrue();

        program.ProgramUsers =
        [
            new ProgramUser { UserId = userId, IsActive = false },
            new ProgramUser { UserId = Guid.NewGuid(), IsActive = true }
        ];
        program.CanUserEnroll(userId).Should().BeTrue();

        program.ProgramUsers.Add(new ProgramUser { UserId = userId, IsActive = true });
        program.CanUserEnroll(userId).Should().BeFalse();

        program.ProgramUsers.Clear();
        program.CloseEnrollment();
        program.CanUserEnroll(userId).Should().BeFalse();
    }

    [Fact]
    public void IsGlobal_ReflectsTenantScope()
    {
        var program = new Program();
        program.IsGlobal.Should().BeTrue();

        program.TenantId = Guid.NewGuid();
        program.IsGlobal.Should().BeFalse();
    }
}
