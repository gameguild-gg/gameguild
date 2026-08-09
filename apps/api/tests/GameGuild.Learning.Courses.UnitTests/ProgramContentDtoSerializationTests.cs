using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests;

public sealed class ProgramContentDtoSerializationTests
{
  [Fact]
  public void Serialize_Module_OmitsLessonOnlyAndActivityOnlyNullFields()
  {
    var dto = new ProgramContentDto
    {
      Id = Guid.NewGuid(),
      ProgramId = Guid.NewGuid(),
      Title = "Production foundations",
      Type = ProgramContentType.Module,
      LessonFormat = null,
      ActivitySettings = null
    };

    var json = JsonSerializer.Serialize(dto);

    using var document = JsonDocument.Parse(json);
    document.RootElement.TryGetProperty("LessonFormat", out _).Should().BeFalse();
    document.RootElement.TryGetProperty("ActivitySettings", out _).Should().BeFalse();
  }
  [Fact]
  public void Serialize_LexicalLesson_EmitsStructuredJsonBody()
  {
    using var source = JsonDocument.Parse("""{"root":{"type":"root","children":[]}}""");
    var dto = new ProgramContentDto
    {
      Id = Guid.NewGuid(),
      ProgramId = Guid.NewGuid(),
      Title = "Lexical lesson",
      Type = ProgramContentType.Lesson,
      LessonFormat = LessonContentFormat.Lexical,
      JsonBody = source.RootElement.Clone(),
    };

    using var serialized = JsonDocument.Parse(JsonSerializer.Serialize(dto));

    serialized.RootElement.GetProperty("JsonBody")
      .GetProperty("root")
      .GetProperty("type")
      .GetString()
      .Should()
      .Be("root");
  }

}