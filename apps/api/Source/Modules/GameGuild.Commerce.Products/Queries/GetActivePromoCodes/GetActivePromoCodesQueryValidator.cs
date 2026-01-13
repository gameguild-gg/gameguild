using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for GetActivePromoCodesQuery.
/// No validation rules required - ProductId is optional.
/// </summary>
public class GetActivePromoCodesQueryValidator : AbstractValidator<GetActivePromoCodesQuery>;

