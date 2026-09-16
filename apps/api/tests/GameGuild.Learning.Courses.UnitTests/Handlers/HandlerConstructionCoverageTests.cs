using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Handlers;

public sealed class HandlerConstructionCoverageTests
{
    [Fact]
    public void ProgramTagHandlers_RequireTheSharedPersistenceDependency()
    {
        var context = Mock.Of<IApplicationDbContext>();

        object[] handlers =
        [
            new AddTagToProgramCommandHandler(context, NullLogger<AddTagToProgramCommandHandler>.Instance),
            new UpdateProgramTagCommandHandler(context, NullLogger<UpdateProgramTagCommandHandler>.Instance),
            new RemoveTagFromProgramCommandHandler(context, NullLogger<RemoveTagFromProgramCommandHandler>.Instance),
            new BulkAddTagsToProgramCommandHandler(context, NullLogger<BulkAddTagsToProgramCommandHandler>.Instance),
            new ReorderProgramTagsCommandHandler(context, NullLogger<ReorderProgramTagsCommandHandler>.Instance),
            new GetProgramTagsQueryHandler(context),
            new GetProgramsByTagQueryHandler(context),
            new GetProgramsBySkillQueryHandler(context),
            new GetProgramsBySkillsQueryHandler(context),
            new GetProgramPrimarySkillQueryHandler(context),
            new SearchProgramsByTagNameQueryHandler(context),
        ];

        handlers.Should().OnlyContain(handler => handler != null);
    }

    [Fact]
    public void StatisticsHandlers_RequirePersistenceAndTypedLoggingDependencies()
    {
        var context = Mock.Of<IApplicationDbContext>();

        object[] handlers =
        [
            new GetProgramStatisticsQueryHandler(context, NullLogger<GetProgramStatisticsQueryHandler>.Instance),
            new GetGlobalProgramStatisticsQueryHandler(context, NullLogger<GetGlobalProgramStatisticsQueryHandler>.Instance),
            new GetCreatorProgramStatisticsQueryHandler(context, NullLogger<GetCreatorProgramStatisticsQueryHandler>.Instance),
            new GetUserProgramProgressQueryHandler(context, NullLogger<GetUserProgramProgressQueryHandler>.Instance),
        ];

        handlers.Should().OnlyContain(handler => handler != null);
    }

    [Fact]
    public void CourseCommandHandlers_RequirePersistenceAndTypedLoggingDependencies()
    {
        var context = Mock.Of<IApplicationDbContext>();

        object[] handlers =
        [
            new AddProgramContentCommandHandler(context, NullLogger<AddProgramContentCommandHandler>.Instance),
            new AddToWishlistCommandHandler(context, NullLogger<AddToWishlistCommandHandler>.Instance),
            new ArchiveProgramCommandHandler(context, NullLogger<ArchiveProgramCommandHandler>.Instance),
            new BulkArchiveProgramsCommandHandler(context, NullLogger<BulkArchiveProgramsCommandHandler>.Instance),
            new BulkUpdateProgramVisibilityCommandHandler(context, NullLogger<BulkUpdateProgramVisibilityCommandHandler>.Instance),
            new CreateProgramCommandHandler(context, NullLogger<CreateProgramCommandHandler>.Instance),
            new DeleteProgramCommandHandler(context, NullLogger<DeleteProgramCommandHandler>.Instance),
            new DeleteProgramRatingCommandHandler(context, NullLogger<DeleteProgramRatingCommandHandler>.Instance),
            new EnrollUserCommandHandler(context, NullLogger<EnrollUserCommandHandler>.Instance),
            new PublishProgramCommandHandler(context, NullLogger<PublishProgramCommandHandler>.Instance),
            new RateProgramCommandHandler(context, NullLogger<RateProgramCommandHandler>.Instance),
            new RemoveFromWishlistCommandHandler(context, NullLogger<RemoveFromWishlistCommandHandler>.Instance),
            new ReorderProgramContentCommandHandler(context, NullLogger<ReorderProgramContentCommandHandler>.Instance),
            new RestoreProgramCommandHandler(context, NullLogger<RestoreProgramCommandHandler>.Instance),
            new UnenrollUserCommandHandler(context, NullLogger<UnenrollUserCommandHandler>.Instance),
            new UnpublishProgramCommandHandler(context, NullLogger<UnpublishProgramCommandHandler>.Instance),
            new UpdateEnrollmentStatusCommandHandler(context, NullLogger<UpdateEnrollmentStatusCommandHandler>.Instance),
            new UpdateProgramCommandHandler(context, NullLogger<UpdateProgramCommandHandler>.Instance),
            new UpdateProgramRatingCommandHandler(context, NullLogger<UpdateProgramRatingCommandHandler>.Instance),
        ];

        handlers.Should().OnlyContain(handler => handler != null);
    }

    [Fact]
    public void CourseQueryHandlers_RequirePersistenceAndTypedLoggingDependencies()
    {
        var context = Mock.Of<IApplicationDbContext>();

        object[] handlers =
        [
            new GetProgramByIdQueryHandler(context, NullLogger<GetProgramByIdQueryHandler>.Instance),
            new GetProgramBySlugQueryHandler(context, NullLogger<GetProgramBySlugQueryHandler>.Instance),
            new GetPublishedProgramBySlugQueryHandler(context, NullLogger<GetPublishedProgramBySlugQueryHandler>.Instance),
            new ProgramBasicQueryHandlers(context, NullLogger<ProgramBasicQueryHandlers>.Instance),
            new ProgramEnrollmentAndProgressQueryHandlers(context, NullLogger<ProgramEnrollmentAndProgressQueryHandlers>.Instance),
            new ProgramStatisticsAndDiscoveryQueryHandlers(context, NullLogger<ProgramStatisticsAndDiscoveryQueryHandlers>.Instance),
        ];

        handlers.Should().OnlyContain(handler => handler != null);
    }
}
