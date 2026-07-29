using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests;

public sealed class ContentInteractionDtoSerializationTests
{
  [Fact]
  public void Serialize_InteractionWithoutLoadedNavigation_OmitsNullNestedResources()
  {
    var dto = new ContentInteractionDto
    {
      Id = Guid.NewGuid(),
      ProgramUserId = Guid.NewGuid(),
      ContentId = Guid.NewGuid(),
      Status = ProgressStatus.InProgress,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
      Content = null,
      ProgramUser = null
    };

    var json = JsonSerializer.Serialize(dto);

    using var document = JsonDocument.Parse(json);
    document.RootElement.TryGetProperty("Content", out _).Should().BeFalse();
    document.RootElement.TryGetProperty("ProgramUser", out _).Should().BeFalse();
  }
}