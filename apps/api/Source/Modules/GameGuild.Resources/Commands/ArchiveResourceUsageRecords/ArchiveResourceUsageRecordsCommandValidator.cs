using FluentValidation;

namespace GameGuild.Resources;

public sealed class ArchiveResourceUsageRecordsCommandValidator : AbstractValidator<ArchiveResourceUsageRecordsCommand>
{
    public ArchiveResourceUsageRecordsCommandValidator()
    {
        RuleFor(x => x.OlderThan)
            .NotEmpty()
            .WithMessage("Archive date is required")
            .LessThan(SystemClock.UtcNow)
            .WithMessage("Archive date must be in the past")
            .GreaterThan(SystemClock.UtcNow.AddYears(-10))
            .WithMessage("Archive date cannot be more than 10 years ago");
    }
}
