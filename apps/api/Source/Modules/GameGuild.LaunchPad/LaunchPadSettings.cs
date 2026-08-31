using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Projects;

namespace GameGuild.LaunchPad;

[Table("launch_pad_settings")]
public sealed class LaunchPadSettings : EntityBase<Guid>
{
    public VersionSubmissionPolicy VersionSubmissionPolicy { get; set; } = VersionSubmissionPolicy.ReleasedImmutable;
}
