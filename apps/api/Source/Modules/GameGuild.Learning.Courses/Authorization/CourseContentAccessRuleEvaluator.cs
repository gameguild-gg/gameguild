using System.Security.Claims;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Utilities;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Learning.Courses;

public sealed class CourseContentAccessRuleEvaluator(
    IProgramCrudService programService,
    IActorContextAccessor actorContextAccessor,
    IAuthorizationSinglePermissionChecker permissionChecker) : IRuleEvaluator
{
    private const string PublicOutlineAccess = "PublicOutline";
    private const string LearnerAccess = "Learner";
    private const string ManageAccess = "Manage";

    private static readonly PermissionType[] ManagementPermissions =
    [
        PermissionType.Read,
        PermissionType.Edit,
        PermissionType.Create,
        PermissionType.Delete
    ];

    public string RuleType => RuleTypes.CourseContentAccess;

    public async Task<RuleEvaluationResult> EvaluateAsync(
        AuthorizationHandlerContext context,
        RuleParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (context.Resource is not Program program)
        {
            return RuleEvaluationResult.Fail("Course content access requires a Program resource");
        }

        return parameters.GetString("access") switch
        {
            PublicOutlineAccess => EvaluatePublicOutline(program),
            LearnerAccess => await EvaluateLearnerAsync(context.User, program).ConfigureAwait(false),
            ManageAccess => await EvaluateManagementAsync(
                context.User,
                program,
                parameters.GetBool("allowCreator"),
                cancellationToken).ConfigureAwait(false),
            _ => RuleEvaluationResult.Fail("Unknown course content access mode")
        };
    }

    private static RuleEvaluationResult EvaluatePublicOutline(Program program)
    {
        return program.Status == ContentStatus.Published
               && program.Visibility == ContentVisibility.Public
            ? RuleEvaluationResult.Success()
            : RuleEvaluationResult.Fail("Course is not published for public access");
    }

    private async Task<RuleEvaluationResult> EvaluateLearnerAsync(
        ClaimsPrincipal user,
        Program program)
    {
        var userId = GetCurrentUserId(user);
        if (!(user.Identity?.IsAuthenticated ?? false) || userId is not Guid authenticatedUserId)
        {
            return RuleEvaluationResult.Fail("Authenticated learner identity is required");
        }

        var progress = await programService
            .GetUserProgressDtoAsync(program.Id, authenticatedUserId)
            .ConfigureAwait(false);

        return progress is not null
            ? RuleEvaluationResult.Success()
            : RuleEvaluationResult.Fail("User is not enrolled in the course");
    }

    private async Task<RuleEvaluationResult> EvaluateManagementAsync(
        ClaimsPrincipal user,
        Program program,
        bool allowCreator,
        CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        if (actor.IsSystemAdmin)
        {
            return RuleEvaluationResult.Success();
        }

        var userId = GetCurrentUserId(user);
        if (!(user.Identity?.IsAuthenticated ?? false) || userId is not Guid authenticatedUserId)
        {
            return RuleEvaluationResult.Fail("Authenticated manager identity is required");
        }

        if (allowCreator && program.CreatorId == authenticatedUserId)
        {
            return RuleEvaluationResult.Success();
        }

        var tenantId = ClaimsExtractor.GetTenantIdAsGuid(user) ?? actor.TenantId;
        if (tenantId is not Guid currentTenantId)
        {
            return RuleEvaluationResult.Fail("Tenant context is required for course management permissions");
        }

        foreach (var permission in ManagementPermissions)
        {
            var permissionName = $"{nameof(Program)}.{program.Id}.{permission}";
            if (await permissionChecker.HasPermissionAsync(
                    authenticatedUserId,
                    currentTenantId,
                    permissionName,
                    cancellationToken).ConfigureAwait(false))
            {
                return RuleEvaluationResult.Success();
            }
        }

        return RuleEvaluationResult.Fail("No course management permission was granted");
    }

    private static Guid? GetCurrentUserId(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
    }
}
