using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Compliance.Audit;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.API.IntegrationTests;

/// <summary>
/// Field-level data access auditing (issue #186) against the real API and PostgreSQL: writes through
/// the universal entity-change pipeline stage classified, masked field observations, and reads of
/// sensitive entity data on [DataMaskingResourceType] endpoints are recorded with the trusted
/// request actor context.
/// </summary>
[Collection(ApiPostgreSqlCollection.Name)]
public sealed class FieldAccessAuditPostgreSqlIntegrationTests(ApiPostgreSqlFixture fixture)
{
    private const string TestAuthenticationScheme = "FieldAccessAuditTest";

    [Fact]
    public async Task GetUsersPage_RecordsSensitiveFieldReadsWithRequestActorContext()
    {
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using var factory = fixture.CreateFactory(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthenticationScheme;
                    options.DefaultForbidScheme = TestAuthenticationScheme;
                }).AddScheme<AuthenticationSchemeOptions, FieldAccessAuditTestAuthenticationHandler>(
                    TestAuthenticationScheme,
                    _ => { });
                services.AddHttpContextAccessor();
                services.RemoveAll<IActorContextAccessor>();
                services.AddScoped<IActorContextAccessor, FieldAccessAuditTestActorContextAccessor>();
            }));

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedAsync(dbContext, tenantId, userId);
        }

        using var client = CreateClient(factory, tenantId, actorId);
        var response = await client.GetAsync($"/v1/users?limit=20&q=fieldaccess-{userId:N}");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Users endpoint returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        // The read surface records the sensitive fields of every returned entity with the
        // trusted actor/tenant context of the request, never a body-supplied identity.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var readRows = await dbContext.Set<FieldAccessAudit>()
                .IgnoreQueryFilters()
                .Where(row => row.EntityType == "User"
                    && row.EntityId == userId
                    && row.AccessType == FieldAccessType.Read)
                .ToListAsync();
            Assert.Contains(readRows, row => row.FieldName == "Email" && row.IsSensitiveField);
            Assert.Contains(readRows, row => row.FieldName == "PhoneNumber" && row.IsSensitiveField);
            Assert.True(readRows.All(row => row.TenantId == tenantId && row.UserId == actorId));
            Assert.True(readRows.All(row => row.NewValue is null && row.MaskedNewValue is null));
        }
    }

    [Fact]
    public async Task EntityWrites_StageClassifiedAndMaskedFieldObservations()
    {
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using var factory = fixture.CreateFactory(_ => { });

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedAsync(dbContext, tenantId, userId);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Set<User>().IgnoreQueryFilters().SingleAsync(candidate => candidate.Id == userId);
            var member = await dbContext.Set<TenantMember>().IgnoreQueryFilters()
                .SingleAsync(candidate => candidate.TenantId == tenantId && candidate.UserId == userId);

            // The ambient operation context is the trusted source for actor and tenant on writes.
            var accessor = new UseCaseOperationContextAccessor();
            using (accessor.Begin(new UseCaseOperationContext(
                "field-access-op", "TestCommand", tenantId, actorId, Guid.NewGuid(), null)))
            {
                user.Email = $"rotated-{userId:N}@example.test";
                await dbContext.SaveChangesAsync();
            }

            // Tenant-scoped entities resolve the tenant from the entity itself without any ambient context.
            member.Role = "Admin";
            await dbContext.SaveChangesAsync();
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailWrite = await dbContext.Set<FieldAccessAudit>()
                .IgnoreQueryFilters()
                .SingleAsync(row => row.EntityType == "User" && row.FieldName == "Email"
                    && row.AccessType == FieldAccessType.Write && row.TenantId == tenantId);
            // Sensitive write observations persist only masked renderings.
            Assert.True(emailWrite.IsSensitiveField);
            Assert.Null(emailWrite.OldValue);
            Assert.Null(emailWrite.NewValue);
            Assert.DoesNotContain($"fieldaccess-{userId:N}@example.test", emailWrite.MaskedOldValue);
            Assert.DoesNotContain($"rotated-{userId:N}@example.test", emailWrite.MaskedNewValue);
            Assert.Equal(actorId, emailWrite.UserId);

            var roleWrite = await dbContext.Set<FieldAccessAudit>()
                .IgnoreQueryFilters()
                .SingleAsync(row => row.EntityType == "TenantMember" && row.FieldName == "Role"
                    && row.AccessType == FieldAccessType.Write
                    && row.OldValue == "Member" && row.NewValue == "Admin");
            Assert.Equal(tenantId, roleWrite.TenantId);
        }
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory, Guid tenantId, Guid actorId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TenantResolver.TenantIdHeader, tenantId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Actor", actorId.ToString());
        return client;
    }

    private static async Task SeedAsync(ApplicationDbContext dbContext, Guid tenantId, Guid userId)
    {
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = $"Field Access Tenant {tenantId:N}",
            Slug = $"fieldaccess-{tenantId:N}",
            AdminEmail = $"admin-{tenantId:N}@example.test",
            IsActive = true
        };
        var user = User.CreateOAuthUser($"fieldaccess-{userId:N}@example.test", "Field Access User");
        user.Id = userId;
        user.PhoneNumber = $"+1555{userId.GetHashCode():000000}";

        dbContext.Set<Tenant>().Add(tenant);
        dbContext.Set<User>().Add(user);
        dbContext.Set<TenantMember>().Add(new TenantMember
        {
            TenantId = tenantId,
            UserId = userId,
            Role = "Member",
            IsActive = true
        });
        await dbContext.SaveChangesAsync();
    }

    public sealed class FieldAccessAuditTestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var actorId = Request.Headers["X-Test-Actor"].ToString();
            var tenantId = Request.Headers[TenantResolver.TenantIdHeader].ToString();
            if (!Guid.TryParse(actorId, out _) || !Guid.TryParse(tenantId, out _))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(
            [
                new Claim("sub", actorId),
                new Claim("tenant_id", tenantId),
                new Claim(ClaimTypes.Role, Policies.SystemAdmin),
                new Claim("role", Policies.SystemAdmin),
                new Claim("permission", UsersPermission.Keys.Read)
            ],
            Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }

    private sealed class FieldAccessAuditTestActorContextAccessor(IHttpContextAccessor httpContextAccessor)
        : IActorContextAccessor
    {
        private ActorContext? _actorContext;

        public ActorContext ActorContext
        {
            get
            {
                if (_actorContext is not null)
                {
                    return _actorContext;
                }

                var request = httpContextAccessor.HttpContext?.Request;
                if (request is null ||
                    !Guid.TryParse(request.Headers["X-Test-Actor"], out var actorId) ||
                    !Guid.TryParse(request.Headers[TenantResolver.TenantIdHeader], out var tenantId))
                {
                    return ActorContext.Anonymous;
                }

                return new ActorContext
                {
                    ActorKind = ActorKind.User,
                    SubjectId = actorId.ToString(),
                    TenantId = tenantId,
                    Roles = new HashSet<string>([Policies.SystemAdmin], StringComparer.OrdinalIgnoreCase),
                    Permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    IsAuthenticated = true
                };
            }
        }

        public void SetActorContext(ActorContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _actorContext = context;
        }

        public void ClearActorContext()
        {
            _actorContext = null;
        }

        public IActorContextScope BeginActorContextScope(ActorContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            var previous = _actorContext;
            SetActorContext(context);
            return new RestoreScope(this, previous);
        }

        private sealed class RestoreScope(FieldAccessAuditTestActorContextAccessor accessor, ActorContext? previous)
            : IActorContextScope
        {
            private bool disposed;

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                accessor._actorContext = previous;
            }
        }
    }
}
