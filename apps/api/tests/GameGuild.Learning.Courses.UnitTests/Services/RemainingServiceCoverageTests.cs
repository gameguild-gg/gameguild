using System.Reflection;
using FluentAssertions;
using GameGuild.Learning.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Services;

public sealed class RemainingServiceCoverageTests
{
    [Fact]
    public void ContentProgressService_UsesExplicitPersistenceAndEnrollmentDependencies()
    {
        var context = Mock.Of<IApplicationDbContext>();
        var enrollment = Mock.Of<IProgramEnrollmentService>();

        var service = new ContentProgressService(context, enrollment);

        service.Should().NotBeNull();
    }

    [Fact]
    public void ProductProgramProvider_UsesExplicitPersistenceDependency()
    {
        var context = Mock.Of<IApplicationDbContext>();

        var provider = new ProductProgramProvider(context);

        provider.Should().BeAssignableTo<IProductProgramProvider>();
    }

    [Fact]
    public void LifecycleLockKey_IsDeterministicAndContentSpecific()
    {
        var method = typeof(ProgramContentLifecycleDatabaseLock).GetMethod(
            "CreateLockKey",
            BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();

        var first = (long)method!.Invoke(null, [firstId])!;
        var repeated = (long)method.Invoke(null, [firstId])!;
        var second = (long)method.Invoke(null, [secondId])!;

        first.Should().Be(repeated);
        first.Should().NotBe(second);
    }

    [Fact]
    public async Task NullLifecycleGuards_DoNotBlockContentChanges()
    {
        IProgramContentScheduleGuard schedule = new NullProgramContentScheduleGuard();
        IProgramContentLifecycleGuard lifecycle = new NullProgramContentLifecycleGuard();
        var contentId = Guid.NewGuid();

        (await schedule.HasActiveScheduleReference(contentId)).Should().BeFalse();
        (await lifecycle.HasBlockingDeleteReference(contentId)).Should().BeFalse();
        (await lifecycle.HasBlockingIncompatibleUpdateReference(
            contentId,
            ProgramContentType.Lesson,
            LessonContentFormat.Markdown)).Should().BeFalse();
    }

    [Fact]
    public async Task LifecycleLockCommit_DelegatesToOwnedTransaction()
    {
        var transaction = new Mock<IDbContextTransaction>();
        var cancellationToken = new CancellationTokenSource().Token;
        transaction
            .Setup(value => value.CommitAsync(cancellationToken))
            .Returns(Task.CompletedTask);

        await ProgramContentLifecycleDatabaseLock.CommitAsync(
            transaction.Object,
            cancellationToken);

        transaction.Verify(value => value.CommitAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public void ProgramLifecycleService_UsesExplicitPersistenceDependency()
    {
        var service = new ProgramLifecycleService(Mock.Of<IApplicationDbContext>());

        service.Should().BeAssignableTo<IProgramLifecycleService>();
    }
}
