using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for GetActivePromoCodesQuery.
/// No validation rules required - ProductId is optional.
/// </summary>
public sealed class GetActivePromoCodesQueryValidator : AbstractValidator<GetActivePromoCodesQuery>;

