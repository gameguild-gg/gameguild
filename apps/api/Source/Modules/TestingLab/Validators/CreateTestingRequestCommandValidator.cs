using FluentValidation;

namespace GameGuild.Modules.TestingLab.Validators;

public class CreateTestingRequestCommandValidator : AbstractValidator<CreateTestingRequestCommand>
{
    public CreateTestingRequestCommandValidator()
    {
        RuleFor(x => x.ProjectVersionId)
            .NotEmpty()
            .WithMessage("Project Version ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage("Title is required and must be less than 255 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must be less than 2000 characters.");

        RuleFor(x => x.DownloadUrl)
            .Must(BeValidUrl)
            .When(x => !string.IsNullOrEmpty(x.DownloadUrl))
            .WithMessage("Download URL must be a valid URL.");

        RuleFor(x => x.MaxTesters)
            .GreaterThan(0)
            .When(x => x.MaxTesters.HasValue)
            .WithMessage("Max testers must be greater than 0.");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Start date must be in the future.");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.");
    }

    private static bool BeValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}
