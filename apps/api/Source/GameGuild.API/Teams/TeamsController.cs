using System.Security.Cryptography;
using Asp.Versioning;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using GameGuild.Teams;
using GameGuild.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.API.Teams;

public sealed record TeamMemberDto(
    Guid UserId,
    TeamMemberAuthority Authority,
    string? ProfessionalTitle,
    bool IsActive,
    DateTime JoinedAt);

public sealed record TeamDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Slug,
    string? Description,
    TeamVisibility Visibility,
    TeamStatus Status,
    bool IsPersonal,
    IReadOnlyList<TeamMemberDto> Members);

public sealed record CreateTeamRequest(
    string Name,
    string Slug,
    TeamVisibility Visibility,
    string? Description);

public sealed record UpdateTeamRequest(
    string Name,
    string Slug,
    TeamVisibility Visibility,
    string? Description);

public sealed record AddTeamMemberRequest(
    Guid UserId,
    TeamMemberAuthority Authority,
    string? ProfessionalTitle);

public sealed record ChangeTeamMemberRequest(
    TeamMemberAuthority Authority,
    string? ProfessionalTitle);

public sealed record CreateTeamInvitationRequest(
    Guid? UserId,
    string? Email,
    TeamMemberAuthority Authority,
    DateTime ExpiresAt);

public sealed record TeamInvitationCreatedDto(Guid Id, string Token, DateTime ExpiresAt);
public sealed record MyTeamInvitationDto(
    Guid Id,
    Guid TeamId,
    string TeamName,
    string TeamSlug,
    TeamMemberAuthority Authority,
    DateTime ExpiresAt);
public sealed record TeamInvitationDto(
    Guid Id,
    Guid? InvitedUserId,
    string? InvitedEmail,
    TeamMemberAuthority Authority,
    Guid InvitedByUserId,
    DateTime ExpiresAt,
    DateTime? RevokedAt,
    DateTime? UsedAt);
