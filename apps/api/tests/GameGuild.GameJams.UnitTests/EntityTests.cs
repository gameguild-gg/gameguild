using FluentAssertions;
using Xunit;

namespace GameGuild.GameJams.UnitTests;

public class JamTests
{
    [Fact]
    public void Jam_DefaultProperties()
    {
        var jam = new Jam();
        jam.Name.Should().Be(string.Empty);
        jam.Slug.Should().Be(string.Empty);
        jam.Theme.Should().BeNull();
        jam.Description.Should().BeNull();
        jam.Rules.Should().BeNull();
        jam.SubmissionCriteria.Should().BeNull();
        jam.VotingEndDate.Should().BeNull();
        jam.MaxParticipants.Should().BeNull();
        jam.ParticipantCount.Should().Be(0);
        jam.Status.Should().Be(JamStatus.Upcoming);
    }

    [Fact]
    public void Jam_SetProperties()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var jam = new Jam
        {
            Name = "Summer Jam 2026",
            Slug = "summer-jam-2026",
            Theme = "Retro Vibes",
            Description = "A fun retro game jam",
            Rules = "Max 48 hours",
            SubmissionCriteria = "Must include audio",
            StartDate = now,
            EndDate = now.AddDays(2),
            VotingEndDate = now.AddDays(5),
            MaxParticipants = 100,
            ParticipantCount = 42,
            Status = JamStatus.Active,
            CreatedBy = userId
        };

        jam.Name.Should().Be("Summer Jam 2026");
        jam.Slug.Should().Be("summer-jam-2026");
        jam.Theme.Should().Be("Retro Vibes");
        jam.Description.Should().Be("A fun retro game jam");
        jam.Rules.Should().Be("Max 48 hours");
        jam.SubmissionCriteria.Should().Be("Must include audio");
        jam.StartDate.Should().Be(now);
        jam.EndDate.Should().Be(now.AddDays(2));
        jam.VotingEndDate.Should().Be(now.AddDays(5));
        jam.MaxParticipants.Should().Be(100);
        jam.ParticipantCount.Should().Be(42);
        jam.Status.Should().Be(JamStatus.Active);
        jam.CreatedBy.Should().Be(userId);
    }
}

public class JamJudgingCriteriaTests
{
    [Fact]
    public void Defaults()
    {
        var c = new JamJudgingCriteria();
        c.Name.Should().Be(string.Empty);
        c.Description.Should().BeNull();
        c.Weight.Should().Be(1.0m);
        c.MaxScore.Should().Be(5);
    }

    [Fact]
    public void SetProperties()
    {
        var jamId = Guid.NewGuid();
        var c = new JamJudgingCriteria
        {
            JamId = jamId,
            Name = "Innovation",
            Description = "How innovative is the entry?",
            Weight = 2.0m,
            MaxScore = 10
        };
        c.JamId.Should().Be(jamId);
        c.Name.Should().Be("Innovation");
        c.Weight.Should().Be(2.0m);
        c.MaxScore.Should().Be(10);
    }
}

public class JamScoreTests
{
    [Fact]
    public void SetProperties()
    {
        var subId = Guid.NewGuid();
        var critId = Guid.NewGuid();
        var judgeId = Guid.NewGuid();
        var s = new JamScore
        {
            SubmissionId = subId,
            CriteriaId = critId,
            JudgeUserId = judgeId,
            Score = 8,
            Feedback = "Great work!"
        };
        s.SubmissionId.Should().Be(subId);
        s.CriteriaId.Should().Be(critId);
        s.JudgeUserId.Should().Be(judgeId);
        s.Score.Should().Be(8);
        s.Feedback.Should().Be("Great work!");
    }
}

public class JamSubmissionTests
{
    [Fact]
    public void SetProperties()
    {
        var jamId = Guid.NewGuid();
        var pvId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var s = new JamSubmission
        {
            JamId = jamId,
            ProjectVersionId = pvId,
            UserId = userId,
            SubmissionNotes = "First submission"
        };
        s.JamId.Should().Be(jamId);
        s.ProjectVersionId.Should().Be(pvId);
        s.UserId.Should().Be(userId);
        s.SubmissionNotes.Should().Be("First submission");
    }
}

public class JamStatusTests
{
    [Fact]
    public void AllValues()
    {
        var values = Enum.GetValues<JamStatus>();
        values.Should().Contain(JamStatus.Upcoming);
        values.Should().Contain(JamStatus.Active);
        values.Should().Contain(JamStatus.Voting);
        values.Should().Contain(JamStatus.Completed);
        values.Should().Contain(JamStatus.Cancelled);
    }
}
