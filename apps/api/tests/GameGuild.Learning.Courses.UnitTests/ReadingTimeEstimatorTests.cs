using FluentAssertions;
using GameGuild.Learning.Courses;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests;

public class ReadingTimeEstimatorTests
{
    [Fact]
    public void Estimate_MarkdownBody400Words_Returns2()
    {
        var body = string.Join(" ", Enumerable.Repeat("word", 400));

        var result = ReadingTimeEstimator.EstimateMinutes(ProgramContentType.Lesson, LessonContentFormat.Markdown, body, null);

        result.Should().Be(2);
    }

    [Fact]
    public void Estimate_MarkdownBodyMultiLine_SplitsOnAnyWhitespace()
    {
        var body = "line one\nline two\nline three";

        var result = ReadingTimeEstimator.EstimateMinutes(ProgramContentType.Lesson, LessonContentFormat.Markdown, body, null);

        result.Should().Be(1);
    }

    [Fact]
    public void Estimate_LexicalJsonWalksTextNodes_Returns2()
    {
        var text = string.Join(" ", Enumerable.Repeat("word", 125));
        var jsonBody = $$$"""
            {"root":{"children":[{"text":"{{{text}}}"},{"children":[{"text":"{{{text}}}"}]}]}}
            """;

        var result = ReadingTimeEstimator.EstimateMinutes(ProgramContentType.Lesson, LessonContentFormat.Lexical, null, jsonBody);

        result.Should().Be(2);
    }

    [Fact]
    public void Estimate_QuizJsonCollectsQuestionAndOption()
    {
        var jsonBody = """{"questions":[{"question":"a b c","choices":[{"option":"d e"}]}]}""";

        var result = ReadingTimeEstimator.EstimateMinutes(ProgramContentType.Questionnaire, null, null, jsonBody);

        result.Should().Be(1);
    }

    [Fact]
    public void Estimate_VideoLessonFormat_ReturnsNull()
    {
        var result = ReadingTimeEstimator.EstimateMinutes(ProgramContentType.Lesson, LessonContentFormat.Video, "https://youtube.com/watch?v=x", null);

        result.Should().BeNull();
    }

    [Fact]
    public void Estimate_EmptyInputs_ReturnsNull()
    {
        ReadingTimeEstimator.EstimateMinutes(ProgramContentType.Lesson, null, null, null).Should().BeNull();
        ReadingTimeEstimator.EstimateMinutes(ProgramContentType.Lesson, null, "   ", null).Should().BeNull();
    }

    [Fact]
    public void Estimate_HtmlBody_StripsTags()
    {
        var body = "<p>Hello <strong>world</strong></p>";

        var result = ReadingTimeEstimator.EstimateMinutes(ProgramContentType.Lesson, LessonContentFormat.Html, body, null);

        result.Should().Be(1);
    }

    [Fact]
    public void Estimate_MalformedJson_FallsBackToBody_NoThrow()
    {
        var jsonBody = "{not valid json";
        var body = "one two three four five";

        var result = ReadingTimeEstimator.EstimateMinutes(ProgramContentType.Lesson, LessonContentFormat.Lexical, body, jsonBody);

        result.Should().Be(1);
    }

    [Fact]
    public void Estimate_AllowlistIsCaseInsensitive()
    {
        var jsonBody = """{"Question":"a b","TITLE":"c d"}""";

        var result = ReadingTimeEstimator.EstimateMinutes(ProgramContentType.Questionnaire, null, null, jsonBody);

        result.Should().Be(1);
    }
}
