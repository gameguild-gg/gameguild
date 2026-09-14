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
}
