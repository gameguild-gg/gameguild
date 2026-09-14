using System.Reflection;
using FluentAssertions;
using GameGuild.Learning.Abstractions;
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
}
