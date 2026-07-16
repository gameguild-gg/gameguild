using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.API.Database.Migrations;
using GameGuild.CQRS;
using GameGuild.Learning.Assessments;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace GameGuild.API.UnitTests.Database;

public sealed class AssignmentDeliveryPostgreSqlMigrationTests
{
    [DockerFact]
    public async Task RegisteredWorkflow_SerializesCueLinksAgainstUpdateAndTreeDeletion()
    {
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("task2_workflow_locks")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();
        await container.StartAsync();
        await using var provider = CreateWorkflowProvider(container.GetConnectionString());
        try
        {
            var fixture = await SeedWorkflowAsync(provider);
            await AssertSingleGuardedDeleteRegistrationAsync(provider, fixture.UpdateContentId);

            await LinkAndUpdateConcurrentlyAsync(provider, fixture);
            await LinkAndDeleteTreeConcurrentlyAsync(provider, fixture);
            await AssertNoActiveCueReferencesInvalidContentAsync(provider);
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    [DockerFact]
    public async Task AdvisoryLifecycleLocks_SerializeConcurrentCueLinkAndContentMutation()
    {
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("task2_locks")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();
        await container.StartAsync();
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(container.GetConnectionString())
                .Options;
            var contentId = Guid.NewGuid();
            await using var firstContext = new ApplicationDbContext(options);
            await using var firstLock = await ProgramContentLifecycleDatabaseLock.AcquireAsync(firstContext, [contentId]);
            firstLock.Should().NotBeNull();

            var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondTask = Task.Run(async () =>
            {
                await using var secondContext = new ApplicationDbContext(options);
                await using var secondLock = await ProgramContentLifecycleDatabaseLock.AcquireAsync(secondContext, [contentId]);
                secondEntered.SetResult();
                await ProgramContentLifecycleDatabaseLock.CommitAsync(secondLock);
            });

            await Task.Delay(300);
            secondEntered.Task.IsCompleted.Should().BeFalse("the first transaction owns the content lifecycle lock");
            await ProgramContentLifecycleDatabaseLock.CommitAsync(firstLock);
            await secondTask.WaitAsync(TimeSpan.FromSeconds(10));
            secondEntered.Task.IsCompleted.Should().BeTrue();
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    [DockerFact]
    public async Task Up_AppliesLegacyRepairAndRejectsInvalidDeliveryContracts()
    {
        var container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("task2_migration")
                .WithUsername("test")
                .WithPassword("test")
                .WithCleanUp(true)
                .Build();

        await container.StartAsync();
        try
        {
            await using var connection = new NpgsqlConnection(container.GetConnectionString());
            await connection.OpenAsync();
            var assessmentId = Guid.NewGuid();
            var submissionId = Guid.NewGuid();
            var from = DateTime.UtcNow.AddDays(2);
            var until = from.AddDays(-1);
            await ExecuteAsync(connection, """
                CREATE TABLE "Assessments" ("Id" uuid PRIMARY KEY, "AvailableFrom" timestamp with time zone NULL, "AvailableUntil" timestamp with time zone NULL);
                CREATE TABLE "AssessmentSubmissions" ("Id" uuid PRIMARY KEY);
                """);
            await using (var seed = new NpgsqlCommand("INSERT INTO \"Assessments\" (\"Id\", \"AvailableFrom\", \"AvailableUntil\") VALUES (@id, @from, @until); INSERT INTO \"AssessmentSubmissions\" (\"Id\") VALUES (@submission);", connection))
            {
                seed.Parameters.AddWithValue("id", assessmentId);
                seed.Parameters.AddWithValue("from", from);
                seed.Parameters.AddWithValue("until", until);
                seed.Parameters.AddWithValue("submission", submissionId);
                await seed.ExecuteNonQueryAsync();
            }

            await ApplyUpAsync(connection);

            await using (var verify = new NpgsqlCommand("SELECT \"AvailableFrom\", \"AvailableUntil\" FROM \"Assessments\" WHERE \"Id\" = @id", connection))
            {
                verify.Parameters.AddWithValue("id", assessmentId);
                await using var reader = await verify.ExecuteReaderAsync();
                (await reader.ReadAsync()).Should().BeTrue();
                reader.GetDateTime(0).Should().BeCloseTo(until, TimeSpan.FromMicroseconds(1));
                reader.GetDateTime(1).Should().BeCloseTo(from, TimeSpan.FromMicroseconds(1));
            }

            await RejectAsync(connection, "UPDATE \"Assessments\" SET \"AvailableFrom\" = now() + interval '2 days', \"AvailableUntil\" = now() + interval '1 day' WHERE \"Id\" = '" + assessmentId + "';");
            await RejectAsync(connection, "UPDATE \"AssessmentSubmissions\" SET \"SubmittedModalities\" = 128 WHERE \"Id\" = '" + submissionId + "';");
            await RejectAsync(connection, "UPDATE \"AssessmentSubmissions\" SET \"SubmittedModalities\" = 1 WHERE \"Id\" = '" + submissionId + "';");
            await RejectAsync(connection, "UPDATE \"AssessmentSubmissions\" SET \"TextPayload\" = 'orphaned' WHERE \"Id\" = '" + submissionId + "';");
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    private static async Task ApplyUpAsync(NpgsqlConnection connection)
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        new ExposedMigration().BuildUp(builder);
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connection.ConnectionString)
                .Options);
        var generator = context.GetService<IMigrationsSqlGenerator>();
        foreach (var command in generator.Generate(builder.Operations, null))
        {
            await ExecuteAsync(connection, command.CommandText);
        }
    }

    private static async Task RejectAsync(NpgsqlConnection connection, string sql)
    {
        Func<Task> action = () => ExecuteAsync(connection, sql);
        await action.Should().ThrowAsync<PostgresException>();
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class ExposedMigration : AddAssignmentDeliveryAndGradingContracts
    {
        public void BuildUp(MigrationBuilder builder) => Up(builder);
    }

    private static ServiceProvider CreateWorkflowProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddCoursesModule();
        services.AddAssessmentsModule();
        services.AddCqrs(typeof(RemoveProgramContentCommandHandler).Assembly);
        return services.BuildServiceProvider();
    }

    private static async Task<WorkflowFixture> SeedWorkflowAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        var courseId = Guid.NewGuid();
        var orderedIds = new[] { Guid.NewGuid(), Guid.NewGuid() }.OrderBy(id => id).ToArray();
        var childId = orderedIds[0];
        var rootId = orderedIds[1];
        var updateContentId = Guid.NewGuid();
        var assessment = Assessment.Create(courseId, "Cue delivery", AssessmentType.Assignment, 100, 60);
        var program = new GameGuild.Learning.Courses.Program { Id = courseId, Title = "Course", Slug = $"course-{courseId:N}" };
        var updateContent = VideoContent(courseId, updateContentId, "Update target");
        var root = VideoContent(courseId, rootId, "Root video");
        var child = VideoContent(courseId, childId, "Child video", rootId);
        context.AddRange(program, assessment, updateContent, root, child);
        await context.SaveChangesAsync();

        return new WorkflowFixture(courseId, assessment.Id, updateContentId, rootId, childId);
    }

    private static async Task AssertSingleGuardedDeleteRegistrationAsync(IServiceProvider provider, Guid contentId)
    {
        await using var scope = provider.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetServices<IRequestHandler<RemoveProgramContentCommand, bool>>().ToArray();
        handlers.Should().ContainSingle().Which.Should().BeOfType<RemoveProgramContentCommandHandler>();
        scope.ServiceProvider.GetServices<ICommandHandler<RemoveProgramContentCommand, bool>>()
            .Should().ContainSingle().Which.Should().BeOfType<RemoveProgramContentCommandHandler>();

        var lifecycleGuard = scope.ServiceProvider.GetRequiredService<IProgramContentLifecycleGuard>();
        lifecycleGuard.Should().BeOfType<AssessmentProgramContentLifecycleGuard>();
        (await lifecycleGuard.HasBlockingDeleteReference(contentId)).Should().BeFalse();
    }

    private static async Task LinkAndUpdateConcurrentlyAsync(IServiceProvider provider, WorkflowFixture fixture)
    {
        await using var gateScope = provider.CreateAsyncScope();
        var gateContext = gateScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var gate = await ProgramContentLifecycleDatabaseLock.AcquireAsync(gateContext, [fixture.UpdateContentId]);
        gate.Should().NotBeNull();

        var linkStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var updateStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var linkTask = Task.Run(async () =>
        {
            await using var scope = provider.CreateAsyncScope();
            linkStarted.SetResult();
            return await scope.ServiceProvider.GetRequiredService<IAssessmentService>().LinkInteractiveVideoCueAsync(
                fixture.AssessmentId,
                new LinkInteractiveVideoCueRequest(fixture.UpdateContentId, "update-race"));
        });
        var updateTask = Task.Run(async () =>
        {
            await using var scope = provider.CreateAsyncScope();
            updateStarted.SetResult();
            try
            {
                await scope.ServiceProvider.GetRequiredService<IProgramContentService>().UpdateContentAsync(
                    new ProgramContent
                    {
                        Id = fixture.UpdateContentId,
                        ProgramId = fixture.CourseId,
                        Title = "Incompatible update",
                        Type = ProgramContentType.Lesson,
                        LessonFormat = LessonContentFormat.Markdown
                    });
                return (Exception?)null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        });

        await Task.WhenAll(linkStarted.Task, updateStarted.Task).WaitAsync(TimeSpan.FromSeconds(10));
        await Task.Delay(300);
        linkTask.IsCompleted.Should().BeFalse("the real cue-link service must wait on the lifecycle lock");
        updateTask.IsCompleted.Should().BeFalse("the real content-update service must wait on the lifecycle lock");
        await ProgramContentLifecycleDatabaseLock.CommitAsync(gate);

        var linkResult = await linkTask.WaitAsync(TimeSpan.FromSeconds(10));
        var updateException = await updateTask.WaitAsync(TimeSpan.FromSeconds(10));
        linkResult.IsSuccess.Should().NotBe(updateException is null,
            "the serialized workflow either links a cue and rejects the update, or updates first and rejects the cue");
    }

    private static async Task LinkAndDeleteTreeConcurrentlyAsync(IServiceProvider provider, WorkflowFixture fixture)
    {
        await using var gateScope = provider.CreateAsyncScope();
        var gateContext = gateScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var gate = await ProgramContentLifecycleDatabaseLock.AcquireAsync(
            gateContext,
            [fixture.RootContentId, fixture.ChildContentId]);
        gate.Should().NotBeNull();

        var rootLinkStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var childLinkStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var deleteStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var rootLinkTask = Task.Run(async () =>
        {
            await using var scope = provider.CreateAsyncScope();
            rootLinkStarted.SetResult();
            return await scope.ServiceProvider.GetRequiredService<IAssessmentService>().LinkInteractiveVideoCueAsync(
                fixture.AssessmentId,
                new LinkInteractiveVideoCueRequest(fixture.RootContentId, "root-race"));
        });
        var childLinkTask = Task.Run(async () =>
        {
            await using var scope = provider.CreateAsyncScope();
            childLinkStarted.SetResult();
            return await scope.ServiceProvider.GetRequiredService<IAssessmentService>().LinkInteractiveVideoCueAsync(
                fixture.AssessmentId,
                new LinkInteractiveVideoCueRequest(fixture.ChildContentId, "child-race"));
        });
        var deleteTask = Task.Run<object>(async () =>
        {
            await using var scope = provider.CreateAsyncScope();
            deleteStarted.SetResult();
            try
            {
                return await scope.ServiceProvider
                    .GetRequiredService<IRequestHandler<RemoveProgramContentCommand, bool>>()
                    .Handle(new RemoveProgramContentCommand(fixture.CourseId, fixture.RootContentId), CancellationToken.None);
            }
            catch (Exception exception)
            {
                return exception;
            }
        });

        await Task.WhenAll(rootLinkStarted.Task, childLinkStarted.Task, deleteStarted.Task).WaitAsync(TimeSpan.FromSeconds(10));
        await Task.Delay(300);
        rootLinkTask.IsCompleted.Should().BeFalse();
        childLinkTask.IsCompleted.Should().BeFalse();
        deleteTask.IsCompleted.Should().BeFalse("the CQRS delete path must acquire the same sorted multi-content locks");
        await ProgramContentLifecycleDatabaseLock.CommitAsync(gate);

        await Task.WhenAll(rootLinkTask, childLinkTask, deleteTask).WaitAsync(TimeSpan.FromSeconds(15));
    }

    private static async Task AssertNoActiveCueReferencesInvalidContentAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var staleCueExists = await (
            from cue in context.Set<InteractiveVideoAssessmentCue>()
            join content in context.Set<ProgramContent>() on cue.ContentId equals content.Id
            where cue.DeletedAt == null &&
                  (content.DeletedAt != null ||
                   content.Type != ProgramContentType.Lesson ||
                   content.LessonFormat != LessonContentFormat.Video)
            select cue.Id).AnyAsync();

        staleCueExists.Should().BeFalse();
    }

    private static ProgramContent VideoContent(Guid courseId, Guid contentId, string title, Guid? parentId = null) => new()
    {
        Id = contentId,
        ProgramId = courseId,
        ParentId = parentId,
        Title = title,
        Type = ProgramContentType.Lesson,
        LessonFormat = LessonContentFormat.Video
    };

    private sealed record WorkflowFixture(
        Guid CourseId,
        Guid AssessmentId,
        Guid UpdateContentId,
        Guid RootContentId,
        Guid ChildContentId);

    private sealed class DockerFactAttribute : FactAttribute
    {
        public DockerFactAttribute()
        {
            try
            {
                using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "version --format {{.Server.Version}}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                if (process == null || !process.WaitForExit(3000) || process.ExitCode != 0) Skip = "Docker is unavailable; PostgreSQL migration execution test was not run.";
            }
            catch
            {
                Skip = "Docker is unavailable; PostgreSQL migration execution test was not run.";
            }
        }
    }
}
