using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for CreateOrderCommand
/// </summary>
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("Idempotency key is required for duplicate prevention.")
            .MinimumLength(8)
            .WithMessage("Idempotency key must be at least 8 characters.")
            .MaximumLength(100)
            .WithMessage("Idempotency key cannot exceed 100 characters.")
            .Matches(@"^[A-Za-z0-9_\-]+$")
            .WithMessage("Idempotency key can only contain letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Length(3)
            .WithMessage("Currency must be a 3-letter ISO 4217 code.")
            .Matches(@"^[A-Z]{3}$")
            .WithMessage("Currency must be uppercase letters only (e.g., USD, EUR, BRL).");
    }
}
