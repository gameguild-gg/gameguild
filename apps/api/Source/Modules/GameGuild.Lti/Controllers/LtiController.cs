using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace GameGuild.Lti;

/// <summary>
/// LTI 1.3 tool endpoints: OIDC third-party login, launch validation, tool JWKS,
/// and admin deployment/line-item management. Launch/login/jwks are unauthenticated
/// per the LTI spec; everything else is system-admin only.
/// </summary>
[Authorize]
public sealed class LtiController(
    IApplicationDbContext context,
    LtiLaunchStateStore stateStore,
    LtiPlatformJwksService jwksService,
    IJwtTokenService jwtTokenService,
    IActorContextAccessor actorContextAccessor,
    ILogger<LtiController> logger) : BaseApiController
{
    private const string SessionCookieName = "gg_session";

    [AllowAnonymous]
    [HttpGet(".well-known/jwks.json")]
    public async Task<IActionResult> Jwks()
    {
        var deployments = await context.Set<LtiDeployment>()
            .Where(d => d.Active && d.DeletedAt == null)
            .ToListAsync()
            .ConfigureAwait(false);

        var keys = new List<object>();
        foreach (var deployment in deployments)
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(deployment.PrivateKeyPem);
                var parameters = rsa.ExportParameters(false);
                keys.Add(new
                {
                    kty = "RSA",
                    use = "sig",
                    alg = "RS256",
                    kid = deployment.KeyId,
                    n = Base64UrlEncoder.Encode(parameters.Modulus!),
                    e = Base64UrlEncoder.Encode(parameters.Exponent!)
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "LTI: deployment {DeploymentId} has an unreadable private key; excluded from JWKS", deployment.Id);
            }
        }

        return Ok(new { keys });
    }

    /// <summary>
    /// OIDC third-party-initiated login. Validates the platform against registered
    /// active deployments, then redirects to the deployment's configured authorization
    /// endpoint with state+nonce. All redirect targets come from admin-configured
    /// deployment records — never from request input.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("lti/login")]
    public async Task<IActionResult> Login()
    {
        if (!Request.HasFormContentType)
        {
            return BadRequest("Login initiation must be a form POST.");
        }

        var form = await Request.ReadFormAsync().ConfigureAwait(false);
        var issuer = form["iss"].ToString();
        var clientId = form["client_id"].ToString();
        var deploymentId = form["deployment_id"].ToString();

        var deployment = await FindActiveDeploymentAsync(issuer, clientId).ConfigureAwait(false);
        if (deployment is null || !string.Equals(deployment.DeploymentId, deploymentId, StringComparison.Ordinal))
        {
            return Unauthorized("Unknown LTI platform.");
        }

        var loginHint = form["login_hint"].ToString();
        if (string.IsNullOrEmpty(loginHint))
        {
            return BadRequest("login_hint is required.");
        }

        var (state, nonce) = stateStore.Issue(deployment.Id);
        var redirectUri = $"{Request.Scheme}://{Request.Host}/lti/launch";
        var query = new Dictionary<string, string?>
        {
            ["scope"] = "openid",
            ["response_type"] = "id_token",
            ["response_mode"] = "form_post",
            ["prompt"] = "none",
            ["client_id"] = deployment.ClientId,
            ["redirect_uri"] = redirectUri,
            ["login_hint"] = loginHint,
            ["state"] = state,
            ["nonce"] = nonce
        };
        var messageHint = form["lti_message_hint"].ToString();
        if (!string.IsNullOrEmpty(messageHint))
        {
            query["lti_message_hint"] = messageHint;
        }

        var separator = deployment.AuthorizationUrl.Contains('?') ? '&' : '?';
        return Redirect(deployment.AuthorizationUrl + separator + QueryString.Create(query).Value);
    }

    /// <summary>
    /// LTI 1.3 launch: the platform form-POSTs the signed id_token here.
    /// id_token in the query string is rejected outright (leaks into logs/history).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("lti/launch")]
    public async Task<IActionResult> Launch()
    {
        if (Request.Query.ContainsKey("id_token"))
        {
            return BadRequest("id_token must be delivered in the POST body.");
        }

        if (!Request.HasFormContentType)
        {
            return BadRequest("Launch must be a form POST.");
        }

        var form = await Request.ReadFormAsync().ConfigureAwait(false);
        var state = form["state"].ToString();
        var idToken = form["id_token"].ToString();
        if (string.IsNullOrEmpty(state) || string.IsNullOrEmpty(idToken))
        {
            return BadRequest("state and id_token are required.");
        }

        // Decode the header unvalidated ONLY to locate the deployment; no claim is
        // trusted until the signature validates against the platform JWKS.
        string issuer;
        string audience;
        try
        {
            var unvalidated = new JwtSecurityTokenHandler().ReadJwtToken(idToken);
            issuer = unvalidated.Issuer;
            audience = unvalidated.Audiences.FirstOrDefault() ?? string.Empty;
        }
        catch (ArgumentException)
        {
            return BadRequest("Malformed id_token.");
        }

        var deployment = await FindActiveDeploymentAsync(issuer, audience).ConfigureAwait(false);
        if (deployment is null)
        {
            return Unauthorized("Unknown LTI platform.");
        }

        var principal = await jwksService.ValidateIdTokenAsync(idToken, deployment).ConfigureAwait(false);
        if (principal is null)
        {
            return Unauthorized("Invalid launch token.");
        }

        var sub = principal.FindFirst("sub")?.Value;
        var nonce = principal.FindFirst("nonce")?.Value;
        if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(nonce))
        {
            return Unauthorized("Launch token is missing required claims.");
        }

        // Single-use: consuming the state also consumes its nonce.
        if (!stateStore.TryConsume(state, nonce, deployment.Id))
        {
            return Unauthorized("Unknown or already used launch state.");
        }

        var user = await ResolveUserAsync(deployment, sub, principal.FindFirst("email")?.Value).ConfigureAwait(false);
        if (user is null)
        {
            // Fail closed — never auto-provision from platform identity.
            return Unauthorized("No gameguild account matches this launch");
        }

        var sessionToken = await jwtTokenService
            .GenerateAccessTokenAsync(user.Id, user.Email, Array.Empty<string>(), user.TenantId)
            .ConfigureAwait(false);

        // SameSite=None is required: the tool runs inside the platform's iframe.
        Response.Cookies.Append(SessionCookieName, sessionToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/"
        });
        return Redirect("/dashboard/tasks");
    }

    // ===== admin: deployment + line-item management =====

    [HttpPost("v1/lti/deployments")]
    public async Task<IActionResult> CreateDeployment([FromBody] CreateLtiDeploymentRequest request)
    {
        if (!actorContextAccessor.ActorContext.IsSystemAdmin)
        {
            return Forbid();
        }

        LtiDeployment deployment;
        try
        {
            deployment = LtiDeployment.Create(
                request.Issuer, request.ClientId, request.DeploymentId,
                request.AuthTokenUrl, request.PlatformJwksUrl, request.AuthorizationUrl,
                request.KeyId, request.PrivateKeyPem, request.Active);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        context.Set<LtiDeployment>().Add(deployment);
        await context.SaveChangesAsync().ConfigureAwait(false);
        logger.LogInformation("LTI deployment created: {DeploymentId}", deployment.Id);

        return Created($"/v1/lti/deployments/{deployment.Id}", LtiDeploymentDto.FromEntity(deployment));
    }

    [HttpPost("v1/lti/deployments/{id:guid}/line-items")]
    public async Task<IActionResult> CreateLineItem(Guid id, [FromBody] CreateLtiLineItemRequest request)
    {
        if (!actorContextAccessor.ActorContext.IsSystemAdmin)
        {
            return Forbid();
        }

        var deployment = await context.Set<LtiDeployment>()
            .FirstOrDefaultAsync(d => d.Id == id && d.DeletedAt == null)
            .ConfigureAwait(false);
        if (deployment is null)
        {
            return NotFound();
        }

        var exists = await context.Set<LtiLineItemMapping>()
            .AnyAsync(m => m.AssessmentId == request.AssessmentId)
            .ConfigureAwait(false);
        if (exists)
        {
            return Conflict($"Assessment {request.AssessmentId} is already mapped to a line item.");
        }

        LtiLineItemMapping mapping;
        try
        {
            mapping = LtiLineItemMapping.Create(
                request.AssessmentId, deployment.Id, request.LineItemId, request.LineItemUrl, request.MaxScore);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        context.Set<LtiLineItemMapping>().Add(mapping);
        await context.SaveChangesAsync().ConfigureAwait(false);
        logger.LogInformation("LTI line item mapping created: assessment {AssessmentId} -> {LineItemUrl}", request.AssessmentId, request.LineItemUrl);

        return Created($"/v1/lti/deployments/{deployment.Id}/line-items/{mapping.Id}", LtiLineItemMappingDto.FromEntity(mapping));
    }

    private async Task<LtiDeployment?> FindActiveDeploymentAsync(string issuer, string clientId)
    {
        if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(clientId))
        {
            return null;
        }

        return await context.Set<LtiDeployment>()
            .FirstOrDefaultAsync(d => d.Issuer == issuer && d.ClientId == clientId && d.Active && d.DeletedAt == null)
            .ConfigureAwait(false);
    }

    private async Task<User?> ResolveUserAsync(LtiDeployment deployment, string sub, string? email)
    {
        var mapping = await context.Set<LtiUserMapping>()
            .FirstOrDefaultAsync(u => u.DeploymentId == deployment.Id && u.Sub == sub)
            .ConfigureAwait(false);

        if (mapping is not null)
        {
            return await context.Set<User>()
                .FirstOrDefaultAsync(u => u.Id == mapping.UserId && u.DeletedAt == null)
                .ConfigureAwait(false);
        }

        if (string.IsNullOrEmpty(email))
        {
            return null;
        }

        var normalized = email.Trim().ToLowerInvariant();
        var user = await context.Set<User>()
            .FirstOrDefaultAsync(u => u.DeletedAt == null && u.Email.ToLower() == normalized)
            .ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        try
        {
            context.Set<LtiUserMapping>().Add(LtiUserMapping.Create(deployment.Id, user.Id, sub));
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Concurrent launch already inserted the mapping — the DB unique index
            // (DeploymentId, Sub) is the authority; continue signing the user in.
            logger.LogWarning(ex, "LTI: user mapping upsert raced for deployment {DeploymentId} sub {Sub}", deployment.Id, sub);
        }

        return user;
    }
}
