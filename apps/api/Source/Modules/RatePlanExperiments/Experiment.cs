using System;

namespace GameGuild.Source.Modules.RatePlanExperiments
{
    public class Experiment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] Variants { get; set; } = Array.Empty<string>();
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
