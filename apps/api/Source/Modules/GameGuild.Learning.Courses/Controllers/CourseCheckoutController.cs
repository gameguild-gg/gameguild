using System.Security.Claims;
using Asp.Versioning;
using GameGuild.Commerce.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Learning.Courses;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}/courses")]
[Authorize]
public sealed class CourseCheckoutController(
    IProgramCrudService programService,
    IProgramEnrollmentService enrollmentService,
    IEntitlementService entitlementService,
    IProductRepository productRepository) : ControllerBase
{
    [HttpPost("{courseId:guid}/checkout/complete")]
    public async Task<ActionResult<CompleteCourseCheckoutResponse>> CompleteCheckout(
        Guid courseId,
        [FromBody] CompleteCourseCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        if (request.ProductId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product required",
                Detail = "A course product must be selected before checkout can complete.",
            });
        }

        var course = await programService.GetProgramByIdAsync(courseId).ConfigureAwait(false);
        if (course == null || course.Status != ContentStatus.Published || course.Visibility != ContentVisibility.Public)
        {
            return NotFound();
        }

        if (!course.IsEnrollmentOpen)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Enrollment closed",
                Detail = "This course is not currently open for checkout enrollment.",
            });
        }

        var linkedProducts = (await programService.GetLinkedProductsAsync(courseId).ConfigureAwait(false)).ToHashSet();
        if (!linkedProducts.Contains(request.ProductId))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product is not linked to course",
                Detail = "The selected product does not grant access to this course.",
            });
        }

        var product = await productRepository.GetByIdAsync(
            request.ProductId,
            cancellationToken,
            includePricing: true,
            isPublished: true).ConfigureAwait(false);

        if (product == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Product not found",
                Detail = "The selected course product is not available for checkout.",
            });
        }

        var pricing = product.Pricing.FirstOrDefault(entry => entry.IsDefault) ?? product.Pricing.FirstOrDefault();
        var amount = pricing?.GetCurrentPrice() ?? 0m;
        var currency = pricing?.Currency ?? "USD";
        var acquisitionType = amount <= 0
            ? ProductAcquisitionType.Free
            : product.Type == ProductType.Subscription
                ? ProductAcquisitionType.Subscription
                : ProductAcquisitionType.Purchase;

        var entitlement = await entitlementService.GrantEntitlementAsync(
            userId.Value,
            request.ProductId,
            acquisitionType,
            amount,
            currency,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!entitlement.Success || entitlement.UserProduct == null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Entitlement could not be granted",
                Detail = entitlement.ErrorMessage ?? "The course product could not be attached to the current learner.",
            });
        }

        var productEnrollments = (await enrollmentService.AutoEnrollInProductProgramsAsync(userId.Value, request.ProductId).ConfigureAwait(false)).ToList();
        var progressRows = new List<UserProgressDto>();

        foreach (var enrollment in productEnrollments)
        {
            var progress = await programService.AddUserToProgramAsync(enrollment.ProgramId, userId.Value).ConfigureAwait(false);
            if (progress != null) progressRows.Add(progress);
        }

        if (progressRows.All(progress => progress.CourseId != courseId))
        {
            var progress = await programService.AddUserToProgramAsync(courseId, userId.Value).ConfigureAwait(false);
            if (progress != null) progressRows.Add(progress);
        }

        return Ok(new CompleteCourseCheckoutResponse(
            courseId,
            request.ProductId,
            entitlement.UserProduct.Id,
            productEnrollments.Select(enrollment => enrollment.Id).ToArray(),
            entitlement.AlreadyHadAccess,
            amount,
            currency,
            course.Slug == null ? $"/courses/{courseId}/content" : $"/courses/{course.Slug}/content",
            request.PaymentProviderReference));
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

public sealed record CompleteCourseCheckoutRequest(
    Guid ProductId,
    string? PaymentProviderReference = null,
    string? PaymentMethod = null);

public sealed record CompleteCourseCheckoutResponse(
    Guid CourseId,
    Guid ProductId,
    Guid EntitlementId,
    IReadOnlyList<Guid> EnrollmentIds,
    bool AlreadyHadAccess,
    decimal Amount,
    string Currency,
    string LearningUrl,
    string? PaymentProviderReference);
