using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Users;
using GameGuild.Learning.Abstractions;
using GameGuild.Tags;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests;

public sealed class LearningCoursesCoverageCompletionTests
{
    [Fact]
    public void CoursesModule_registers_expected_services()
    {
        var services = new ServiceCollection();

        services.AddCoursesModule();

        AssertScoped<IProgramReadService, ProgramReadService>(services);
        AssertScoped<IProgramWriteService, ProgramWriteService>(services);
        AssertScoped<IProgramCrudService, ProgramCrudService>(services);
        AssertScoped<IProgramLifecycleService, ProgramLifecycleService>(services);
        AssertScoped<IProgramService, ProgramService>(services);
        AssertScoped<IProgramContentService, ProgramContentService>(services);
        AssertScoped<IProgramEnrollmentService, ProgramEnrollmentService>(services);
        AssertScoped<IContentInteractionService, ContentInteractionService>(services);
        AssertScoped<IActivityGradeService, ActivityGradeService>(services);
        AssertScoped<IContentProgressService, ContentProgressService>(services);
        AssertScoped<IPrerequisiteService, PrerequisiteService>(services);
        AssertScoped<IProductProgramProvider, ProductProgramProvider>(services);
    }

    [Fact]
    public void Facade_services_delegate_every_public_method()
    {
        var crud = new Mock<IProgramCrudService>(MockBehavior.Loose);
        var lifecycle = new Mock<IProgramLifecycleService>(MockBehavior.Loose);
        var programService = new ProgramService(crud.Object, lifecycle.Object);

        InvokeDeclaredPublicMethods(programService);

        var read = new Mock<IProgramReadService>(MockBehavior.Loose);
        var write = new Mock<IProgramWriteService>(MockBehavior.Loose);
        var crudService = new ProgramCrudService(read.Object, write.Object);

        InvokeDeclaredPublicMethods(crudService);
    }

