using FluentValidation;

namespace GameGuild.Projects;

/// <summary> Validator for CreateProjectCommand </summary>
public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand> {
  public CreateProjectCommandValidator() {
    RuleFor(x => x.Title)
      .NotEmpty().WithMessage("Project name is required")
      .MaximumLength(200).WithMessage("Project name must not exceed 200 characters");

    RuleFor(x => x.Description)
      .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters")
      .When(x => x.Description != null);
  }
}

/// <summary> Validator for UpdateProjectCommand </summary>
public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand> {
  public UpdateProjectCommandValidator() {
    RuleFor(x => x.ProjectId)
      .NotEmpty().WithMessage("Project ID is required");

    RuleFor(x => x.Title)
      .NotEmpty().WithMessage("Project name is required")
      .MaximumLength(200).WithMessage("Project name must not exceed 200 characters")
      .When(x => x.Title != null);
  }
}

/// <summary> Validator for DeleteProjectCommand </summary>
public class DeleteProjectCommandValidator : AbstractValidator<DeleteProjectCommand> {
  public DeleteProjectCommandValidator() {
    RuleFor(x => x.ProjectId)
      .NotEmpty().WithMessage("Project ID is required");
  }
}

/// <summary> Validator for PublishProjectCommand </summary>
public class PublishProjectCommandValidator : AbstractValidator<PublishProjectCommand> {
  public PublishProjectCommandValidator() {
    RuleFor(x => x.ProjectId)
      .NotEmpty().WithMessage("Project ID is required");
  }
}

/// <summary> Validator for UnpublishProjectCommand </summary>
public class UnpublishProjectCommandValidator : AbstractValidator<UnpublishProjectCommand> {
  public UnpublishProjectCommandValidator() {
    RuleFor(x => x.ProjectId)
      .NotEmpty().WithMessage("Project ID is required");
  }
}

/// <summary> Validator for ArchiveProjectCommand </summary>
public class ArchiveProjectCommandValidator : AbstractValidator<ArchiveProjectCommand> {
  public ArchiveProjectCommandValidator() {
    RuleFor(x => x.ProjectId)
      .NotEmpty().WithMessage("Project ID is required");
  }
}