public sealed record AcceptTeamInvitationRequest(string Token);

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("v{version:apiVersion}/teams")]
public sealed class TeamsController(
    IApplicationDbContext context,
    IActorContextAccessor actorContextAccessor,
    ITeamAuthorizationService authorization,
    IResourceQuotaEnforcer quotaEnforcer) : ControllerBase
{
    private static readonly TimeSpan RecentAuthenticationWindow = TimeSpan.FromMinutes(15);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TeamDto>>> List(CancellationToken cancellationToken)
    {
        var teams = await authorization.ApplyMembershipAccess(context.Set<Team>().AsNoTracking())
            .Include(team => team.Members.Where(member => member.IsActive && member.DeletedAt == null))
            .OrderByDescending(team => team.UpdatedAt)
            .ThenBy(team => team.Name)
            .Take(100)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Ok(teams.Select(Map).ToArray());
    }

    [HttpGet("{teamId:guid}")]
    public async Task<ActionResult<TeamDto>> Get(Guid teamId, CancellationToken cancellationToken)
    {
        if (!await authorization.HasAuthorityAsync(teamId, TeamMemberAuthority.Viewer, cancellationToken).ConfigureAwait(false))
            return NotFound();
        var team = await LoadTeamAsync(teamId, cancellationToken).ConfigureAwait(false);
        return team == null ? NotFound() : Ok(Map(team));
    }

    [HttpPost]
    public async Task<ActionResult<TeamDto>> Create(CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        if (!await authorization.CanCreateAsync(cancellationToken).ConfigureAwait(false) ||
            actor.SubjectIdAsGuid is not { } actorId || actor.TenantId is not { } tenantId)
            return Forbid();
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug))
            return ValidationProblem("Name and slug are required.");

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        if (await context.Set<Team>().AnyAsync(team =>
                team.TenantId == tenantId && team.Slug == normalizedSlug && team.DeletedAt == null,
                cancellationToken).ConfigureAwait(false))
            return Conflict(new ProblemDetails { Title = "Team slug already exists.", Status = StatusCodes.Status409Conflict });

        var (quotaAllowed, currentUsage, hardLimit) = await quotaEnforcer.TryAtomicConsumeAsync(
            tenantId, ResourceUsageType.Teams, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!quotaAllowed)
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                code = "Teams.QuotaExceeded",
                currentUsage,
                hardLimit,
            });

        Team team;
        try
        {
            team = Team.Create(tenantId, request.Name, normalizedSlug, actorId);
            team.Visibility = request.Visibility;
            team.Description = request.Description?.Trim();
            context.Set<Team>().Add(team);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await quotaEnforcer.DecrementUsageAsync(
                tenantId, ResourceUsageType.Teams, cancellationToken: cancellationToken).ConfigureAwait(false);
            throw;
        }

        return CreatedAtAction(nameof(Get), new { teamId = team.Id, version = "1" }, Map(team));
    }

    [HttpPut("{teamId:guid}")]
    public async Task<ActionResult<TeamDto>> Update(
        Guid teamId,
        UpdateTeamRequest request,
        CancellationToken cancellationToken)
    {
        if (!await authorization.HasAuthorityAsync(teamId, TeamMemberAuthority.Manager, cancellationToken).ConfigureAwait(false))
            return Forbid();
        var team = await LoadTeamAsync(teamId, cancellationToken).ConfigureAwait(false);
        if (team == null) return NotFound();

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        if (await context.Set<Team>().AnyAsync(candidate =>
                candidate.Id != team.Id &&
                candidate.TenantId == team.TenantId &&
                candidate.Slug == normalizedSlug &&
                candidate.DeletedAt == null,
                cancellationToken).ConfigureAwait(false))
            return Conflict(new ProblemDetails { Title = "Team slug already exists.", Status = StatusCodes.Status409Conflict });

        team.Name = request.Name.Trim();
        team.Slug = normalizedSlug;
        team.Description = request.Description?.Trim();
        team.Visibility = request.Visibility;
        team.Touch();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(Map(team));
    }

    [HttpDelete("{teamId:guid}")]
    public async Task<IActionResult> Archive(Guid teamId, CancellationToken cancellationToken)
    {
        if (!await authorization.HasAuthorityAsync(teamId, TeamMemberAuthority.Owner, cancellationToken).ConfigureAwait(false))
            return Forbid();
        var team = await LoadTeamAsync(teamId, cancellationToken).ConfigureAwait(false);
        if (team == null) return NotFound();
        team.Archive();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{teamId:guid}/members")]
    public async Task<ActionResult<TeamMemberDto>> AddMember(
        Guid teamId,
        AddTeamMemberRequest request,
        CancellationToken cancellationToken)
    {
        var required = request.Authority == TeamMemberAuthority.Owner
            ? TeamMemberAuthority.Owner
            : TeamMemberAuthority.Manager;
        if (!await authorization.HasAuthorityAsync(teamId, required, cancellationToken).ConfigureAwait(false))
            return Forbid();
        var team = await LoadTeamAsync(teamId, cancellationToken).ConfigureAwait(false);
        if (team == null) return NotFound();
        if (!await IsActiveTenantMemberAsync(request.UserId, team.TenantId!.Value, cancellationToken).ConfigureAwait(false))
            return UnprocessableEntity(new { code = "Teams.ActiveTenantMembershipRequired" });
        if (request.Authority == TeamMemberAuthority.Owner &&
            !await HasSensitiveActionAssuranceAsync(cancellationToken).ConfigureAwait(false))
            return Forbid();
        var existing = team.Members.SingleOrDefault(member => member.UserId == request.UserId && member.DeletedAt == null);
        var member = team.AddMember(request.UserId, request.Authority, request.ProfessionalTitle);
        if (existing == null) context.Set<TeamMember>().Add(member);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(Map(member));
    }

    [HttpPut("{teamId:guid}/members/{userId:guid}")]
    public async Task<ActionResult<TeamMemberDto>> ChangeMember(
        Guid teamId,
        Guid userId,
        ChangeTeamMemberRequest request,
        CancellationToken cancellationToken)
    {
        var required = request.Authority == TeamMemberAuthority.Owner
            ? TeamMemberAuthority.Owner
            : TeamMemberAuthority.Manager;
        if (!await authorization.HasAuthorityAsync(teamId, required, cancellationToken).ConfigureAwait(false))
            return Forbid();
        var team = await LoadTeamAsync(teamId, cancellationToken).ConfigureAwait(false);
        if (team == null) return NotFound();
        if (request.Authority == TeamMemberAuthority.Owner &&
            !await HasSensitiveActionAssuranceAsync(cancellationToken).ConfigureAwait(false))
            return Forbid();
        try { team.ChangeAuthority(userId, request.Authority); }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Title = exception.Message, Status = StatusCodes.Status409Conflict });
        }
        var member = team.Members.Single(candidate => candidate.UserId == userId && candidate.IsActive);
        member.ProfessionalTitle = request.ProfessionalTitle?.Trim();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(Map(member));
    }

    [HttpDelete("{teamId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid teamId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await authorization.HasAuthorityAsync(teamId, TeamMemberAuthority.Manager, cancellationToken).ConfigureAwait(false))
            return Forbid();
        var team = await LoadTeamAsync(teamId, cancellationToken).ConfigureAwait(false);
        if (team == null) return NotFound();
        try { team.RemoveMember(userId); }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Title = exception.Message, Status = StatusCodes.Status409Conflict });
        }
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{teamId:guid}/invitations")]
    public async Task<ActionResult<TeamInvitationCreatedDto>> CreateInvitation(
        Guid teamId,
        CreateTeamInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var required = request.Authority == TeamMemberAuthority.Owner
            ? TeamMemberAuthority.Owner
            : TeamMemberAuthority.Manager;
        if (!await authorization.HasAuthorityAsync(teamId, required, cancellationToken).ConfigureAwait(false))
            return Forbid();
        if (request.Authority == TeamMemberAuthority.Owner &&
            !await HasSensitiveActionAssuranceAsync(cancellationToken).ConfigureAwait(false))
            return Forbid();
        var actor = actorContextAccessor.ActorContext;
        var team = await context.Set<Team>().SingleOrDefaultAsync(candidate => candidate.Id == teamId, cancellationToken).ConfigureAwait(false);
        if (team?.TenantId == null || actor.SubjectIdAsGuid is not { } actorId) return NotFound();
        if (request.ExpiresAt <= SystemClock.UtcNow) return ValidationProblem("Invitation expiry must be in the future.");

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var invitation = TeamInvitation.Create(
            team.TenantId.Value,
            team.Id,
            actorId,
            request.Email,
            request.Authority,
            token,
            request.ExpiresAt,
            request.UserId);
        context.Set<TeamInvitation>().Add(invitation);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new TeamInvitationCreatedDto(invitation.Id, token, invitation.ExpiresAt));
    }

    [HttpGet("{teamId:guid}/invitations")]
    public async Task<ActionResult<IReadOnlyList<TeamInvitationDto>>> ListInvitations(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        if (!await authorization.HasAuthorityAsync(teamId, TeamMemberAuthority.Manager, cancellationToken).ConfigureAwait(false))
            return Forbid();
        var actorTenantId = actorContextAccessor.ActorContext.TenantId;
        if (!actorTenantId.HasValue) return Unauthorized();
        var teamExists = await context.Set<Team>().AsNoTracking().AnyAsync(team =>
            team.Id == teamId && team.TenantId == actorTenantId && team.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
        if (!teamExists) return NotFound();

        var invitations = await context.Set<TeamInvitation>().AsNoTracking()
            .Where(invitation => invitation.TeamId == teamId && invitation.TenantId == actorTenantId && invitation.DeletedAt == null)
            .OrderByDescending(invitation => invitation.CreatedAt)
            .Select(invitation => new TeamInvitationDto(
                invitation.Id,
                invitation.InvitedUserId,
                invitation.InvitedEmail,
                invitation.Authority,
                invitation.InvitedByUserId,
                invitation.ExpiresAt,
                invitation.RevokedAt,
                invitation.UsedAt))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(invitations);
    }

    [HttpGet("my-invitations")]
    public async Task<ActionResult<IReadOnlyList<MyTeamInvitationDto>>> ListMyInvitations(
        CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        if (actor.SubjectIdAsGuid is not { } actorId || actor.TenantId is not { } tenantId)
            return Unauthorized();
        if (!await IsActiveTenantMemberAsync(actorId, tenantId, cancellationToken).ConfigureAwait(false))
            return Forbid();
        var actorEmail = await context.Set<User>().AsNoTracking()
            .Where(user => user.Id == actorId && user.IsActive && user.DeletedAt == null)
            .Select(user => user.Email.ToLower())
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var now = SystemClock.UtcNow;
        var invitations = await context.Set<TeamInvitation>().AsNoTracking()
            .Where(invitation =>
                invitation.TenantId == tenantId && invitation.DeletedAt == null &&
                invitation.UsedAt == null && invitation.RevokedAt == null && invitation.ExpiresAt > now &&
                invitation.Team != null && invitation.Team.IsActive && invitation.Team.DeletedAt == null &&
                (invitation.InvitedUserId == actorId ||
                 (invitation.InvitedUserId == null && actorEmail != null && invitation.InvitedEmail == actorEmail)))
            .OrderBy(invitation => invitation.ExpiresAt)
            .Select(invitation => new MyTeamInvitationDto(
                invitation.Id,
                invitation.TeamId,
                invitation.Team!.Name,
                invitation.Team.Slug,
                invitation.Authority,
                invitation.ExpiresAt))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(invitations);
    }

    [HttpDelete("{teamId:guid}/invitations/{invitationId:guid}")]
    public async Task<IActionResult> RevokeInvitation(
        Guid teamId,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        if (!await authorization.HasAuthorityAsync(teamId, TeamMemberAuthority.Manager, cancellationToken).ConfigureAwait(false))
            return Forbid();
        var actorTenantId = actorContextAccessor.ActorContext.TenantId;
        if (!actorTenantId.HasValue) return Unauthorized();
        var invitation = await context.Set<TeamInvitation>().SingleOrDefaultAsync(candidate =>
            candidate.Id == invitationId && candidate.TeamId == teamId && candidate.TenantId == actorTenantId &&
            candidate.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
        if (invitation == null) return NotFound();
        if (invitation.UsedAt.HasValue)
            return Conflict(new ProblemDetails { Title = "An accepted invitation cannot be revoked.", Status = StatusCodes.Status409Conflict });
        invitation.Revoke(SystemClock.UtcNow);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("invitations/accept")]
    public async Task<ActionResult<TeamDto>> AcceptInvitation(
        AcceptTeamInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        if (actor.SubjectIdAsGuid is not { } actorId || actor.TenantId is not { } tenantId)
            return Unauthorized();
        var hash = TeamInvitation.HashToken(request.Token);
        var invitation = await context.Set<TeamInvitation>()
            .Include(candidate => candidate.Team)!.ThenInclude(team => team!.Members)
            .SingleOrDefaultAsync(candidate => candidate.TokenHash == hash && candidate.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);
        if (invitation?.Team == null || invitation.TenantId != tenantId)
            return NotFound();
        if (!await IsActiveTenantMemberAsync(actorId, tenantId, cancellationToken).ConfigureAwait(false))
            return Forbid();
        if (invitation.InvitedUserId.HasValue && invitation.InvitedUserId != actorId)
            return Forbid();
        if (!invitation.InvitedUserId.HasValue && !string.IsNullOrWhiteSpace(invitation.InvitedEmail))
        {
            var actorEmail = await context.Set<User>().AsNoTracking()
                .Where(user => user.Id == actorId && user.IsActive && user.DeletedAt == null)
                .Select(user => user.Email)
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (!string.Equals(actorEmail?.Trim(), invitation.InvitedEmail.Trim(), StringComparison.OrdinalIgnoreCase))
                return Forbid();
        }
        if (invitation.Authority == TeamMemberAuthority.Owner &&
            !await HasSensitiveActionAssuranceAsync(cancellationToken).ConfigureAwait(false))
            return Forbid();
        if (!invitation.Accept(request.Token, actorId, SystemClock.UtcNow))
            return Conflict(new ProblemDetails { Title = "Invitation is expired, revoked, or already used.", Status = StatusCodes.Status409Conflict });
        var existingMember = invitation.Team.Members.SingleOrDefault(member => member.UserId == actorId && member.DeletedAt == null);
        var acceptedMember = invitation.Team.AddMember(actorId, invitation.Authority);
        if (existingMember == null) context.Set<TeamMember>().Add(acceptedMember);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(Map(invitation.Team));
    }

    [HttpPost("invitations/{invitationId:guid}:accept")]
    public async Task<ActionResult<TeamDto>> AcceptAuthenticatedInvitation(
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        if (actor.SubjectIdAsGuid is not { } actorId || actor.TenantId is not { } tenantId)
            return Unauthorized();
        if (!await IsActiveTenantMemberAsync(actorId, tenantId, cancellationToken).ConfigureAwait(false))
            return Forbid();
        var actorEmail = await context.Set<User>().AsNoTracking()
            .Where(user => user.Id == actorId && user.IsActive && user.DeletedAt == null)
            .Select(user => user.Email)
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var invitation = await context.Set<TeamInvitation>()
            .Include(candidate => candidate.Team)!.ThenInclude(team => team!.Members)
            .SingleOrDefaultAsync(candidate =>
                candidate.Id == invitationId && candidate.TenantId == tenantId && candidate.DeletedAt == null,
                cancellationToken).ConfigureAwait(false);
        if (invitation?.Team == null || !invitation.Team.IsActive || invitation.Team.DeletedAt != null)
            return NotFound();
        if (invitation.InvitedUserId.HasValue && invitation.InvitedUserId != actorId)
            return Forbid();
        if (!invitation.InvitedUserId.HasValue &&
            !string.Equals(actorEmail?.Trim(), invitation.InvitedEmail?.Trim(), StringComparison.OrdinalIgnoreCase))
            return Forbid();
        if (invitation.Authority == TeamMemberAuthority.Owner &&
            !await HasSensitiveActionAssuranceAsync(cancellationToken).ConfigureAwait(false))
            return Forbid();
        if (!invitation.AcceptAuthenticated(actorId, SystemClock.UtcNow))
            return Conflict(new ProblemDetails { Title = "Invitation is expired, revoked, or already used.", Status = StatusCodes.Status409Conflict });
        var existingMember = invitation.Team.Members.SingleOrDefault(member => member.UserId == actorId && member.DeletedAt == null);
        var acceptedMember = invitation.Team.AddMember(actorId, invitation.Authority);
        if (existingMember == null) context.Set<TeamMember>().Add(acceptedMember);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(Map(invitation.Team));
    }

    private Task<Team?> LoadTeamAsync(Guid teamId, CancellationToken cancellationToken) =>
        context.Set<Team>().Include(team => team.Members)
            .SingleOrDefaultAsync(team => team.Id == teamId && team.DeletedAt == null, cancellationToken);

    private Task<bool> IsActiveTenantMemberAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken) =>
        context.Set<TenantMember>().AsNoTracking().AnyAsync(member =>
            member.UserId == userId && member.TenantId == tenantId && member.IsActive && member.DeletedAt == null,
            cancellationToken);

    private async Task<bool> HasSensitiveActionAssuranceAsync(CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        if (actor.TypedAttributes.AuthenticatedAt is not { } authenticatedAt ||
            authenticatedAt < DateTimeOffset.UtcNow.Subtract(RecentAuthenticationWindow) ||
            actor.SubjectIdAsGuid is not { } actorId)
            return false;
        var hasMfa = await context.Set<UserMfaConfiguration>().AsNoTracking().AnyAsync(configuration =>
            configuration.UserId == actorId && configuration.IsEnabled && configuration.IsSetupComplete,
            cancellationToken).ConfigureAwait(false);
        return !hasMfa || actor.IsMfaVerified;
    }

    private static TeamDto Map(Team team) => new(
        team.Id,
        team.TenantId!.Value,
        team.Name,
        team.Slug,
        team.Description,
        team.Visibility,
        team.Status,
        team.IsPersonal,
        team.Members.Where(member => member.IsActive && member.DeletedAt == null).Select(Map).ToArray());

    private static TeamMemberDto Map(TeamMember member) => new(
        member.UserId,
        member.Authority,
        member.ProfessionalTitle,
        member.IsActive,
        member.JoinedAt);
}
