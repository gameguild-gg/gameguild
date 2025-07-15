using GameGuild.Common;
using GameGuild.Modules.Programs;
using GameGuild.Modules.Programs.Models;
using HotChocolate;

namespace GameGuild.Modules.Programs.GraphQL;

/// <summary>
/// GraphQL mutations for Program Enrollment operations
/// </summary>
[ExtendObjectType<Mutation>]
public class ProgramEnrollmentMutations
{
    /// <summary>
    /// Enroll user in a program
    /// </summary>
    public async Task<ProgramEnrollmentResult> EnrollInProgram(
        Guid programId,
        [Service] IProgramEnrollmentService enrollmentService)
    {
        try
        {
            // TODO: Implement proper current user service
            var currentUserId = GetCurrentUserId();

            var enrollment = await enrollmentService.EnrollUserAsync(currentUserId, programId, EnrollmentSource.Manual);

            return new ProgramEnrollmentResult
            {
                Success = true,
                ErrorMessage = null,
                Enrollment = enrollment
            };
        }
        catch (Exception ex)
        {
            return new ProgramEnrollmentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Enrollment = null
            };
        }
    }

    /// <summary>
    /// Auto-enroll user in programs associated with a product purchase
    /// </summary>
    public async Task<AutoEnrollmentResult> AutoEnrollFromProductPurchase(
        Guid productId,
        [Service] IProgramEnrollmentService enrollmentService)
    {
        try
        {
            // TODO: Implement proper current user service
            var currentUserId = GetCurrentUserId();

            var enrollments = await enrollmentService.AutoEnrollInProductProgramsAsync(currentUserId, productId);

            return new AutoEnrollmentResult
            {
                Success = true,
                ErrorMessage = null,
                Enrollments = enrollments.ToList()
            };
        }
        catch (Exception ex)
        {
            return new AutoEnrollmentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Enrollments = new List<ProgramEnrollment>()
            };
        }
    }
    
    // Helper method to get current user ID (temporary implementation)
    private static Guid GetCurrentUserId()
    {
        // TODO: Implement proper current user service with JWT token extraction
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }
}
        try
        {
            // TODO: Implement proper current user service
            var currentUserId = GetCurrentUserId();

            var enrollment = await enrollmentService.EnrollUserAsync(currentUserId, programId, EnrollmentSource.Manual);

            return new ProgramEnrollmentResult
            {
                Success = true,
                ErrorMessage = null,
                Enrollment = enrollment
            };
        }
        catch (Exception ex)
        {
            return new ProgramEnrollmentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Enrollment = null
            };
        }
    }

    /// <summary>
    /// Auto-enroll user in all programs from a product purchase
    /// </summary>
    public async Task<ProductEnrollmentResult> EnrollInProductPrograms(
        Guid productId,
        [Service] IProgramEnrollmentService enrollmentService,
        [Service] ICurrentUserService currentUserService)
    {
        try
        {
            var currentUser = await currentUserService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return new ProductEnrollmentResult
                {
                    Success = false,
                    ErrorMessage = "User not authenticated",
                    Enrollments = new List<ProgramEnrollment>()
                };
            }

            var enrollments = await enrollmentService.AutoEnrollInProductProgramsAsync(currentUser.Id, productId);

            return new ProductEnrollmentResult
            {
                Success = true,
                ErrorMessage = null,
                Enrollments = enrollments
            };
        }
        catch (Exception ex)
        {
            return new ProductEnrollmentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Enrollments = new List<ProgramEnrollment>()
            };
        }
    }

    /// <summary>
    /// Cancel program enrollment
    /// </summary>
    public async Task<BooleanResult> CancelEnrollment(
        Guid enrollmentId,
        [Service] IProgramEnrollmentService enrollmentService)
    {
        try
        {
            var success = await enrollmentService.CancelEnrollmentAsync(enrollmentId);
            return new BooleanResult
            {
                Success = success,
                ErrorMessage = success ? null : "Failed to cancel enrollment"
            };
        }
        catch (Exception ex)
        {
            return new BooleanResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    
    // Helper method to get current user ID (temporary implementation)
    private static Guid GetCurrentUserId()
    {
        // TODO: Implement proper current user service with JWT token extraction
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }
}

/// <summary>
/// Result type for program enrollment operations
/// </summary>
public class ProgramEnrollmentResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ProgramEnrollment? Enrollment { get; set; }
}

/// <summary>
/// Result type for product enrollment operations
/// </summary>
public class ProductEnrollmentResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<ProgramEnrollment> Enrollments { get; set; } = new List<ProgramEnrollment>();
}

/// <summary>
/// Generic boolean result type
/// </summary>
public class BooleanResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
