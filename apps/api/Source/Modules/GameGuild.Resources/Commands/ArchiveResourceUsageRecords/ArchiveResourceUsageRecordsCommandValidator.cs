using FluentValidation;

namespace GameGuild.Resources;

public sealed class ArchiveResourceUsageRecordsCommandValidator : AbstractValidator<ArchiveResourceUsageRecordsCommand>
{
    public ArchiveResourceUsageRecordsCommandValidator()
    {
        RuleFor(x => x.OlderThan)
            .NotEmpty()
            .WithMessage("Archive date is required")
            .LessThan(DateTime.UtcNow)
            .WithMessage("Archive date must be in the past")
            .GreaterThan(DateTime.UtcNow.AddYears(-10))
            .WithMessage("Archive date cannot be more than 10 years ago");
    }
}
