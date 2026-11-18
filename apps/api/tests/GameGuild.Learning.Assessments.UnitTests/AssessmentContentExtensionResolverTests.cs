using FluentAssertions;
using GameGuild.Learning.Assessments.Grading.Abstractions;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

public sealed class AssessmentContentExtensionResolverTests
{
    [Fact]
    public void ResolveOptional_ReturnsTheSingleMatchingExtension()
    {
        var expected = new StubContentExtension("matching");

        var resolved = AssessmentContentExtensionResolver.ResolveOptional(
            new[] { new StubContentExtension("other"), expected },
            "matching",
            "test extension");

        resolved.Should().BeSameAs(expected);
    }

    [Fact]
    public void ResolveOptional_ReturnsNullWhenNoExtensionMatches()
    {
        var resolved = AssessmentContentExtensionResolver.ResolveOptional(
            new[] { new StubContentExtension("other") },
            "missing",
            "test extension");

        resolved.Should().BeNull();
    }

    [Fact]
    public void ResolveOptional_RejectsDuplicateRegistrationsForTheSameContentType()
    {
        var extensions = new[]
        {
            new StubContentExtension("duplicate"),
            new StubContentExtension("duplicate"),
        };

        var resolve = () => AssessmentContentExtensionResolver.ResolveOptional(
            extensions,
            "duplicate",
            "test extension");

        resolve.Should().Throw<InvalidOperationException>()
            .WithMessage("More than one test extension is registered for content type duplicate.");
    }

    private sealed record StubContentExtension(string ContentType) : IAssessmentContentExtension;
}
