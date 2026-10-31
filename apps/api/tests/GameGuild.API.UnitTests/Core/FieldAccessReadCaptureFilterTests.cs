using FluentAssertions;
using GameGuild.API;
using GameGuild.API.Database;
using GameGuild.Commerce.Products;
using GameGuild.Compliance.Audit;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GameGuild.API.UnitTests.Core;

/// <summary>
/// Field-level read capture (issue #186 criterion 1): endpoints returning sensitive entity data
/// ([DataMaskingResourceType]) record one read observation per classifier-detected sensitive field,
/// attributed to the trusted ambient actor; capture is fail-open and never alters the response.
/// </summary>
public sealed class FieldAccessReadCaptureFilterTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    [Fact]
    public async Task AnnotatedEndpoint_RecordsSensitiveFieldReadsWithTrustedActorContext()
    {
        await using var context = NewContext();
        var filter = CreateFilter(context);
        var userId = Guid.NewGuid();
        var context2 = CreateContext(
            new ObjectResult(new UserDto(userId, "reader@example.com", "Reader", DateTime.UtcNow, null))
            {
                StatusCode = StatusCodes.Status200OK
            },
            new DataMaskingResourceTypeAttribute("User"));
        context2.HttpContext.Request.Path = "/v1/users";

        await ExecuteAsync(context2, filter);

        var rows = await context.Set<FieldAccessAudit>().IgnoreQueryFilters().ToListAsync();
        rows.Should().NotBeEmpty();
        rows.Should().OnlyContain(row => row.AccessType == FieldAccessType.Read);
        rows.Should().OnlyContain(row => row.TenantId == TenantId && row.UserId == ActorId);
        rows.Should().Contain(row => row.EntityId == userId && row.FieldName == "Email" && row.IsSensitiveField);
        rows.Should().NotContain(row => row.FieldName == "Name"); // non-sensitive reads are not the compliance surface
        rows.Should().OnlyContain(row => row.ApiEndpoint == "/v1/users");
    }

    [Fact]
    public async Task PagedResponse_RecordsReadsPerReturnedEntity_AndHonorsTheItemCap()
    {
        await using var context = NewContext();
        var filter = CreateFilter(context, new FieldLevelAuditOptions { MaxReadCaptureItemsPerResponse = 1 });
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var paged = new PagedResult<UserDto>(
        [
            new UserDto(first, "first@example.com", "First", DateTime.UtcNow, null),
            new UserDto(second, "second@example.com", "Second", DateTime.UtcNow, null)
        ], totalCount: 2, skip: 0, take: 2);
        var filterContext = CreateContext(
            new ObjectResult(paged) { StatusCode = StatusCodes.Status200OK },
            new DataMaskingResourceTypeAttribute("User"));

        await ExecuteAsync(filterContext, filter);

        var rows = await context.Set<FieldAccessAudit>().IgnoreQueryFilters().ToListAsync();
        // Only the first returned entity is walked (cap = 1): its sensitive fields — Email and the
        // name-classified PhoneNumber slot — are recorded and nothing for the second entity.
        rows.Should().OnlyContain(row => row.EntityId == first);
        rows.Should().Contain(row => row.FieldName == "Email" && row.IsSensitiveField);
        rows.Should().NotContain(row => row.EntityId == second);
    }

    [Fact]
    public async Task CaptureFailure_IsFailOpenAndNeverChangesTheResponse()
    {
        // A disposed context makes persistence throw; the response must pass through untouched.
        var context = NewContext();
        await context.DisposeAsync();
        var filter = CreateFilter(context);
        var result = new ObjectResult(new UserDto(Guid.NewGuid(), "fail@example.com", "Fail", DateTime.UtcNow, null))
        {
            StatusCode = StatusCodes.Status200OK
        };
        var filterContext = CreateContext(result, new DataMaskingResourceTypeAttribute("User"));
        var nextCalled = false;

        await filter.OnResultExecutionAsync(filterContext, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ResultExecutedContext(
                ToActionContext(filterContext), filterContext.Filters, filterContext.Result, filterContext.Controller));
        });

        nextCalled.Should().BeTrue();
        filterContext.Result.Should().BeSameAs(result);
        filterContext.HttpContext.Response.StatusCode.Should().NotBe(500);
    }

    [Fact]
    public async Task EndpointWithoutResourceAnnotation_RecordsNothing()
    {
        await using var context = NewContext();
        var filter = CreateFilter(context);
        var filterContext = CreateContext(
            new ObjectResult(new UserDto(Guid.NewGuid(), "plain@example.com", "Plain", DateTime.UtcNow, null))
            {
                StatusCode = StatusCodes.Status200OK
            },
            new object());

        await ExecuteAsync(filterContext, filter);

        (await context.Set<FieldAccessAudit>().IgnoreQueryFilters().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AnonymousActor_RecordsNothing()
    {
        await using var context = NewContext();
        var filter = CreateFilter(context, anonymous: true);
        var filterContext = CreateContext(
            new ObjectResult(new UserDto(Guid.NewGuid(), "anon@example.com", "Anon", DateTime.UtcNow, null))
            {
                StatusCode = StatusCodes.Status200OK
            },
            new DataMaskingResourceTypeAttribute("User"));

        await ExecuteAsync(filterContext, filter);

        (await context.Set<FieldAccessAudit>().IgnoreQueryFilters().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SupportTicketSurface_RecordsReporterEmailReadsFromTicketResponses()
    {
        await using var context = NewContext();
        var filter = CreateFilter(context);
        var ticketId = Guid.NewGuid();
        var ticket = new SupportTicketDto(
            ticketId,
            TenantId,
            Guid.NewGuid(), "Customer Company",
            Guid.NewGuid(), "Reporter Name", "reporter@example.com",
            "Cannot access purchased item", null,
            SupportTicketStatus.Open, SupportTicketPriority.Normal,
            null, null,
            DateTime.UtcNow, null, null, null, null, null,
            DateTime.UtcNow, null, 0,
            []);
        var filterContext = CreateContext(
            new ObjectResult(ticket) { StatusCode = StatusCodes.Status200OK },
            new DataMaskingResourceTypeAttribute("SupportTicket"));

        await ExecuteAsync(filterContext, filter);

        var rows = await context.Set<FieldAccessAudit>().IgnoreQueryFilters().ToListAsync();
        rows.Should().NotBeEmpty();
        rows.Should().Contain(row => row.EntityId == ticketId && row.FieldName == "ReporterEmail" && row.IsSensitiveField);
        // Bare person names (CustomerName/ReporterName) are not classifier-sensitive; nested
        // message author emails are not top-level ticket fields.
        rows.Should().NotContain(row => row.FieldName == "CustomerName" || row.FieldName == "ReporterName");
    }

    [Fact]
    public async Task UserProfileSurface_RecordsDisplayNameAndAvatarUrlReads()
    {
        await using var context = NewContext();
        var filter = CreateFilter(context);
        var profileId = Guid.NewGuid();
        var profile = new UserProfileDto(
            profileId,
            Guid.NewGuid(), "Display Name", "bio", "location", "https://example.com", "title", "company",
            "https://cdn.example.com/avatar.png", null, null, null,
            "Private", false, false,
            DateTimeOffset.UtcNow, null,
            []);
        var filterContext = CreateContext(
            new ObjectResult(profile) { StatusCode = StatusCodes.Status200OK },
            new DataMaskingResourceTypeAttribute("UserProfile"));

        await ExecuteAsync(filterContext, filter);

        var rows = await context.Set<FieldAccessAudit>().IgnoreQueryFilters().ToListAsync();
        rows.Should().NotBeEmpty();
        rows.Should().Contain(row => row.EntityId == profileId && row.FieldName == "DisplayName" && row.IsSensitiveField);
        rows.Should().Contain(row => row.EntityId == profileId && row.FieldName == "AvatarUrl" && row.IsSensitiveField);
    }

    /// <summary>
    /// The read-capture surface must keep covering the controllers that return data-subject PII:
    /// the annotation is what binds <see cref="FieldAccessReadCaptureFilter"/> (and, separately,
    /// rule-driven masking) to an endpoint. Regressions that drop the attribute silently disable
    /// field-level read tracking for that surface.
    /// </summary>
    [Fact]
    public void PiiReturningControllers_AreAnnotatedForFieldLevelReadCapture()
    {
        var expectedSurfaces = new Dictionary<Type, string>
        {
            [typeof(UsersController)] = "User",
            [typeof(UserProfilesController)] = "UserProfile",
            [typeof(SupportTicketsController)] = "SupportTicket",
            [typeof(MySupportTicketsController)] = "SupportTicket"
        };

        foreach (var (controller, resourceType) in expectedSurfaces)
        {
            var attribute = controller.GetCustomAttributes(typeof(DataMaskingResourceTypeAttribute), inherit: true)
                .OfType<DataMaskingResourceTypeAttribute>()
                .SingleOrDefault();
            attribute.Should().NotBeNull(
                $"{controller.Name} returns data-subject PII and must carry [DataMaskingResourceType] so field-level read capture (issue #186 criterion 1) applies to its responses");
            attribute!.ResourceType.Should().Be(resourceType);
        }
    }

    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static FieldAccessReadCaptureFilter CreateFilter(ApplicationDbContext context, FieldLevelAuditOptions? settings = null, bool anonymous = false)
    {
        var actor = anonymous
            ? ActorContext.Anonymous
            : new ActorContext
            {
                ActorKind = ActorKind.User,
                SubjectId = ActorId.ToString(),
                TenantId = TenantId,
                Roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                Permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                IsAuthenticated = true
            };
        var actors = new Mock<IActorContextAccessor>();
        actors.SetupGet(accessor => accessor.ActorContext).Returns(actor);
        return new FieldAccessReadCaptureFilter(
            new FieldAccessAuditService(context, TimeProvider.System),
            actors.Object,
            Options.Create(settings ?? new FieldLevelAuditOptions()),
            NullLogger<FieldAccessReadCaptureFilter>.Instance);
    }

    private static ResultExecutingContext CreateContext(IActionResult result, object endpointMetadata)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/v1/users";
        var actionDescriptor = new ActionDescriptor { EndpointMetadata = [endpointMetadata] };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        var filters = new List<IFilterMetadata>();
        return new ResultExecutingContext(actionContext, filters, result, new object());
    }

    private static ActionContext ToActionContext(ResultExecutingContext context) =>
        new(context.HttpContext, context.RouteData, context.ActionDescriptor);

    private static Task ExecuteAsync(ResultExecutingContext context, FieldAccessReadCaptureFilter filter) =>
        filter.OnResultExecutionAsync(context, () => Task.FromResult(
            new ResultExecutedContext(ToActionContext(context), context.Filters, context.Result, context.Controller)));
}
