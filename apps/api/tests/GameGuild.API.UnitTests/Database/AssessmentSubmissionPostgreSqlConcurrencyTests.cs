using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Learning.Assessments;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;

namespace GameGuild.API.UnitTests.Database;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class AssessmentSubmissionPostgreSqlConcurrencyTests
{
    [Fact]
    public async Task ConcurrentStarts_RespectMaxAttemptsAndAssignOneFirstAttempt()
    {
        var container = await EconomyPostgreSqlTestDatabase.CreateAsync("assessment_attempt_concurrency");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(container.ConnectionString)
                .Options;
            var enrollmentId = Guid.NewGuid();
            Guid assessmentId;
            await using (var setup = new ApplicationDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                var assessment = Assessment.Create(Guid.NewGuid(), "Concurrent quiz", AssessmentType.Quiz, 100);
                assessment.SetMaxAttempts(1);
                assessmentId = assessment.Id;
                setup.Add(assessment);
                await setup.SaveChangesAsync();
            }

            await using var connection = new NpgsqlConnection(container.ConnectionString);
            await connection.OpenAsync();
            await ExecuteAsync(connection, """
                CREATE OR REPLACE FUNCTION pause_assessment_submission_insert() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    PERFORM pg_sleep(1);
                    RETURN NEW;
                END $$;
                CREATE TRIGGER pause_assessment_submission_insert
                    BEFORE INSERT ON "AssessmentSubmissions"
                    FOR EACH ROW EXECUTE FUNCTION pause_assessment_submission_insert();
                """);

            var first = StartAsync(options, assessmentId, enrollmentId);
            await WaitForActiveInsertAsync(connection);
            var second = StartAsync(options, assessmentId, enrollmentId);
            var results = await Task.WhenAll(first, second);

            results.Count(result => result.IsSuccess).Should().Be(1);
            results.Single(result => result.IsSuccess).Value.AttemptNumber.Should().Be(1);
            results.Single(result => !result.IsSuccess).Error.Code.Should().Be("Assessment.MaxAttemptsReached");
            await using var verify = new ApplicationDbContext(options);
            var submissions = await verify.Set<AssessmentSubmission>()
                .Where(submission => submission.AssessmentId == assessmentId && submission.EnrollmentId == enrollmentId)
                .ToListAsync();
            submissions.Should().ContainSingle();
            submissions.Single().AttemptNumber.Should().Be(1);
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    private static async Task<Result<AssessmentSubmission>> StartAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid assessmentId,
        Guid enrollmentId)
    {
        await using var context = new ApplicationDbContext(options);
        var service = new AssessmentService(context, Mock.Of<IProgramContentService>(), new RubricService(context, NullLogger<RubricService>.Instance), NullLogger<AssessmentService>.Instance);
        return await service.StartSubmissionAsync(assessmentId, enrollmentId, Guid.NewGuid());
    }

    private static async Task WaitForActiveInsertAsync(NpgsqlConnection connection)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!timeout.IsCancellationRequested)
        {
            await using var command = new NpgsqlCommand(
                "SELECT EXISTS (SELECT 1 FROM pg_stat_activity WHERE query LIKE 'INSERT INTO \\\"AssessmentSubmissions\\\"%' AND state = 'active')",
                connection);
            if ((bool)(await command.ExecuteScalarAsync(timeout.Token))!) return;
            await Task.Delay(20, timeout.Token);
        }

        throw new TimeoutException("The first assessment submission insert did not become active.");
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
