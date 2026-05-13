using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Tests.Localization.Unit;

public class TranslationWorkflowRepositoryTests
{
    [Fact]
    public void Constructor_CreatesRepository_WhenContextIsProvided()
    {
        var context = new Mock<IApplicationDbContext>();

        var repository = new GameGuild.Localization.TranslationWorkflowRepository(context.Object);

        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ThrowsOnNullContext()
    {
        var act = () => new GameGuild.Localization.TranslationWorkflowRepository(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }
}
