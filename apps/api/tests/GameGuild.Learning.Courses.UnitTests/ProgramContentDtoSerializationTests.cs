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
}