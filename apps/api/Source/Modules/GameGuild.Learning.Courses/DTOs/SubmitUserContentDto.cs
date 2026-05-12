using System.ComponentModel.DataAnnotations;

namespace GameGuild.Learning.Courses;

public sealed record SubmitUserContentDto
{
    [Required]
    [MinLength(1)]
    public string SubmissionData { get; init; } = string.Empty;
}