using GameGuild.Learning.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Services;

public sealed class ProgramEnrollmentServiceConstructorTests
{
    [Fact]
    public async Task AutoEnroll_UsesConfiguredProductProgramProvider()
    {
        var context = new Mock<IApplicationDbContext>(MockBehavior.Strict);
        var certificates = new Mock<ICertificateIssuanceService>(MockBehavior.Strict);
        var productPrograms = new Mock<IProductProgramProvider>(MockBehavior.Strict);
        var productId = Guid.NewGuid();
        productPrograms.Setup(provider => provider.GetProgramIdsForProductAsync(productId))
            .ReturnsAsync(Array.Empty<Guid>());
        var service = new ProgramEnrollmentService(
            context.Object,
            NullLogger<ProgramEnrollmentService>.Instance,
            certificates.Object,
            productPrograms.Object);

        var enrollments = await service.AutoEnrollInProductProgramsAsync(Guid.NewGuid(), productId);

        Assert.Empty(enrollments);
        productPrograms.VerifyAll();
    }
}
