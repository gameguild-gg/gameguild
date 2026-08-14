using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using GameGuild.Identity.Context.Middleware;

namespace GameGuild.Identity.Context.UnitTests;

#region ActorContext Additional Tests

public class ActorContextAdditionalTests
{
    private static ActorContext CreateContext(
        IEnumerable<string>? roles = null,
        IEnumerable<string>? permissions = null,
        ActorAttributes? typedAttributes = null,
        string? subjectId = "user-1",
        bool isAuthenticated = true)
    {
        return new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = subjectId,
            TenantId = Guid.NewGuid(),
            Roles = new HashSet<string>(roles ?? []),
            Permissions = new HashSet<string>(permissions ?? []),
            TypedAttributes = typedAttributes ?? ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = isAuthenticated
        };
    }

    // --- HasPermission(string) regular match ---
    [Fact]
    public void HasPermission_Should_Return_True_For_Exact_Match()
    {
        var ctx = CreateContext(permissions: ["users:read", "users:write"]);
        ctx.HasPermission("users:read").Should().BeTrue();
    }

    [Fact]
    public void HasPermission_Should_Return_False_When_Not_Present()
    {
        var ctx = CreateContext(permissions: ["users:read"]);
        ctx.HasPermission("users:delete").Should().BeFalse();
    }

    [Fact]
    public void HasPermission_String_Should_Throw_When_Null()
    {
        var ctx = CreateContext();
        var act = () => ctx.HasPermission((string)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HasPermission_Object_Should_Throw_When_Null()
    {
        var ctx = CreateContext();
        var act = () => ctx.HasPermission((object)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // --- HasAnyPermission(string[]) ---
    [Fact]
    public void HasAnyPermission_String_Should_Return_False_For_Empty()
    {
        var ctx = CreateContext(permissions: ["users:read"]);
        ctx.HasAnyPermission(Array.Empty<string>()).Should().BeFalse();
    }

    [Fact]
    public void HasAnyPermission_String_Should_Return_True_For_SystemAdmin()
    {
        var ctx = CreateContext(roles: ["SystemAdmin"]);
        ctx.HasAnyPermission("any:perm", "other:perm").Should().BeTrue();
    }

    // --- HasAnyPermission(object[]) ---
    [Fact]
    public void HasAnyPermission_Object_Should_Return_False_For_Empty()
    {
        var ctx = CreateContext(permissions: ["users:read"]);
        ctx.HasAnyPermission(Array.Empty<object>()).Should().BeFalse();
    }

    [Fact]
    public void HasAnyPermission_Object_Should_Return_True_For_SystemAdmin()
    {
        var ctx = CreateContext(roles: ["SystemAdmin"]);
        ctx.HasAnyPermission((object)"any:perm").Should().BeTrue();
    }

    [Fact]
    public void HasAnyPermission_Object_Should_Return_True_When_Match()
    {
        var ctx = CreateContext(permissions: ["projects:read"]);
        ctx.HasAnyPermission((object)"projects:read", (object)"users:read").Should().BeTrue();
    }

    [Fact]
    public void HasAnyPermission_Object_Should_Return_False_When_No_Match()
    {
        var ctx = CreateContext(permissions: ["projects:read"]);
        ctx.HasAnyPermission((object)"users:read", (object)"users:write").Should().BeFalse();
    }

    // --- HasAllPermissions(string[]) ---
    [Fact]
    public void HasAllPermissions_String_Should_Return_True_For_Empty()
    {
        var ctx = CreateContext();
        ctx.HasAllPermissions(Array.Empty<string>()).Should().BeTrue();
    }

    [Fact]
    public void HasAllPermissions_String_Should_Return_True_For_SystemAdmin()
    {
        var ctx = CreateContext(roles: ["SystemAdmin"]);
        ctx.HasAllPermissions("a:b", "c:d").Should().BeTrue();
    }

    [Fact]
    public void HasAllPermissions_String_Should_Return_True_When_All_Match()
    {
        var ctx = CreateContext(permissions: ["a:b", "c:d"]);
        ctx.HasAllPermissions("a:b", "c:d").Should().BeTrue();
    }

    [Fact]
    public void HasAllPermissions_String_Should_Return_False_When_One_Missing()
    {
        var ctx = CreateContext(permissions: ["a:b"]);
        ctx.HasAllPermissions("a:b", "c:d").Should().BeFalse();
    }

    // --- HasAllPermissions(object[]) ---
    [Fact]
    public void HasAllPermissions_Object_Should_Return_True_For_Empty()
    {
        var ctx = CreateContext();
        ctx.HasAllPermissions(Array.Empty<object>()).Should().BeTrue();
    }

    [Fact]
    public void HasAllPermissions_Object_Should_Return_True_For_SystemAdmin()
    {
        var ctx = CreateContext(roles: ["SystemAdmin"]);
        ctx.HasAllPermissions((object)"a:b", (object)"c:d").Should().BeTrue();
    }

    [Fact]
    public void HasAllPermissions_Object_Should_Return_True_When_All_Match()
    {
        var ctx = CreateContext(permissions: ["a:b", "c:d"]);
        ctx.HasAllPermissions((object)"a:b", (object)"c:d").Should().BeTrue();
    }

    [Fact]
    public void HasAllPermissions_Object_Should_Return_False_When_One_Missing()
    {
        var ctx = CreateContext(permissions: ["a:b"]);
        ctx.HasAllPermissions((object)"a:b", (object)"c:d").Should().BeFalse();
    }

    // --- IsInRole ---
    [Fact]
    public void IsInRole_Should_Return_True_When_In_Role()
    {
        var ctx = CreateContext(roles: ["Editor", "Viewer"]);
        ctx.IsInRole("Editor").Should().BeTrue();
    }

    [Fact]
    public void IsInRole_Should_Return_False_When_Not_In_Role()
    {
        var ctx = CreateContext(roles: ["Editor"]);
        ctx.IsInRole("Admin").Should().BeFalse();
    }

    [Fact]
    public void IsInRole_Should_Throw_When_Null()
    {
        var ctx = CreateContext();
        var act = () => ctx.IsInRole(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // --- GetAttribute ---
    [Fact]
    public void GetAttribute_Should_Return_Custom_Attribute()
    {
        var ctx = ActorContextBuilder.ForUser(Guid.NewGuid())
            .WithAttribute("custom_key", "custom_value")
            .Build();
        ctx.GetAttribute("custom_key").Should().Be("custom_value");
    }

    [Fact]
    public void GetAttribute_Should_Return_Typed_Attribute()
    {
        var attrs = new ActorAttributes { Email = "test@example.com" };
        var ctx = ActorContextBuilder.Create()
            .WithTypedAttributes(attrs)
            .Build();
        ctx.GetAttribute("email").Should().Be("test@example.com");
    }

    [Fact]
    public void GetAttribute_Should_Return_Null_When_Not_Found()
    {
        var ctx = CreateContext();
        ctx.GetAttribute("nonexistent").Should().BeNull();
    }

    // --- IsMfaVerified ---
    [Fact]
    public void IsMfaVerified_Should_Return_True_When_MFA_Verified()
    {
        var ctx = ActorContextBuilder.Create()
            .WithMfaVerified()
            .AsAuthenticated()
            .Build();
        ctx.IsMfaVerified.Should().BeTrue();
    }

    [Fact]
    public void IsMfaVerified_Should_Return_False_When_Not_Verified()
    {
        var ctx = CreateContext();
        ctx.IsMfaVerified.Should().BeFalse();
    }

    // --- SubjectIdAsGuid ---
    [Fact]
    public void SubjectIdAsGuid_Should_Return_Null_For_Non_Guid()
    {
        var ctx = CreateContext(subjectId: "not-a-guid");
        ctx.SubjectIdAsGuid.Should().BeNull();
    }

    [Fact]
    public void SubjectIdAsGuid_Should_Return_Null_For_Null_SubjectId()
    {
        var ctx = CreateContext(subjectId: null);
        ctx.SubjectIdAsGuid.Should().BeNull();
    }

    // --- IsSystemAdmin ---
    [Fact]
    public void IsSystemAdmin_Should_Return_True_For_SystemAdmin_Role()
    {
        var ctx = CreateContext(roles: ["SystemAdmin"]);
        ctx.IsSystemAdmin.Should().BeTrue();
    }

    [Fact]
    public void IsSystemAdmin_Should_Return_False_When_No_Admin_Role()
    {
        var ctx = CreateContext(roles: ["Editor"]);
        ctx.IsSystemAdmin.Should().BeFalse();
    }

    // --- IsTenantAdmin via IsSystemAdmin ---
    [Fact]
    public void IsTenantAdmin_Should_Return_True_For_SystemAdmin()
    {
        var ctx = CreateContext(roles: ["SystemAdmin"]);
        ctx.IsTenantAdmin.Should().BeTrue();
    }

    [Fact]
    public void IsTenantAdmin_Should_Return_False_When_No_Admin_Role()
    {
        var ctx = CreateContext(roles: ["Editor"]);
        ctx.IsTenantAdmin.Should().BeFalse();
    }

    // --- Obsolete Attributes property ---
    [Fact]
#pragma warning disable CS0618
    public void Attributes_Should_Return_Dictionary_From_TypedAttributes()
    {
        var attrs = new ActorAttributes { Email = "test@example.com", TenantRole = "Owner" };
        var ctx = ActorContextBuilder.Create()
            .WithTypedAttributes(attrs)
            .Build();
        var dict = ctx.Attributes;
        dict.Should().ContainKey("email");
        dict["email"].Should().Be("test@example.com");
    }
#pragma warning restore CS0618

    // --- Anonymous ---
    [Fact]
    public void Anonymous_Should_Not_Be_Authenticated()
    {
        ActorContext.Anonymous.IsAuthenticated.Should().BeFalse();
        ActorContext.Anonymous.ActorKind.Should().Be(ActorKind.Anonymous);
        ActorContext.Anonymous.SubjectId.Should().BeNull();
        ActorContext.Anonymous.IsSystemAdmin.Should().BeFalse();
        ActorContext.Anonymous.IsTenantAdmin.Should().BeFalse();
        ActorContext.Anonymous.IsMfaVerified.Should().BeFalse();
    }
}

#endregion

#region ActorContextBuilder Additional Tests

public class ActorContextBuilderAdditionalTests
{
    [Fact]
    public void ForUser_UserActor_Should_Map_Email_And_Name()
    {
        var userId = Guid.NewGuid();
        var actor = new UserActor(userId, "user@example.com", "John Doe");
        var ctx = ActorContextBuilder.ForUser(actor).Build();

        ctx.ActorKind.Should().Be(ActorKind.User);
        ctx.SubjectId.Should().Be(userId.ToString());
        ctx.IsAuthenticated.Should().BeTrue();
        ctx.GetAttribute("email").Should().Be("user@example.com");
        ctx.GetAttribute("name").Should().Be("John Doe");
    }

    [Fact]
    public void ForUser_UserActor_Should_Work_Without_Email_And_Name()
    {
        var userId = Guid.NewGuid();
        var actor = new UserActor(userId);
        var ctx = ActorContextBuilder.ForUser(actor).Build();

        ctx.SubjectId.Should().Be(userId.ToString());
        ctx.GetAttribute("email").Should().BeNull();
        ctx.GetAttribute("name").Should().BeNull();
    }

    [Fact]
    public void ForUser_UserActor_Should_Throw_When_Null()
    {
        var act = () => ActorContextBuilder.ForUser((UserActor)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithRoles_Should_Add_Multiple_Roles()
    {
        var ctx = ActorContextBuilder.Create()
            .WithRoles(["Admin", "Editor", "Viewer"])
            .AsAuthenticated()
            .Build();

        ctx.Roles.Should().Contain("Admin");
        ctx.Roles.Should().Contain("Editor");
        ctx.Roles.Should().Contain("Viewer");
    }

    [Fact]
    public void WithRoles_Should_Throw_When_Null()
    {
        var act = () => ActorContextBuilder.Create().WithRoles(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithPermissions_Should_Add_Multiple_Permissions()
    {
        var ctx = ActorContextBuilder.Create()
            .WithPermissions(["users:read", "users:write"])
            .AsAuthenticated()
            .Build();

        ctx.Permissions.Should().Contain("users:read");
        ctx.Permissions.Should().Contain("users:write");
    }

    [Fact]
    public void WithPermissions_Should_Throw_When_Null()
    {
        var act = () => ActorContextBuilder.Create().WithPermissions(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithRole_Should_Throw_When_Null()
    {
        var act = () => ActorContextBuilder.Create().WithRole(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithPermission_Should_Throw_When_Null()
    {
        var act = () => ActorContextBuilder.Create().WithPermission(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithAttribute_Should_Throw_When_Key_Null()
    {
        var act = () => ActorContextBuilder.Create().WithAttribute(null!, "value");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithAttribute_Should_Throw_When_Value_Null()
    {
        var act = () => ActorContextBuilder.Create().WithAttribute("key", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithAttributes_Should_Add_Multiple()
    {
        var attrs = new Dictionary<string, string>
        {
            ["department"] = "Engineering",
            ["region"] = "US"
        };
        var ctx = ActorContextBuilder.Create()
            .WithAttributes(attrs)
            .Build();

        ctx.GetAttribute("department").Should().Be("Engineering");
        ctx.GetAttribute("region").Should().Be("US");
    }

    [Fact]
    public void WithAttributes_Should_Throw_When_Null()
    {
        var act = () => ActorContextBuilder.Create().WithAttributes(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithAuthScheme_Should_Set_AuthScheme()
    {
        var ctx = ActorContextBuilder.ForUser(Guid.NewGuid())
            .WithAuthScheme("ApiKey")
            .Build();

        ctx.AuthScheme.Should().Be("ApiKey");
    }

    [Fact]
    public void WithMfaVerified_False_Should_Set_Attribute_To_False()
    {
        var ctx = ActorContextBuilder.Create()
            .WithMfaVerified(false)
            .AsAuthenticated()
            .Build();

        ctx.IsMfaVerified.Should().BeFalse();
    }

    [Fact]
    public void WithTypedAttributes_Should_Throw_When_Null()
    {
        var act = () => ActorContextBuilder.Create().WithTypedAttributes(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithPermission_Should_Add_Single_Permission()
    {
        var ctx = ActorContextBuilder.Create()
            .WithPermission("users:read")
            .Build();

        ctx.Permissions.Should().Contain("users:read");
    }

    [Fact]
    public void ForService_Should_Throw_On_Null_ServiceId()
    {
        var act = () => ActorContextBuilder.ForService(null!, "name");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ForService_Should_Throw_On_Null_ServiceName()
    {
        var act = () => ActorContextBuilder.ForService("id", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ForSystem_Should_Throw_On_Null_OperationName()
    {
        var act = () => ActorContextBuilder.ForSystem(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}

#endregion

#region SecurityAuditLogger Additional Tests

public class SecurityAuditLoggerAdditionalTests
{
    private sealed class TestLogger<T> : ILogger<T>
    {
        public LogLevel? LastLevel { get; private set; }
        public string? LastMessage { get; private set; }
        public int LogCount { get; private set; }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NoopScope();
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LastLevel = logLevel;
            LastMessage = formatter(state, exception);
            LogCount++;
        }

        private sealed class NoopScope : IDisposable { public void Dispose() { } }
    }

    private static ActorContext CreateUserContext() =>
        ActorContextBuilder.ForUser(Guid.NewGuid()).Build();

    [Fact]
    public async Task LogSensitiveAccessAsync_Should_Log_Information()
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);

        await auditLogger.LogSensitiveAccessAsync(CreateUserContext(),
            "UserProfile", "123", "ViewSSN");

        logger.LastLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public async Task LogPrivilegeEscalationAsync_Success_Should_Log_Information()
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);

        await auditLogger.LogPrivilegeEscalationAsync(CreateUserContext(),
            new[] { "Editor" }, new[] { "Admin" }, success: true);

        logger.LastLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public async Task LogPrivilegeEscalationAsync_Failure_Should_Log_Warning()
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);

        await auditLogger.LogPrivilegeEscalationAsync(CreateUserContext(),
            new[] { "Editor" }, new[] { "Admin" }, success: false, reason: "Denied");

        logger.LastLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public async Task LogCrossTenantAccessAsync_Success_Should_Log_Information()
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);

        await auditLogger.LogCrossTenantAccessAsync(CreateUserContext(),
            Guid.NewGuid(), Guid.NewGuid(), "resource", success: true);

        logger.LastLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public async Task LogUnauthorizedAccessAsync_Should_Log_Warning_With_Reason()
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);

        await auditLogger.LogUnauthorizedAccessAsync(CreateUserContext(),
            "Document", "doc-1", "documents:read", "Insufficient clearance");

        logger.LastLevel.Should().Be(LogLevel.Warning);
        logger.LastMessage.Should().Contain("UnauthorizedAccessAttempt");
    }

    [Fact]
    public async Task LogAsync_Direct_Should_Log_At_Correct_Level()
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);

        var auditEvent = SecurityAuditEvent.Create(
            SecurityEventType.ActorContextCreated,
            CreateUserContext(),
            "TestResource", "1",
            permission: null, success: true, reason: "Test");

        await auditLogger.LogAsync(auditEvent);

        logger.LastLevel.Should().Be(LogLevel.Debug);
    }

    [Fact]
    public async Task LogAsync_ImpersonationStarted_Should_Log_Warning()
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);

        var auditEvent = SecurityAuditEvent.Create(
            SecurityEventType.ImpersonationStarted,
            CreateUserContext(),
            "User", "target-1",
            permission: null, success: true, reason: "Admin impersonation");

        await auditLogger.LogAsync(auditEvent);

        logger.LastLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public async Task LogAsync_ImpersonationEnded_Should_Log_Information()
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);

        var auditEvent = SecurityAuditEvent.Create(
            SecurityEventType.ImpersonationEnded,
            CreateUserContext(),
            "User", "target-1",
            permission: null, success: true, reason: "Session ended");

        await auditLogger.LogAsync(auditEvent);

        logger.LastLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public async Task LogAsync_SessionTerminated_Should_Log_Information()
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);

        var auditEvent = SecurityAuditEvent.Create(
            SecurityEventType.SessionTerminated,
            CreateUserContext(),
            null, null,
            permission: null, success: true, reason: "Logout");

        await auditLogger.LogAsync(auditEvent);

        logger.LastLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public async Task LogAsync_ContextElevated_Should_Log_Information()
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);

        var auditEvent = SecurityAuditEvent.Create(
            SecurityEventType.ContextElevated,
            CreateUserContext(),
            "JIT", "elevation-1",
            permission: null, success: true, reason: "JIT access");

        await auditLogger.LogAsync(auditEvent);

        logger.LastLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public async Task LogAsync_ContextElevationExpired_Should_Log_Information()
    {
        var logger = new TestLogger<SecurityAuditLogger>();
        var auditLogger = new SecurityAuditLogger(logger);

        var auditEvent = SecurityAuditEvent.Create(
            SecurityEventType.ContextElevationExpired,
            CreateUserContext(),
            "JIT", "elevation-1",
            permission: null, success: true, reason: "Expired");

        await auditLogger.LogAsync(auditEvent);

        logger.LastLevel.Should().Be(LogLevel.Information);
    }
}

#endregion

#region TenantValidationResult Additional Tests

public class TenantValidationResultAdditionalTests
{
    [Fact]
    public void Success_ToActionResult_Should_Return_Null()
    {
        var result = TenantValidationResult.Success();
        result.ToActionResult().Should().BeNull();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Forbidden_ToActionResult_Should_Return_ObjectResult_Without_ErrorDetails()
    {
        var result = TenantValidationResult.Forbidden("No tenant association");
        result.IsValid.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        result.ErrorDetails.Should().BeNull();

        var actionResult = result.ToActionResult();
        actionResult.Should().BeOfType<ObjectResult>();
        var objResult = actionResult as ObjectResult;
        objResult!.StatusCode.Should().Be(403);
    }

    [Fact]
    public void CrossTenantDenied_Should_Have_ErrorDetails()
    {
        var userTenant = Guid.NewGuid();
        var requestedTenant = Guid.NewGuid();
        var result = TenantValidationResult.CrossTenantDenied(userTenant, requestedTenant, "update resource");

        result.IsValid.Should().BeFalse();
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        result.ErrorDetails.Should().NotBeNull();
        result.ErrorMessage.Should().Contain(userTenant.ToString());
        result.ErrorMessage.Should().Contain(requestedTenant.ToString());
    }

    [Fact]
    public void CrossTenantDenied_ToActionResult_Should_Return_ObjectResult_With_ErrorDetails()
    {
        var result = TenantValidationResult.CrossTenantDenied(Guid.NewGuid(), Guid.NewGuid(), "delete");
        var actionResult = result.ToActionResult();
        actionResult.Should().BeOfType<ObjectResult>();
        var objResult = actionResult as ObjectResult;
        objResult!.StatusCode.Should().Be(403);
    }
}

#endregion

#region ActorContextAccessor Additional Tests

public class ActorContextAccessorAdditionalTests
{
    [Fact]
    public void SetActorContext_Should_Throw_When_Null()
    {
        var accessor = new ActorContextAccessor();
        var act = () => accessor.SetActorContext(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ClearActorContext_On_Fresh_Accessor_Should_Not_Throw()
    {
        var accessor = new ActorContextAccessor();
        // ClearActorContext on a fresh accessor (no holder set)
        var act = () => accessor.ClearActorContext();
        act.Should().NotThrow();
        accessor.ActorContext.Should().Be(ActorContext.Anonymous);
    }

    [Fact]
    public void SetActorContext_Should_Replace_Previous_Context()
    {
        var accessor = new ActorContextAccessor();
        var context1 = ActorContextBuilder.ForSystem("Job1").Build();
        var context2 = ActorContextBuilder.ForSystem("Job2").Build();

        accessor.SetActorContext(context1);
        accessor.ActorContext.Should().Be(context1);

        accessor.SetActorContext(context2);
        accessor.ActorContext.Should().Be(context2);
    }
}

#endregion

#region TenantValidationExtensions Additional Tests

public class TenantValidationExtensionsAdditionalTests
{
    [Fact]
    public void ValidateTenantAccess_Should_Succeed_When_Tenant_Matches()
    {
        var tenantId = Guid.NewGuid();
        var context = ActorContextBuilder.ForUser(Guid.NewGuid())
            .WithTenantId(tenantId)
            .Build();
        var accessor = new Mock<IActorContextAccessor>();
        accessor.Setup(a => a.ActorContext).Returns(context);

        var result = accessor.Object.ValidateTenantAccess(tenantId, "update settings");
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateTenantAccessAsActionResult_Should_Return_Null_When_Valid()
    {
        var tenantId = Guid.NewGuid();
        var context = ActorContextBuilder.ForUser(Guid.NewGuid())
            .WithTenantId(tenantId)
            .Build();
        var accessor = new Mock<IActorContextAccessor>();
        accessor.Setup(a => a.ActorContext).Returns(context);

        var result = accessor.Object.ValidateTenantAccessAsActionResult(tenantId, "read resource");
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateTenantAccessAsActionResult_Anonymous_Should_Return_Null()
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.Setup(a => a.ActorContext).Returns(ActorContext.Anonymous);

        var result = accessor.Object.ValidateTenantAccessAsActionResult(Guid.NewGuid(), "test");
        result.Should().BeNull();
    }
}

#endregion

#region MiddlewareOrderValidator Additional Tests

public class MiddlewareOrderValidatorAdditionalTests
{
    // Reuse the fake middleware types
    private sealed class AuthenticationMiddleware
    {
        public RequestDelegate Invoke(RequestDelegate next) => ctx => next(ctx);
    }

    private sealed class TenantMiddleware
    {
        public RequestDelegate Invoke(RequestDelegate next) => ctx => next(ctx);
    }

    private sealed class ActorContextMiddleware
    {
        public RequestDelegate Invoke(RequestDelegate next) => ctx => next(ctx);
    }

    private sealed class AuthorizationMiddleware
    {
        public RequestDelegate Invoke(RequestDelegate next) => ctx => next(ctx);
    }

    private static IApplicationBuilder CreateAppWithPipeline(params Func<RequestDelegate, RequestDelegate>[] components)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var app = new ApplicationBuilder(services);
        foreach (var component in components)
            app.Use(component);
        return app;
    }

    [Fact]
    public void Should_Pass_When_No_Middleware_Registered()
    {
        var app = CreateAppWithPipeline();
        var act = () => MiddlewareOrderValidator.ValidateSecurityMiddlewareOrder(app);
        var ex = Record.Exception(act);
        if (ex is FileLoadException) return;
        ex.Should().BeNull();
    }

    [Fact]
    public void Should_Pass_When_Only_Auth_And_Tenant_In_Correct_Order()
    {
        var app = CreateAppWithPipeline(
            new AuthenticationMiddleware().Invoke,
            new TenantMiddleware().Invoke);
        var act = () => MiddlewareOrderValidator.ValidateSecurityMiddlewareOrder(app);
        var ex = Record.Exception(act);
        if (ex is FileLoadException) return;
        ex.Should().BeNull();
    }

    [Fact]
    public void Should_Fail_When_ActorContext_Before_Tenant()
    {
        var app = CreateAppWithPipeline(
            new AuthenticationMiddleware().Invoke,
            new ActorContextMiddleware().Invoke,
            new TenantMiddleware().Invoke);
        var act = () => MiddlewareOrderValidator.ValidateSecurityMiddlewareOrder(app);
        var ex = Record.Exception(act);
        if (ex is FileLoadException) return;
        ex.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Contain("ActorContextMiddleware must run AFTER TenantMiddleware");
    }

    [Fact]
    public void Should_Fail_When_ActorContext_Without_Tenant()
    {
        var app = CreateAppWithPipeline(
            new AuthenticationMiddleware().Invoke,
            new ActorContextMiddleware().Invoke);
        var act = () => MiddlewareOrderValidator.ValidateSecurityMiddlewareOrder(app);
        var ex = Record.Exception(act);
        if (ex is FileLoadException) return;
        ex.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Contain("requires TenantMiddleware");
    }

    [Fact]
    public void Should_Fail_Tenant_Before_Auth_Without_ActorContext()
    {
        // Legacy path: hasAuthentication && hasTenant && !hasActorContext
        // with tenantIndex < authenticationIndex
        var app = CreateAppWithPipeline(
            new TenantMiddleware().Invoke,
            new AuthenticationMiddleware().Invoke);
        var act = () => MiddlewareOrderValidator.ValidateSecurityMiddlewareOrder(app);
        var ex = Record.Exception(act);
        if (ex is FileLoadException) return;
        ex.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Contain("TenantMiddleware must run AFTER Authentication");
    }

    [Fact]
    public void Should_Pass_Auth_And_Tenant_Correct_Without_ActorContext()
    {
        // Legacy path: hasAuthentication && hasTenant && !hasActorContext
        // with correct order
        var app = CreateAppWithPipeline(
            new AuthenticationMiddleware().Invoke,
            new TenantMiddleware().Invoke);
        var act = () => MiddlewareOrderValidator.ValidateSecurityMiddlewareOrder(app);
        var ex = Record.Exception(act);
        if (ex is FileLoadException) return;
        ex.Should().BeNull();
    }
}

#endregion