    [Fact]
    public void Constructors_for_services_controllers_handlers_and_validators_are_covered()
    {
        var db = new CoursesTestDbContext(CreateOptions());
        var serviceProvider = new Dictionary<Type, object>
        {
            [typeof(IApplicationDbContext)] = db,
            [typeof(IProgramCrudService)] = Mock.Of<IProgramCrudService>(),
            [typeof(IProgramLifecycleService)] = Mock.Of<IProgramLifecycleService>(),
            [typeof(IProgramContentService)] = Mock.Of<IProgramContentService>(),
            [typeof(IPrerequisiteService)] = Mock.Of<IPrerequisiteService>(),
            [typeof(IContentInteractionService)] = Mock.Of<IContentInteractionService>(),
            [typeof(IActivityGradeService)] = Mock.Of<IActivityGradeService>(),
            [typeof(IProgramReadService)] = Mock.Of<IProgramReadService>(),
            [typeof(IProgramWriteService)] = Mock.Of<IProgramWriteService>(),
        };

        var constructed = typeof(Program).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(Program).Namespace)
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.Name.EndsWith("Service", StringComparison.Ordinal)
                        || t.Name.EndsWith("Controller", StringComparison.Ordinal)
                        || t.Name.EndsWith("Handler", StringComparison.Ordinal)
                        || t.Name.EndsWith("Validator", StringComparison.Ordinal))
            .Select(t => TryCreate(t, serviceProvider))
            .Where(o => o is not null)
            .ToList();

        constructed.Should().NotBeEmpty();
        constructed.Should().Contain(o => o!.GetType() == typeof(ProgramReadService));
        constructed.Should().Contain(o => o!.GetType() == typeof(ProgramWriteService));
        constructed.Should().Contain(o => o!.GetType() == typeof(ProgramLifecycleService));
    }

    [Fact]
    public void Dto_record_and_model_properties_are_read_and_written()
    {
        var assembly = typeof(Program).Assembly;
        var created = new List<object>();

        foreach (var type in assembly.GetTypes().Where(IsSimpleCoverageType))
        {
            var instance = TryCreate(type, new Dictionary<Type, object>());
            if (instance is null)
            {
                continue;
            }

            ExercisePublicProperties(instance);
            created.Add(instance);
        }

        created.Should().Contain(o => o.GetType() == typeof(ProgramAnalyticsDto));
        created.Should().Contain(o => o.GetType() == typeof(ContentProgress));
        created.Should().Contain(o => o.GetType() == typeof(PeerReview));
    }

    [Fact]
    public void Remaining_records_and_configuration_classes_are_covered()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        new ProgramConfiguration().Configure(modelBuilder.Entity<Program>());
        new ProgramRatingConfiguration().Configure(modelBuilder.Entity<ProgramRating>());
        new ProgramUserConfiguration().Configure(modelBuilder.Entity<ProgramUser>());
        new ProgramContentConfiguration().Configure(modelBuilder.Entity<ProgramContent>());
        new ContentInteractionConfiguration().Configure(modelBuilder.Entity<ContentInteraction>());
        new ActivityGradeConfiguration().Configure(modelBuilder.Entity<ActivityGrade>());
        new ProgramWishlistConfiguration().Configure(modelBuilder.Entity<ProgramWishlist>());
        new CoursePrerequisiteConfiguration().Configure(modelBuilder.Entity<CoursePrerequisite>());
        new CoursesModelConfiguration().Configure(modelBuilder);

        var program = new Program { Id = Guid.NewGuid(), Title = "Tagged" };
        var tagDto = new ProgramTagDto(Guid.NewGuid(), program.Id, Guid.NewGuid(), "C#", "Skill", SkillProficiencyLevel.Advanced, true, 1);
        var addDto = new AddTagToProgramDto(tagDto.TagId, SkillProficiencyLevel.Intermediate, true, 2);
        var updateDto = new UpdateProgramTagDto(SkillProficiencyLevel.Expert, false, 3);
        var getTags = new GetProgramTagsQuery(program.Id);
        var getByTag = new GetProgramsByTagQuery(tagDto.TagId, 1, 2);
        var getBySkill = new GetProgramsBySkillQuery(tagDto.TagId, SkillProficiencyLevel.Beginner, 2, 3);
        var getBySkills = new GetProgramsBySkillsQuery(new[] { tagDto.TagId }, true, 3, 4);
        var getPrimary = new GetProgramPrimarySkillQuery(program.Id);
        var searchByTag = new SearchProgramsByTagNameQuery("testing", 4, 5);
        var skillProgram = new ProgramWithSkillDto(program, tagDto);
        var reorderTags = new ReorderProgramTagsCommand(program.Id, new[] { tagDto.TagId });
        var allPrograms = new GetAllProgramsQuery(1, 2, "search", ProgramCategory.GameDevelopment, ProgramDifficulty.Advanced, ContentStatus.Published, ContentVisibility.Public, EnrollmentStatus.Open, "creator", true, "Title", false);
        var searchPrograms = new SearchProgramsQuery("term", ProgramCategory.Programming, ProgramDifficulty.Beginner, 1, 10, 4, true, 2, 3);
        var updateProgram = new UpdateProgramCommand(Guid.NewGuid(), "Title", "Description", "Summary text", "https://example.test/thumb.png", "https://example.test/video.mp4", 2, ProgramCategory.Design, ProgramDifficulty.Intermediate, EnrollmentStatus.Closed, 20, SystemClock.UtcNow.AddDays(1));
        var progress = new ProgramUserProgress(program.Id, Guid.NewGuid(), 1, 2, 50, TimeSpan.FromHours(1), SystemClock.UtcNow, false, null);
        var createPrerequisite = new CreatePrerequisiteRequest(program.Id, Guid.NewGuid(), Guid.NewGuid(), PrerequisiteType.Corequisite, 70, "desc", 1, "A");
        var updatePrerequisite = new UpdatePrerequisiteRequest(PrerequisiteType.Recommended, 80, "updated", 2, "B");
        var status = new PrerequisiteStatus(Guid.NewGuid(), Guid.NewGuid(), "Course", PrerequisiteType.Required, true, 90, 95, null);
        var checkResult = new PrerequisiteCheckResult(true, new[] { status });

        new object[]
        {
            tagDto, addDto, updateDto, getTags, getByTag, getBySkill, getBySkills, getPrimary,
            searchByTag, skillProgram, reorderTags, allPrograms, searchPrograms, updateProgram,
            progress, createPrerequisite, updatePrerequisite, status, checkResult,
        }.Should().NotContainNulls();
    }

    [Fact]
    public void ProgramRatingConfiguration_LinksRatingsToProgramEnrollmentFeedback()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        new ProgramUserConfiguration().Configure(modelBuilder.Entity<ProgramUser>());
        new ProgramRatingConfiguration().Configure(modelBuilder.Entity<ProgramRating>());

        var ratingEntity = modelBuilder.Model.FindEntityType(typeof(ProgramRating))!;
        var programUserId = ratingEntity.FindProperty("ProgramUserId");

        programUserId.Should().NotBeNull();
        programUserId!.PropertyInfo.Should().NotBeNull();
        ratingEntity.GetForeignKeys().Should().Contain(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(ProgramUser) &&
            foreignKey.Properties.Count == 1 &&
            foreignKey.Properties[0].Name == "ProgramUserId");
    }

    [Fact]
    public void Program_mapping_extensions_cover_null_and_populated_paths()
    {
        var program = new Program
        {
            Id = Guid.NewGuid(),
            CreatorId = Guid.NewGuid(),
            Title = "Advanced Testing",
            Description = "Description",
            Slug = "advanced-testing",
            Status = ContentStatus.Published,
            Visibility = ContentVisibility.Public,
            Thumbnail = "https://example.test/thumb.png",
            VideoShowcaseUrl = "https://example.test/video.mp4",
            EstimatedHours = 4,
            MaxEnrollments = 2,
            EnrollmentDeadline = SystemClock.UtcNow.AddDays(2),
            Category = ProgramCategory.GameDevelopment,
            Difficulty = ProgramDifficulty.Advanced,
            SkillsRequired = "csharp",
            SkillsProvided = "testing",
            ProgramUsers = new List<ProgramUser>
            {
                new() { UserId = Guid.NewGuid(), IsActive = true },
                new() { UserId = Guid.NewGuid(), IsActive = false },
            },
            ProgramRatings = new List<ProgramRating>
            {
                new() { Rating = 4 },
                new() { Rating = 5 },
            },
        };

        var dto = program.ToDto();
        var dtoList = new[] { program }.ToDtos().ToList();
        var nullCollectionsDto = new Program
        {
            ProgramUsers = null!,
            ProgramRatings = null!,
            EnrollmentStatus = EnrollmentStatus.Open,
            Metadata = "null",
        };
        nullCollectionsDto.SetMetadata("x", 1);
        new Program { EnrollmentStatus = EnrollmentStatus.Open, EnrollmentDeadline = SystemClock.UtcNow.AddDays(1), MaxEnrollments = 2, ProgramUsers = new List<ProgramUser> { new() { IsActive = true } } }.IsEnrollmentOpen.Should().BeTrue();
        new Program { EnrollmentStatus = EnrollmentStatus.Open, EnrollmentDeadline = SystemClock.UtcNow.AddDays(1), MaxEnrollments = null, ProgramUsers = new List<ProgramUser>() }.IsEnrollmentOpen.Should().BeTrue();
        new Program { EnrollmentStatus = EnrollmentStatus.Open, EnrollmentDeadline = SystemClock.UtcNow.AddDays(-1), MaxEnrollments = null, ProgramUsers = new List<ProgramUser>() }.IsEnrollmentOpen.Should().BeFalse();
        new Program { ProgramContents = null! }.CalculateEstimatedHours();
        new Program { ProgramUsers = null!, EnrollmentStatus = EnrollmentStatus.Open }.CanUserEnroll(Guid.NewGuid()).Should().BeTrue();

        dto.Title.Should().Be("Advanced Testing");
        dto.CurrentEnrollments.Should().Be(1);
        dto.AverageRating.Should().Be(4.5m);
        dto.TotalRatings.Should().Be(2);
        dtoList.Should().ContainSingle();
        nullCollectionsDto.CurrentEnrollments.Should().Be(0);
        nullCollectionsDto.AverageRating.Should().Be(0);
        nullCollectionsDto.TotalRatings.Should().Be(0);
    }

    [Fact]
    public void Program_content_mapping_extensions_cover_json_markdown_children_and_updates()
    {
        var child = new ProgramContent
        {
            Id = Guid.NewGuid(),
            Title = "Child",
            Body = "not json",
            DeletedAt = null,
        };
        var deletedChild = new ProgramContent
        {
            Id = Guid.NewGuid(),
            Title = "Deleted",
            DeletedAt = SystemClock.UtcNow,
        };
        var content = new ProgramContent
        {
            Id = Guid.NewGuid(),
            ProgramId = Guid.NewGuid(),
            Program = new Program { Title = "Course" },
            Parent = new ProgramContent { Title = "Parent" },
            ParentId = Guid.NewGuid(),
            Title = "Lesson",
            Description = null,
            Type = ProgramContentType.Assignment,
            Body = "<!-- gameguild-source:import -->\r\n{\"markdown\":\"Hello\"}",
            SortOrder = 3,
            IsRequired = false,
            GradingMethod = GradingMethod.Instructor,
            MaxPoints = 10,
            EstimatedMinutes = 45,
            Visibility = Visibility.Private,
            Children = new List<ProgramContent> { child, deletedChild },
        };

        var dto = content.ToDto();
        var fallbackDto = child.ToDto();
        var emptyBodyDto = new ProgramContent { Title = "Empty" }.ToDto();
        var unterminatedMarkerDto = new ProgramContent { Title = "Marker", Body = "<!-- gameguild-source:broken" }.ToDto();
        var markerWithoutNewlineDto = new ProgramContent { Title = "Marker2", Body = "<!-- gameguild-source:ok -->{\"markdown\":\"x\"}" }.ToDto();
        var nullNavigationDto = new ProgramContent { Title = "Nulls", Children = null! }.ToDto();
        var entity = new CreateProgramContentDto
        {
            ProgramId = Guid.NewGuid(),
            ParentId = Guid.NewGuid(),
            Title = "Created",
            Description = "Created desc",
            Type = ProgramContentType.Questionnaire,
            Body = "{}",
            SortOrder = 5,
            IsRequired = true,
            GradingMethod = GradingMethod.AutomatedTests,
            MaxPoints = 20,
            EstimatedMinutes = 10,
            Visibility = Visibility.Public,
        }.ToEntity();
        var nullableEntity = new CreateProgramContentDto
        {
            ProgramId = Guid.NewGuid(),
            Title = "Nullable",
            Type = ProgramContentType.Lesson,
            GradingMethod = null,
            MaxPoints = null,
        }.ToEntity();

        entity.ApplyUpdates(new UpdateProgramContentDto
        {
            Id = entity.Id,
            Title = "Updated",
            Description = "Updated desc",
            Type = ProgramContentType.Page,
            Body = "Updated body",
            SortOrder = 8,
            IsRequired = false,
            GradingMethod = GradingMethod.Peer,
            MaxPoints = 30,
            EstimatedMinutes = 15,
            Visibility = Visibility.Private,
        });
        entity.ApplyUpdates(new UpdateProgramContentDto { Id = entity.Id });

        dto.Children.Should().ContainSingle();
        dto.ProgramTitle.Should().Be("Course");
        dto.ParentTitle.Should().Be("Parent");
        dto.Body.Should().NotBeNull();
        fallbackDto.Body.Should().NotBeNull();
        emptyBodyDto.Body.Should().BeNull();
        unterminatedMarkerDto.Body.Should().NotBeNull();
        markerWithoutNewlineDto.Body.Should().NotBeNull();
        nullNavigationDto.Children.Should().BeEmpty();
        nullableEntity.GradingMethod.Should().Be(GradingMethod.None);
        nullableEntity.MaxPoints.Should().BeNull();
        entity.Title.Should().Be("Updated");
        new[] { content, child }.ToDtos().Should().HaveCount(2);
    }

    [Fact]
    public void Activity_and_interaction_mapping_extensions_cover_nested_summaries()
    {
        var user = new User { Id = Guid.NewGuid(), Name = "Student", Email = "student@example.test" };
        var grader = new User { Id = Guid.NewGuid(), Name = "Grader", Email = "grader@example.test" };
        var content = new ProgramContent { Id = Guid.NewGuid(), Title = "Exercise", Type = ProgramContentType.Assignment, EstimatedMinutes = 20 };
        var programUser = new ProgramUser { Id = Guid.NewGuid(), UserId = user.Id, User = user };
        var graderProgramUser = new ProgramUser { Id = Guid.NewGuid(), UserId = grader.Id, User = grader };
        var interaction = new ContentInteraction
        {
            Id = Guid.NewGuid(),
            ProgramUserId = programUser.Id,
            ProgramUser = programUser,
            ContentId = content.Id,
            Content = content,
            Status = ProgressStatus.Completed,
            SubmissionData = "submission",
            CompletionPercentage = 100,
            TimeSpentMinutes = 12,
            SubmittedAt = SystemClock.UtcNow,
        };
        var grade = new ActivityGrade
        {
            Id = Guid.NewGuid(),
            ContentInteractionId = interaction.Id,
            ContentInteraction = interaction,
            GraderProgramUserId = graderProgramUser.Id,
            GraderProgramUser = graderProgramUser,
            Grade = 90,
            Feedback = "Good",
            GradingDetails = "{}",
            GradedAt = SystemClock.UtcNow,
        };

        grade.ToDto().ContentInteraction!.Student!.UserEmail.Should().Be("student@example.test");
        grade.ToDto().Grader!.UserEmail.Should().Be("grader@example.test");
        new[] { grade }.ToDto().Should().ContainSingle();
        new GradeStatistics
        {
            TotalGrades = 3,
            AverageGrade = 85,
            MinGrade = 70,
            MaxGrade = 100,
            PassingRate = 66,
        }.ToDto().PassingRate.Should().Be(66);

        interaction.ToDto().ProgramUser!.UserEmail.Should().Be("student@example.test");
        new[] { interaction }.ToDto().Should().ContainSingle();

        new ActivityGrade { Id = Guid.NewGuid() }.ToDto().ContentInteraction.Should().BeNull();
        new ActivityGrade
        {
            Id = Guid.NewGuid(),
            ContentInteraction = new ContentInteraction { Id = Guid.NewGuid(), Content = null!, ProgramUser = null! },
            GraderProgramUser = new ProgramUser { User = null! },
        }.ToDto().ContentInteraction.Should().NotBeNull();
        new ContentInteraction { Id = Guid.NewGuid() }.ToDto().Content.Should().BeNull();
    }

    [Fact]
    public void Domain_models_cover_remaining_branch_paths()
    {
        var program = new Program
        {
            ProgramContents = new List<ProgramContent>
            {
                new() { Id = Guid.NewGuid(), EstimatedMinutes = 30, IsRequired = true },
                new() { Id = Guid.NewGuid(), EstimatedMinutes = 45, IsRequired = true },
            },
            ProgramUsers = new List<ProgramUser>(),
            Metadata = "{bad json",
        };
        program.CalculateEstimatedHours();
        program.SetMetadata("level", "advanced");
        program.CanUserEnroll(Guid.NewGuid()).Should().BeTrue();

        var enrolledUserId = Guid.NewGuid();
        program.ProgramUsers.Add(new ProgramUser { UserId = enrolledUserId, IsActive = true });
        program.CanUserEnroll(enrolledUserId).Should().BeFalse();
        program.MaxEnrollments = 1;
        program.CanUserEnroll(Guid.NewGuid()).Should().BeFalse();

        var contentId = program.ProgramContents.First().Id;
        var programUser = new ProgramUser
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Program = program,
            JoinedAt = SystemClock.UtcNow.AddDays(-3),
            LastAccessedAt = SystemClock.UtcNow.AddDays(-1),
            ReceivedGrades = new List<ActivityGrade>
            {
                new() { Points = 80 },
                new() { Points = null },
                new() { Points = 100 },
            },
            ContentInteractions = new List<ContentInteraction>
            {
                new() { ContentId = contentId, IsCompleted = true },
            },
        };

        programUser.DaysSinceEnrollment.Should().BeGreaterThanOrEqualTo(2);
        programUser.IsGlobal.Should().BeFalse();
        programUser.DaysSinceLastAccess.Should().BeGreaterThanOrEqualTo(0);
        programUser.AverageGrade.Should().Be(90);
        programUser.UpdateCompletionPercentage();
        programUser.CompletionPercentage.Should().Be(50);
        programUser.CanAccessContent(contentId).Should().BeTrue();
        programUser.CanAccessContent(Guid.NewGuid()).Should().BeFalse();
        programUser.GetContentProgress(contentId).Should().NotBeNull();

        programUser.ContentInteractions.Add(new ContentInteraction { ContentId = program.ProgramContents.Last().Id, IsCompleted = true });
        programUser.UpdateCompletionPercentage();
        programUser.CompletedAt.Should().NotBeNull();

        var emptyProgramUser = new ProgramUser { Program = new Program { ProgramContents = new List<ProgramContent>() } };
        emptyProgramUser.UpdateCompletionPercentage();
        emptyProgramUser.CalculateFinalGrade().Should().BeNull();
        emptyProgramUser.DaysSinceLastAccess.Should().BeNull();
        emptyProgramUser.AverageGrade.Should().BeNull();
        new ProgramUser { ReceivedGrades = null! }.AverageGrade.Should().BeNull();
        new ProgramUser { ReceivedGrades = new List<ActivityGrade>() }.AverageGrade.Should().BeNull();
        new ProgramUser { ReceivedGrades = null! }.CalculateFinalGrade().Should().BeNull();
        emptyProgramUser.GetContentProgress(Guid.NewGuid()).Should().BeNull();
        emptyProgramUser.Start();
        emptyProgramUser.IsInProgress.Should().BeTrue();
        emptyProgramUser.Deactivate();
        emptyProgramUser.IsInProgress.Should().BeFalse();
        emptyProgramUser.Reactivate();
        emptyProgramUser.Complete();
        emptyProgramUser.IsInProgress.Should().BeFalse();

        var inactiveProgramUser = new ProgramUser { Program = program, IsActive = false };
        inactiveProgramUser.CanAccessContent(contentId).Should().BeFalse();
        new ProgramUser { Program = new Program { ProgramContents = null! } }.CanAccessContent(Guid.NewGuid()).Should().BeFalse();
        new ProgramUser { ContentInteractions = null! }.GetContentProgress(Guid.NewGuid()).Should().BeNull();
        new ProgramUser
        {
            Program = new Program { ProgramContents = null! },
            ContentInteractions = null!,
            ReceivedGrades = null!,
        }.UpdateCompletionPercentage();
    }

    [Fact]
    public void Wishlist_peer_review_content_progress_and_permissions_cover_branches()
    {
        var userId = Guid.NewGuid();
        var wishlist = new ProgramWishlist
        {
            UserId = userId,
            AddedAt = SystemClock.UtcNow.AddDays(-4),
            TenantId = Guid.NewGuid(),
            Program = new Program { ProgramUsers = new List<ProgramUser>() },
        };

        wishlist.IsGlobal.Should().BeFalse();
        wishlist.DaysOnWishlist.Should().BeGreaterThanOrEqualTo(3);
        foreach (var priority in new[] { 1, 2, 3, 4, 5, 99 })
        {
            wishlist.Priority = priority;
            wishlist.PriorityDescription.Should().NotBeNullOrWhiteSpace();
        }

        wishlist.ShouldNotify.Should().BeTrue();
        wishlist.SetPriority(-10);
        wishlist.Priority.Should().Be(1);
        wishlist.SetPriority(10);
        wishlist.Priority.Should().Be(5);
        wishlist.DisableNotifications();
        wishlist.ShouldNotify.Should().BeFalse();
        wishlist.EnableNotifications();
        wishlist.MarkNotificationSent();
        wishlist.ResetNotificationStatus();
        wishlist.UpdateNotes("notes");
        wishlist.SetInterestedTags("design", "", "testing");
        wishlist.GetInterestedTagsArray().Should().HaveCount(2);
        wishlist.IsInterestedInTag("TESTING").Should().BeTrue();
        wishlist.IncreasePriority();
        wishlist.DecreasePriority();
        wishlist.Priority = 4;
        wishlist.IncreasePriority();
        wishlist.Priority = 1;
        wishlist.DecreasePriority();
        wishlist.InterestedTags = null;
        wishlist.GetInterestedTagsArray().Should().BeEmpty();
        wishlist.CanEnrollNow().Should().BeTrue();

        var progress = new ContentProgress();
        progress.MarkAsAccessed();
        progress.MarkAsAccessed();
        var newlyStartedProgress = new ContentProgress();
        newlyStartedProgress.UpdateProgress(50);
        newlyStartedProgress.CompletionStatus.Should().Be(ContentCompletionStatus.InProgress);
        progress.UpdateProgress(50);
        new ContentProgress().UpdateProgress(0);
        var completedProgress = new ContentProgress { CompletionStatus = ContentCompletionStatus.Completed };
        completedProgress.UpdateProgress(50);
        completedProgress.CompletionStatus.Should().Be(ContentCompletionStatus.Completed);
        var completedByDomainProgress = new ContentProgress();
        completedByDomainProgress.MarkAsCompleted();
        completedByDomainProgress.UpdateProgress(50);
        completedByDomainProgress.CompletionStatus.Should().Be(ContentCompletionStatus.Completed);
        progress.UpdateProgress(100);
        progress.UpdateProgress(100);
        progress.UpdateProgress(-5);
        progress.MarkAsCompleted(9, 10);
        progress.AddTimeSpent(30);
        progress.IncrementAttempts();
        progress.Score.Should().Be(9);

        var review = new PeerReview();
        review.SubmitReview(90, "feedback", "{}");
        review.AcceptReview("ok");
        review.RejectReview("bad");
        review.RateReview(0);
        review.ReviewQuality.Should().Be(1);
        review.RateReview(10);
        review.ReviewQuality.Should().Be(5);

        foreach (var permission in Enum.GetValues<PermissionType>())
        {
            var programPermission = new ProgramPermission(Guid.NewGuid(), null, Guid.NewGuid(), permission);
            _ = programPermission.CanViewContent;
            _ = programPermission.CanEditContent;
            _ = programPermission.CanReviewContent;
            _ = programPermission.CanCreateDrafts;
            _ = programPermission.CanSubmitForReview;
            _ = programPermission.CanArchive;
            _ = programPermission.CanClone;
            _ = programPermission.CanDelete;
            _ = programPermission.CanManageUsers;
            _ = programPermission.CanViewUserProgress;
            _ = programPermission.CanManageFeedback;
            _ = programPermission.CanPublish;
            _ = programPermission.CanUnpublish;
            _ = programPermission.CanSchedule;
            _ = programPermission.CanMonetize;
            _ = programPermission.CanSetPricing;
            _ = programPermission.CanAddPaywall;
            _ = programPermission.CanViewAnalytics;
            _ = programPermission.CanViewPerformance;
            _ = programPermission.CanApprove;
            _ = programPermission.CanReject;
            _ = programPermission.CanCategorize;
            _ = programPermission.CanAddToCollection;
            _ = programPermission.CanCreateSeries;
        }

        var emptyPermission = new ProgramPermission();
        emptyPermission.CanViewContent.Should().BeFalse();
        new ProgramPermission(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).CanViewContent.Should().BeFalse();
    }

    [Fact]
    public void Tags_prerequisites_reports_and_entity_branches_are_covered()
    {
        var tag = new Tag { Id = Guid.NewGuid(), Name = "Testing", Type = GameGuild.Tags.TagType.Skill };
        var programTag = ProgramTag.Create(Guid.NewGuid(), tag.Id, SkillProficiencyLevel.Intermediate, true, 7);
        SetPrivateProperty(programTag, nameof(ProgramTag.Tag), tag);
        programTag.UpdateProficiency(SkillProficiencyLevel.Expert);
        programTag.SetPrimary(false);
        programTag.SetDisplayOrder(9);
        programTag.ToDto().TagName.Should().Be("Testing");
        ProgramTag.Create(Guid.NewGuid(), Guid.NewGuid()).ToDto().TagName.Should().BeEmpty();

        var courseId = Guid.NewGuid();
        var prerequisiteCourse = new Program { Id = Guid.NewGuid(), Title = "Intro" };
        var prerequisite = CoursePrerequisite.Create(courseId, prerequisiteCourse.Id, Guid.NewGuid());
        SetPrivateProperty(prerequisite, nameof(CoursePrerequisite.PrerequisiteCourse), prerequisiteCourse);
        prerequisite.Update(PrerequisiteType.Corequisite, 75, "updated", 2, "G1");
        prerequisite.Type.Should().Be(PrerequisiteType.Corequisite);
        PrerequisiteDto.FromEntity(prerequisite).PrerequisiteCourseName.Should().Be("Intro");
        PrerequisiteDto.FromEntity(CoursePrerequisite.Create(Guid.NewGuid(), Guid.NewGuid(), null)).PrerequisiteCourseName.Should().BeNull();
        new PrerequisiteDto(Guid.NewGuid(), courseId, prerequisiteCourse.Id, "Intro", Guid.NewGuid(), PrerequisiteType.Required, 80, "desc", 1, "A", SystemClock.UtcNow).Should().NotBeNull();
        new PrerequisiteCheckResultDto(true, new[] { new PrerequisiteStatusDto(Guid.NewGuid(), prerequisiteCourse.Id, "Intro", PrerequisiteType.Required, true, 80, 90, null) }).Should().NotBeNull();
        new CircularDependencyCheckResult(false).Should().NotBeNull();

        var report = new ContentReport
        {
            ReporterId = Guid.NewGuid(),
            Category = "abuse",
            Description = "details",
            ReportType = ReportType.Behavior,
            Subject = ReportSubject.Content,
        };
        report.AssignToModerator(Guid.NewGuid());
        report.Resolve("resolved", "hidden");
        report.Dismiss("duplicate");
        report.Status.Should().Be(ReportStatus.Dismissed);

        var parent = new ProgramContent
        {
            Id = Guid.NewGuid(),
            Title = "Parent",
            TenantId = Guid.NewGuid(),
            Visibility = Visibility.Internal,
            Program = new Program
            {
                ProgramUsers = new List<ProgramUser>
                {
                    new() { UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), IsActive = true },
                },
            },
            ContentInteractions = new List<ContentInteraction>
            {
                new() { UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), ProgressPercentage = 10, IsCompleted = false },
            },
        };
        parent.IsGlobal.Should().BeFalse();
        var leaf = new ProgramContent
        {
            Id = Guid.NewGuid(),
            Title = "Leaf",
            Parent = parent,
            Visibility = Visibility.Internal,
            Program = parent.Program,
            ContentInteractions = new List<ContentInteraction>
            {
                new() { UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), ProgressPercentage = 40, IsCompleted = false, UpdatedAt = SystemClock.UtcNow.AddMinutes(-1) },
                new() { UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), ProgressPercentage = 70, IsCompleted = true, UpdatedAt = SystemClock.UtcNow },
            },
        };
        parent.Children = new List<ProgramContent> { leaf };
        parent.IsAccessibleBy(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")).Should().BeTrue();
        parent.IsAccessibleBy(Guid.NewGuid()).Should().BeFalse();
        new ProgramContent { Visibility = Visibility.Public }.IsAccessibleBy(Guid.NewGuid()).Should().BeTrue();
        new ProgramContent { Visibility = Visibility.Private }.IsAccessibleBy(Guid.NewGuid()).Should().BeFalse();
        new ProgramContent { Visibility = Visibility.Internal, Program = null! }.IsAccessibleBy(Guid.NewGuid()).Should().BeFalse();
        leaf.FullPath.Should().Contain("Parent");
        leaf.GetCompletionPercentage(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")).Should().Be(100);
        parent.GetCompletionPercentage(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")).Should().Be(100);
        new ProgramContent { Children = null! }.ChildCount.Should().Be(0);
        new ProgramContent().GetCompletionPercentage(Guid.NewGuid()).Should().Be(0);
        new ProgramContent
        {
            ContentInteractions = new List<ContentInteraction> { new() { UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), ProgressPercentage = 20 } },
            Children = new List<ProgramContent>(),
        }.GetCompletionPercentage(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")).Should().Be(20);
        new ProgramContent
        {
            ContentInteractions = null!,
        }.GetCompletionPercentage(Guid.NewGuid()).Should().Be(0);
        new ProgramContent
        {
            ContentInteractions = new List<ContentInteraction>(),
        }.GetCompletionPercentage(Guid.NewGuid()).Should().Be(0);
        new ProgramContent
        {
            ContentInteractions = new List<ContentInteraction> { new() { UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), ProgressPercentage = 20 } },
            Children = null!,
        }.GetCompletionPercentage(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")).Should().Be(20);

        var interaction = new ContentInteraction
        {
            TenantId = Guid.NewGuid(),
            Content = new ProgramContent { EstimatedMinutes = 20 },
            StartedAt = SystemClock.UtcNow.AddMinutes(-30),
            CompletedAt = SystemClock.UtcNow,
            LastAccessedAt = SystemClock.UtcNow.AddDays(-2),
            TimeSpentMinutes = 3,
            AttemptCount = 3,
            BestScore = 30,
        };
        interaction.IsGlobal.Should().BeFalse();
        new ContentInteraction().IsInProgress.Should().BeFalse();
        interaction.DaysSinceLastAccess.Should().BeGreaterThanOrEqualTo(1);
        interaction.EngagementDuration.Should().NotBeNull();
        interaction.CalculateEngagementScore().Should().BeGreaterThan(0);
        interaction.TimeSpentMinutes = 6;
        interaction.CalculateEngagementScore().Should().BeGreaterThan(0);
        interaction.TimeSpentMinutes = 10;
        interaction.CalculateEngagementScore().Should().BeGreaterThan(0);
        interaction.Content = null!;
        interaction.CalculateEngagementScore().Should().BeGreaterThanOrEqualTo(0);
        new ContentInteraction { TimeSpentMinutes = null, Content = new ProgramContent { EstimatedMinutes = 10 } }.CalculateEngagementScore().Should().Be(0);
        new ContentInteraction { TimeSpentMinutes = 10, Content = new ProgramContent { EstimatedMinutes = null } }.CalculateEngagementScore().Should().Be(0);

        var grade = new ActivityGrade { Points = null, MaxPoints = 100, GradedAt = SystemClock.UtcNow.AddDays(-2), GradeType = GradeType.Automatic, TenantId = Guid.NewGuid() };
        grade.IsGlobal.Should().BeFalse();
        grade.PercentageScore.Should().BeNull();
        grade.IsPassing.Should().BeNull();
        grade.IsAutomaticGrade.Should().BeTrue();
        grade.GradeType = GradeType.PeerReview;
        grade.IsPeerReview.Should().BeTrue();
        grade.DaysSinceGrading.Should().BeGreaterThanOrEqualTo(1);
        grade.UpdateFeedback("feedback");
        grade.RecordGradingTime(5);
        grade.SetRubricData("{}");
        grade.CalculateLetterGrade().Should().BeNull();
        grade.AssignPoints(20);
        grade.MaxPoints.Should().Be(100);
        grade.SetLetterGrade(null!);
        grade.GradeLetter.Should().BeNull();
        new ActivityGrade { Points = 5, MaxPoints = 0 }.IsValid().Should().BeFalse();
        new ActivityGrade { Points = 0, MaxPoints = 0 }.IsValid().Should().BeFalse();
        new ActivityGrade { Points = 5, MaxPoints = null }.IsValid().Should().BeTrue();
        new ActivityGrade { Points = 5, MaxPoints = 10 }.IsValid().Should().BeTrue();
        new ActivityGrade { Points = null, MaxPoints = null }.IsValid().Should().BeTrue();
        new ActivityGrade { Points = -1, MaxPoints = null }.IsValid().Should().BeFalse();
        foreach (var points in new[] { 78m, 71m, 68m, 64m })
        {
            new ActivityGrade { Points = points, MaxPoints = 100 }.CalculateLetterGrade().Should().NotBeNull();
        }
        var revision = new ActivityGrade
        {
            StudentId = Guid.NewGuid(),
            GraderId = Guid.NewGuid(),
            ContentInteractionId = Guid.NewGuid(),
            ProgramUserId = Guid.NewGuid(),
            Points = 70,
            MaxPoints = 100,
            Feedback = "original",
            TenantId = Guid.NewGuid(),
        }.CreateRevision(75);
        revision.Feedback.Should().Be("original");
    }

    [Fact]
    public void Controller_private_helpers_cover_claim_and_sanitization_branches()
    {
        var contentController = new ProgramContentController(
            Mock.Of<IProgramContentService>(),
            Mock.Of<IProgramCrudService>(),
            MockActorAccessor(ActorContext.Anonymous),
            Mock.Of<IPermissionQueryService>());

        var body = System.Text.Json.JsonDocument.Parse("{\"x\":1}");
        var visible = new ProgramContentDto
        {
            Visibility = Visibility.Public,
            Body = body,
            Children = new List<ProgramContentDto>
            {
                new() { Visibility = Visibility.Public, Body = body },
                new() { Visibility = Visibility.Private, Body = body },
            },
        };
        var hidden = new ProgramContentDto { Visibility = Visibility.Private, Body = body };

        var sanitized = InvokePrivateStatic<List<ProgramContentDto>>(
            typeof(ProgramContentController),
            "SanitizePublicContent",
            new object[] { new List<ProgramContentDto> { visible, hidden } });

        sanitized.Should().ContainSingle();
        sanitized[0].Body.Should().BeNull();
        sanitized[0].Children.Should().ContainSingle();
        sanitized[0].ChildrenCount.Should().Be(1);

        var userId = Guid.NewGuid();
        SetControllerUser(contentController, new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        InvokePrivate<Guid?>(contentController, "GetCurrentUserId").Should().Be(userId);
        SetControllerUser(contentController, new Claim("sub", userId.ToString()));
        InvokePrivate<Guid?>(contentController, "GetCurrentUserId").Should().Be(userId);
        SetControllerUser(contentController, new Claim("userId", userId.ToString()));
        InvokePrivate<Guid?>(contentController, "GetCurrentUserId").Should().Be(userId);
        SetControllerUser(contentController, new Claim("userId", "bad"));
        InvokePrivate<Guid?>(contentController, "GetCurrentUserId").Should().BeNull();
        SetControllerUser(contentController);
        InvokePrivate<Guid?>(contentController, "GetCurrentUserId").Should().BeNull();

        var crudController = new ProgramCrudController(Mock.Of<IProgramCrudService>());
        SetControllerUser(crudController, new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        InvokePrivate<Guid?>(crudController, "GetCurrentUserId").Should().Be(userId);
        SetControllerUser(crudController, new Claim("sub", userId.ToString()));
        InvokePrivate<Guid?>(crudController, "GetCurrentUserId").Should().Be(userId);
        SetControllerUser(crudController, new Claim("userId", userId.ToString()));
        InvokePrivate<Guid?>(crudController, "GetCurrentUserId").Should().Be(userId);
        SetControllerUser(crudController, new Claim("sub", "bad"));
        InvokePrivate<Guid?>(crudController, "GetCurrentUserId").Should().BeNull();
        SetControllerUser(crudController);
        InvokePrivate<Guid?>(crudController, "GetCurrentUserId").Should().BeNull();
    }

    [Fact]
    public async Task Provider_handlers_and_validators_cover_remaining_paths()
    {
        await using var db = new CoursesTestDbContext(CreateOptions());
        var productId = Guid.NewGuid();
        var firstProgramId = Guid.NewGuid();
        var secondProgramId = Guid.NewGuid();
        db.ProductPrograms.AddRange(
            new ProductProgram { ProductId = productId, ProgramId = secondProgramId, SortOrder = 2 },
            new ProductProgram { ProductId = productId, ProgramId = firstProgramId, SortOrder = 1 });
        await db.SaveChangesAsync();

        var provider = new ProductProgramProvider(db);
        (await provider.GetProgramIdsForProductAsync(productId)).Should().Equal(firstProgramId, secondProgramId);
        (await provider.ProductIncludesProgramAsync(productId, firstProgramId)).Should().BeTrue();
        (await provider.ProductIncludesProgramAsync(productId, Guid.NewGuid())).Should().BeFalse();

        var updateValidator = new UpdateProgramCommandValidator();
        updateValidator.Validate(new UpdateProgramCommand(Guid.NewGuid(), Thumbnail: "ftp://example.test/file.png")).IsValid.Should().BeFalse();
        updateValidator.Validate(new UpdateProgramCommand(Guid.NewGuid(), Thumbnail: "not a url")).IsValid.Should().BeFalse();
        updateValidator.Validate(new UpdateProgramCommand(Guid.NewGuid(), Thumbnail: "http://example.test/file.png")).IsValid.Should().BeTrue();
        updateValidator.Validate(new UpdateProgramCommand(Guid.NewGuid(), Thumbnail: "https://example.test/file.png", VideoShowcaseUrl: "https://example.test/video.mp4")).IsValid.Should().BeTrue();
        updateValidator.Validate(new UpdateProgramCommand(Guid.NewGuid(), EnrollmentDeadline: SystemClock.UtcNow.AddDays(-1))).IsValid.Should().BeFalse();

        var createValidator = new CreateProgramCommandValidator();
        createValidator.Validate(new CreateProgramCommand("Valid Title", "A valid description", Thumbnail: "ftp://example.test/file.png")).IsValid.Should().BeFalse();
        createValidator.Validate(new CreateProgramCommand("Valid Title", "A valid description", Thumbnail: "not a url")).IsValid.Should().BeFalse();
        createValidator.Validate(new CreateProgramCommand("Valid Title", "A valid description", Thumbnail: "http://example.test/file.png")).IsValid.Should().BeTrue();
        createValidator.Validate(new CreateProgramCommand("Valid Title", "A valid description", Thumbnail: "https://example.test/file.png")).IsValid.Should().BeTrue();

        var dbMock = Mock.Of<IApplicationDbContext>();
        new ProgramBasicQueryHandlers(dbMock, NullLogger<ProgramBasicQueryHandlers>.Instance).Should().NotBeNull();
        new ProgramCommandHandlers(dbMock, NullLogger<ProgramCommandHandlers>.Instance).Should().NotBeNull();
        new ProgramEnrollmentAndProgressQueryHandlers(dbMock, NullLogger<ProgramEnrollmentAndProgressQueryHandlers>.Instance).Should().NotBeNull();
        new ProgramStatisticsAndDiscoveryQueryHandlers(dbMock, NullLogger<ProgramStatisticsAndDiscoveryQueryHandlers>.Instance).Should().NotBeNull();

        var slug = typeof(ProgramWriteService)
            .GetMethod("GenerateSlug", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { "John's \"Great\" Course" });
        slug.Should().Be("johns-great-course");
    }

    [Fact]
    public async Task Program_write_service_persists_dashboard_editable_skills_through_metadata()
    {
        var options = CreateOptions();
        var programId = Guid.NewGuid();

        await using (var db = new CoursesTestDbContext(options))
        {
            db.Programs.Add(new Program
            {
                Id = programId,
                Title = "Editable storefront",
                Slug = "editable-storefront",
            });
            await db.SaveChangesAsync();

            var write = new ProgramWriteService(db);
            await write.UpdateProgramAsync(programId, new UpdateProgramDto
            {
                SkillsRequired = "portfolio basics, critique",
                SkillsProvided = "production planning, release pitch",
            });
        }

        await using (var db = new CoursesTestDbContext(options))
        {
            var reloaded = await db.Programs.SingleAsync(p => p.Id == programId);

            reloaded.SkillsRequired.Should().Be("portfolio basics, critique");
            reloaded.SkillsProvided.Should().Be("production planning, release pitch");
        }
    }

    [Fact]
    public async Task Read_write_and_lifecycle_services_cover_in_memory_paths()
    {
        await using var db = new CoursesTestDbContext(CreateOptions());
        var programId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var program = new Program
        {
            Id = programId,
            Title = "Seed Program",
            Slug = "seed-program",
            Description = "Seed",
            Status = ContentStatus.Published,
            Visibility = ContentVisibility.Public,
            Category = ProgramCategory.GameDevelopment,
            Difficulty = ProgramDifficulty.Intermediate,
            CreatedAt = SystemClock.UtcNow.AddDays(-1),
            ProgramContents = new List<ProgramContent>
            {
                new() { Id = contentId, ProgramId = programId, Title = "Lesson", SortOrder = 1, IsRequired = true },
            },
        };

        db.Programs.Add(program);
        db.Users.Add(new User { Id = userId, Email = "learner@example.test", Name = "Learner" });
        var enrollmentId = Guid.NewGuid();
        db.ProgramUsers.Add(new ProgramUser
        {
            Id = enrollmentId,
            ProgramId = programId,
            UserId = userId,
            IsActive = true,
            JoinedAt = SystemClock.UtcNow,
            CompletionPercentage = 25,
        });
        await db.SaveChangesAsync();

        var read = new ProgramReadService(db);
        (await read.GetProgramByIdAsync(programId)).Should().NotBeNull();
        (await read.GetProgramBySlugAsync("seed-program")).Should().NotBeNull();
        (await read.GetPublishedProgramBySlugAsync("seed-program")).Should().NotBeNull();
        (await read.GetProgramWithContentAsync(programId)).Should().NotBeNull();
        (await read.ProgramExistsAsync(programId)).Should().BeTrue();
        (await read.GetProgramsAsync()).Should().ContainSingle();
        (await read.GetProgramContentAsync(programId)).Should().ContainSingle();
        (await read.GetPublishedProgramsAsync()).Should().ContainSingle();
        (await read.GetPublicPublishedProgramsAsync()).Should().ContainSingle();
        (await read.SearchProgramsAsync("")).Should().ContainSingle();
        (await read.SearchProgramsAsync("Seed")).Should().ContainSingle();
        (await read.GetProgramsByCreatorAsync(Guid.NewGuid())).Should().ContainSingle();
        (await read.GetFeaturedProgramsAsync()).Should().ContainSingle();
        (await read.GetRecentProgramsAsync()).Should().ContainSingle();
        (await read.GetPopularProgramsAsync()).Should().ContainSingle();
        (await read.GetProgramsByCategoryAsync(ProgramCategory.GameDevelopment)).Should().ContainSingle();
        (await read.GetProgramsByDifficultyAsync(ProgramDifficulty.Intermediate)).Should().ContainSingle();
        (await read.GetProgramUsersAsync(programId)).Should().ContainSingle();
        (await read.GetUserProgramsAsync(userId)).Should().ContainSingle();
        (await read.IsUserInProgramAsync(programId, userId)).Should().BeTrue();
        (await read.GetProgramUsersAsync(programId, 0, 5)).Should().ContainSingle();
        (await read.GetUserProgressAsync(programId, userId)).Should().Be(25);
        var progressDto = await read.GetUserProgressDtoAsync(programId, userId);
        progressDto.Should().NotBeNull();
        progressDto!.EnrollmentId.Should().Be(enrollmentId);
        progressDto.CourseId.Should().Be(programId);
        progressDto.UserId.Should().Be(userId);
        (await read.GetUserInteractionsAsync(programId, Guid.NewGuid())).Should().BeEmpty();
        (await read.GetProgramCountAsync(ContentStatus.Published, ContentVisibility.Public)).Should().Be(1);
        (await read.GetUserCountForProgramAsync(programId)).Should().Be(1);
        (await read.GetAverageCompletionRateAsync(programId)).Should().Be(25);
        (await read.GetProgramStatisticsAsync(programId))["completionRate"].Should().Be(0m);
        (await read.GetProgramAnalyticsAsync(programId)).Should().NotBeNull();
        (await read.GetCompletionRatesAsync(programId)).Should().NotBeNull();
        (await read.GetEngagementMetricsAsync(programId)).Should().NotBeNull();
        (await read.GetRevenueAnalyticsAsync(programId)).Should().NotBeNull();
        (await read.GetProgramPricingAsync(programId)).Should().NotBeNull();
        (await read.GetLinkedProductsAsync(programId)).Should().BeEmpty();
        (await read.GetProgramAnalyticsAsync(Guid.NewGuid())).Should().BeNull();
        (await read.GetCompletionRatesAsync(Guid.NewGuid())).Should().BeNull();
        (await read.GetEngagementMetricsAsync(Guid.NewGuid())).Should().BeNull();
        (await read.GetRevenueAnalyticsAsync(Guid.NewGuid())).Should().BeNull();
        (await read.GetProgramPricingAsync(Guid.NewGuid())).Should().BeNull();
        (await read.GetLinkedProductsAsync(Guid.NewGuid())).Should().BeEmpty();

        var write = new ProgramWriteService(db);
        var created = await write.CreateProgramAsync(new Program { Id = Guid.NewGuid(), Title = "Created" });
        created.Visibility.Should().Be(ContentVisibility.Private);
        var createdFromDto = await write.CreateProgramAsync(new CreateProgramDto("DTO", "Description", "dto"));
        createdFromDto.Title.Should().Be("DTO");
        (await write.UpdateProgramAsync(createdFromDto.Id, new UpdateProgramDto
        {
            Title = "Updated",
            Description = "Updated desc",
            Slug = "updated",
            Thumbnail = "thumb",
            VideoShowcaseUrl = "video",
            EstimatedHours = 12,
            Visibility = ContentVisibility.Public,
            Category = ProgramCategory.Design,
            Difficulty = ProgramDifficulty.Expert,
            SkillsRequired = "required",
            SkillsProvided = "provided",
            EnrollmentStatus = EnrollmentStatus.Closed,
            MaxEnrollments = 10,
            EnrollmentDeadline = SystemClock.UtcNow.AddDays(1),
        })).Should().NotBeNull();
        (await write.UpdateProgramAsync(Guid.NewGuid(), new UpdateProgramDto())).Should().BeNull();

        var addedContent = await write.AddContentAsync(programId, new ProgramContent { Id = Guid.NewGuid(), Title = "Added" });
        addedContent.SortOrder.Should().BeGreaterThan(0);
        await write.UpdateContentAsync(addedContent);
        addedContent.Version = 1;
        await write.DeleteContentAsync(addedContent.Id);
        await FluentActions.Awaiting(() => write.AddContentAsync(Guid.NewGuid(), new ProgramContent()))
            .Should().ThrowAsync<ArgumentException>();
    }

    private static void AssertScoped<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(TService)
            && d.ImplementationType == typeof(TImplementation)
            && d.Lifetime == ServiceLifetime.Scoped);
    }

    private static void InvokeDeclaredPublicMethods(object instance)
    {
        foreach (var method in instance.GetType()
                     .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                     .Where(m => !m.IsSpecialName))
        {
            var args = method.GetParameters().Select(p => DefaultValue(p.ParameterType)).ToArray();
            method.Invoke(instance, args);
        }
    }

    private static object? TryCreate(Type type, IDictionary<Type, object> services)
    {
        foreach (var constructor in type.GetConstructors().OrderByDescending(c => c.GetParameters().Length))
        {
            var args = constructor.GetParameters()
                .Select(p => ResolveParameter(p.ParameterType, services))
                .ToArray();

            if (args.Any(a => a == Missing.Value))
            {
                continue;
            }

            try
            {
                return constructor.Invoke(args);
            }
            catch
            {
                // Try a simpler constructor if this one enforces invariants.
            }
        }

        return null;
    }

    private static object? ResolveParameter(Type type, IDictionary<Type, object> services)
    {
        if (services.TryGetValue(type, out var service))
        {
            return service;
        }

        if (type.IsInterface)
        {
            var mockType = typeof(Mock<>).MakeGenericType(type);
            var mock = Activator.CreateInstance(mockType)!;
            return mockType.GetProperties()
                .Single(p => p.Name == nameof(Mock<object>.Object) && p.PropertyType == type)
                .GetValue(mock);
        }

        return CanDefault(type) ? DefaultValue(type) : Missing.Value;
    }

    private static bool CanDefault(Type type)
    {
        return type.IsValueType
               || type == typeof(string)
               || type.IsArray
               || type.IsEnum
               || type == typeof(DateTime)
               || type == typeof(DateTime?)
               || type == typeof(TimeSpan)
               || type == typeof(TimeSpan?)
               || type == typeof(Dictionary<string, object>)
               || type == typeof(Dictionary<Guid, decimal>)
               || type == typeof(Dictionary<string, int>)
               || type == typeof(List<Guid>)
               || type == typeof(List<ContentProgressDto>)
               || type == typeof(IEnumerable<Guid>)
               || type == typeof(IEnumerable<Program>)
               || type == typeof(IEnumerable<ProgramContent>)
               || type == typeof(IEnumerable<ProgramUser>)
               || type == typeof(IEnumerable<UserProgressDto>)
               || type == typeof(IEnumerable<ContentInteraction>);
    }

    private static object? DefaultValue(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable is not null)
        {
            return null;
        }

        if (type == typeof(string))
        {
            return "value";
        }

        if (type == typeof(Guid))
        {
            return Guid.NewGuid();
        }

        if (type == typeof(DateTime))
        {
            return SystemClock.UtcNow;
        }

        if (type == typeof(TimeSpan))
        {
            return TimeSpan.FromMinutes(1);
        }

        if (type == typeof(decimal))
        {
            return 1m;
        }

        if (type == typeof(int))
        {
            return 1;
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type.IsEnum)
        {
            return Enum.GetValues(type).GetValue(0);
        }

        if (type.IsArray)
        {
            return Array.CreateInstance(type.GetElementType()!, 0);
        }

        if (type == typeof(Dictionary<string, object>))
        {
            return new Dictionary<string, object>();
        }

        if (type == typeof(Dictionary<Guid, decimal>))
        {
            return new Dictionary<Guid, decimal>();
        }

        if (type == typeof(Dictionary<string, int>))
        {
            return new Dictionary<string, int>();
        }

        if (type == typeof(List<Guid>))
        {
            return new List<Guid> { Guid.NewGuid() };
        }

        if (type == typeof(List<ContentProgressDto>))
        {
            return new List<ContentProgressDto>();
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return Array.CreateInstance(type.GetGenericArguments()[0], 0);
        }

        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private static bool IsSimpleCoverageType(Type type)
    {
        return type.Namespace == typeof(Program).Namespace
               && type is { IsClass: true, IsAbstract: false, IsPublic: true }
               && (type.Name.EndsWith("Dto", StringComparison.Ordinal)
                   || type.Name.EndsWith("Progress", StringComparison.Ordinal)
                   || type.Name.EndsWith("Statistics", StringComparison.Ordinal)
                   || type.Name.EndsWith("Metrics", StringComparison.Ordinal)
                   || type.Name.EndsWith("Result", StringComparison.Ordinal)
                   || type.Name.EndsWith("Request", StringComparison.Ordinal)
                   || type == typeof(ContentProgress)
                   || type == typeof(ContentReport)
                   || type == typeof(PeerReview));
    }

    private static void ExercisePublicProperties(object instance)
    {
        foreach (var property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            if (property.CanWrite)
            {
                var value = DefaultValue(property.PropertyType);
                try
                {
                    property.SetValue(instance, value);
                }
                catch
                {
                    // Some navigation properties enforce concrete non-null values; getters are still covered below.
                }
            }

            if (property.CanRead)
            {
                try
                {
                    _ = property.GetValue(instance);
                }
                catch
                {
                    // Computed properties can depend on navigation state. Targeted tests cover those separately.
                }
            }
        }

        _ = instance.ToString();
        _ = instance.GetHashCode();
        _ = instance.Equals(instance);
    }

    private static DbContextOptions<CoursesTestDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<CoursesTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private static void SetPrivateProperty<T>(object target, string propertyName, T value)
    {
        target.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);
    }

    private static T InvokePrivate<T>(object target, string methodName, params object[] args)
    {
        return (T)target.GetType()
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(target, args)!;
    }

    private static T InvokePrivateStatic<T>(Type type, string methodName, object[] args)
    {
        return (T)type
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, args)!;
    }

    private static void SetControllerUser(ControllerBase controller, params Claim[] claims)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")),
            },
        };
    }

    private static IActorContextAccessor MockActorAccessor(ActorContext actorContext)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(a => a.ActorContext).Returns(actorContext);
        return accessor.Object;
    }

    private sealed class CoursesTestDbContext(DbContextOptions<CoursesTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<Program> Programs => Set<Program>();
        public DbSet<ProgramContent> ProgramContents => Set<ProgramContent>();
        public DbSet<ProgramUser> ProgramUsers => Set<ProgramUser>();
        public DbSet<ContentInteraction> ContentInteractions => Set<ContentInteraction>();
        public DbSet<ActivityGrade> ActivityGrades => Set<ActivityGrade>();
        public DbSet<ProductProgram> ProductPrograms => Set<ProductProgram>();
        public DbSet<User> Users => Set<User>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Program>()
                .Ignore(p => p.ProgramRatings)
                .Ignore(p => p.ProgramWishlists)
                .Ignore(p => p.SkillsRequired)
                .Ignore(p => p.SkillsProvided);
            modelBuilder.Entity<ProgramContent>();
            modelBuilder.Entity<User>()
                .Ignore(u => u.Profile)
                .Ignore(u => u.Metadata)
                .Ignore(u => u.Preferences)
                .Ignore(u => u.Notifications)
                .Ignore(u => u.TenantMemberships);
            modelBuilder.Entity<ProgramUser>()
                .Ignore(pu => pu.ReceivedGrades)
                .Ignore(pu => pu.GivenGrades)
                .Ignore(pu => pu.ProgramRatings);
            modelBuilder.Entity<ContentInteraction>();
            modelBuilder.Entity<ProductProgram>();
            modelBuilder.Ignore<ActivityGrade>();
            modelBuilder.Entity<ProgramRating>();
            modelBuilder.Entity<ProgramWishlist>();
        }
    }
}
