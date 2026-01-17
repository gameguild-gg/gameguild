using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Controllers;

public class TenantMetadataControllerTests
{
    [Fact]
    public async Task Metadata_Endpoints_Should_Return_Expected_Results()
    {
        var controller = new TenantMetadataController();
        var tenantId = Guid.NewGuid();

        var updateRequest = new UpdateTenantMetadataRequest(
            CustomFields: null,
            Tags: null,
            ExternalReferences: null,
            BusinessInfo: null,
            ContactInfo: null,
            AdminNotes: null
        );

        var replaceRequest = new ReplaceTenantMetadataRequest(
            new Dictionary<string, object?>(),
            new List<string>(),
            new Dictionary<string, string>(),
            new UpdateTenantBusinessInfoRequest(null, null, null, null, null),
            new UpdateTenantContactInfoRequest(null, null, null, null, null, null),
            null
        );

        (await controller.GetMetadata(tenantId, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.UpdateMetadata(tenantId, updateRequest, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.ReplaceMetadata(tenantId, replaceRequest, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.GetCustomFields(tenantId, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.UpdateCustomFields(tenantId, new Dictionary<string, object?>(), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.GetTags(tenantId, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.UpdateTags(tenantId, new UpdateTenantTagsRequest(new List<string>()), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.ReplaceTags(tenantId, new List<string>(), CancellationToken.None)).Should().BeOfType<NoContentResult>();
    }
}
