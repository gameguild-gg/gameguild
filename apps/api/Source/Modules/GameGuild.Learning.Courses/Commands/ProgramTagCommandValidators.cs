using FluentValidation;

namespace GameGuild.Learning.Courses;

public class AddTagToProgramCommandValidator : AbstractValidator<AddTagToProgramCommand>
{
    public AddTagToProgramCommandValidator()
    {
        RuleFor(x => x.ProgramId)
            .NotEmpty()
            .WithMessage("ProgramId is required");

        RuleFor(x => x.TagId)
            .NotEmpty()
            .WithMessage("TagId is required");

        RuleFor(x => x.ProficiencyLevel)
            .IsInEnum()
            .WithMessage("Invalid proficiency level");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Display order must be non-negative");
    }
}

public class UpdateProgramTagCommandValidator : AbstractValidator<UpdateProgramTagCommand>
{
    public UpdateProgramTagCommandValidator()
    {
        RuleFor(x => x.ProgramId)
            .NotEmpty()
            .WithMessage("ProgramId is required");

        RuleFor(x => x.TagId)
            .NotEmpty()
            .WithMessage("TagId is required");

        RuleFor(x => x.ProficiencyLevel)
            .IsInEnum()
            .When(x => x.ProficiencyLevel.HasValue)
            .WithMessage("Invalid proficiency level");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .When(x => x.DisplayOrder.HasValue)
            .WithMessage("Display order must be non-negative");
    }
}

public class RemoveTagFromProgramCommandValidator : AbstractValidator<RemoveTagFromProgramCommand>
{
    public RemoveTagFromProgramCommandValidator()
    {
        RuleFor(x => x.ProgramId)
            .NotEmpty()
            .WithMessage("ProgramId is required");

        RuleFor(x => x.TagId)
            .NotEmpty()
            .WithMessage("TagId is required");
    }
}

public class BulkAddTagsToProgramCommandValidator : AbstractValidator<BulkAddTagsToProgramCommand>
{
    public BulkAddTagsToProgramCommandValidator()
    {
        RuleFor(x => x.ProgramId)
            .NotEmpty()
            .WithMessage("ProgramId is required");

        RuleFor(x => x.Tags)
            .NotEmpty()
            .WithMessage("At least one tag is required");

        RuleForEach(x => x.Tags)
            .ChildRules(tag =>
            {
                tag.RuleFor(t => t.TagId)
                    .NotEmpty()
                    .WithMessage("TagId is required");
            });
    }
}
