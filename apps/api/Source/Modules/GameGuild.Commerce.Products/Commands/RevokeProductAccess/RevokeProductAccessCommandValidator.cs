using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for RevokeProductAccessCommand
/// </summary>
public sealed class RevokeProductAccessCommandValidator : AbstractValidator<RevokeProductAccessCommand>
{
    public RevokeProductAccessCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");
    }
}
