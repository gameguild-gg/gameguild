using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Models;

public sealed class LessonContentFormatInferenceCoverageTests
{
    [Theory]
    [InlineData("<p>Lesson</p>")]
    [InlineData("   <section>Lesson</section>")]
    public void FromBody_WhenMarkupStartsWithElement_ReturnsHtml(string body)
    {
        Assert.Equal(LessonContentFormat.Html, LessonContentFormatInference.FromBody(body));
    }

    [Fact]
    public void FromBody_WhenJsonObjectIsNotLexical_ReturnsMarkdown()
    {
        Assert.Equal(LessonContentFormat.Markdown, LessonContentFormatInference.FromBody("{}"));
    }

    [Fact]
    public void FromBody_WhenJsonRootIsNotAnObject_ReturnsMarkdown()
    {
        Assert.Equal(LessonContentFormat.Markdown, LessonContentFormatInference.FromBody("[]"));
    }
}
