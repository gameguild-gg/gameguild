using FluentValidation;

namespace GameGuild.Learning.Experience.Recommendations;

public sealed class CreateOrUpdateLearningProfileCommandValidator : AbstractValidator<CreateOrUpdateLearningProfileCommand>
{
    public CreateOrUpdateLearningProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");

        RuleFor(x => x.PreferredCategories)
            .Must(c => c == null || c.Length <= 10)
            .WithMessage("Maximum 10 preferred categories allowed");

        RuleFor(x => x.PreferredDifficulty)
            .MaximumLength(50)
            .When(x => x.PreferredDifficulty != null);

        RuleFor(x => x.PreferredDuration)
            .Must(d => d == null || new[] { "short", "medium", "long" }.Contains(d.ToLowerInvariant()))
            .WithMessage("PreferredDuration must be 'short', 'medium', or 'long'");

        RuleFor(x => x.LearningGoals)
            .Must(g => g == null || g.Length <= 20)
            .WithMessage("Maximum 20 learning goals allowed");

        RuleFor(x => x.Skills)
            .Must(s => s == null || s.Length <= 50)
            .WithMessage("Maximum 50 skills allowed");
    }
}

public sealed class AddSkillToProfileCommandValidator : AbstractValidator<AddSkillToProfileCommand>
{
    public AddSkillToProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");

        RuleFor(x => x.Skill)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Skill must be between 1 and 100 characters");
    }
}

public sealed class RemoveSkillFromProfileCommandValidator : AbstractValidator<RemoveSkillFromProfileCommand>
{
    public RemoveSkillFromProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");

        RuleFor(x => x.Skill)
            .NotEmpty()
            .WithMessage("Skill is required");
    }
}

public sealed class GenerateRecommendationsCommandValidator : AbstractValidator<GenerateRecommendationsCommand>
{
    public GenerateRecommendationsCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");

        RuleFor(x => x.MaxResults)
            .InclusiveBetween(1, 50)
            .WithMessage("MaxResults must be between 1 and 50");
    }
}

public sealed class MarkRecommendationViewedCommandValidator : AbstractValidator<MarkRecommendationViewedCommand>
{
    public MarkRecommendationViewedCommandValidator()
    {
        RuleFor(x => x.RecommendationId)
            .NotEmpty()
            .WithMessage("RecommendationId is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");
    }
}

public sealed class DismissRecommendationCommandValidator : AbstractValidator<DismissRecommendationCommand>
{
    public DismissRecommendationCommandValidator()
    {
        RuleFor(x => x.RecommendationId)
            .NotEmpty()
            .WithMessage("RecommendationId is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");
    }
}
