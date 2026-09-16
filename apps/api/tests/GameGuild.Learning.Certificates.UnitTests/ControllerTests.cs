using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Certificates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Learning.Certificates.Tests;

public class ControllerTests
{
    private readonly Mock<ICertificateService> _certSvc = new();
    private readonly Mock<ICertificateTemplateService> _tmplSvc = new();
    private readonly Mock<IActorContextAccessor> _actor = new();
    private readonly Mock<ILogger<CertificatesController>> _log = new();
    private readonly Mock<ISender> _sender = new();

    private CertificatesController CreateController(Guid? userId = null, Guid? tenantId = null)
    {
        _actor.Setup(a => a.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = (userId ?? Guid.NewGuid()).ToString(),
            TenantId = tenantId ?? Guid.NewGuid(),
            IsAuthenticated = true,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>()
        });
        return new CertificatesController(_certSvc.Object, _tmplSvc.Object, _actor.Object, _log.Object, _sender.Object);
    }

    [Fact] public void Ctor_Creates() => CreateController().Should().NotBeNull();

    [Fact]
    public async Task GetMyCertificates_ReturnsOk()
    {
        var uid = Guid.NewGuid(); var tid = Guid.NewGuid();
        _certSvc.Setup(s => s.GetUserCertificatesAsync(uid, tid)).ReturnsAsync(new List<Certificate>());
        var r = await CreateController(uid, tid).GetMyCertificates();
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCertificate_Found_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _certSvc.Setup(s => s.GetCertificateByIdAsync(id))
            .ReturnsAsync(Certificate.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "U", "C"));
        var r = await CreateController().GetCertificate(id);
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCertificate_NotFound_Returns404()
    {
        _certSvc.Setup(s => s.GetCertificateByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Certificate?)null);
        var r = await CreateController().GetCertificate(Guid.NewGuid());
        r.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task VerifyCertificate_ReturnsOk()
    {
        var vr = new CertificateVerificationResult(true, "C-1", "U", "C", DateTime.UtcNow, null, CertificateStatus.Active, "Valid");
        _certSvc.Setup(s => s.VerifyCertificateAsync("C-1")).ReturnsAsync(Result.Success(vr));
        var r = await CreateController().VerifyCertificate("C-1");
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCourseCertificates_ReturnsOk()
    {
        var cId = Guid.NewGuid(); var tid = Guid.NewGuid();
        _certSvc.Setup(s => s.GetCourseCertificatesAsync(cId, tid)).ReturnsAsync(new List<Certificate>());
        var r = await CreateController(tenantId: tid).GetCourseCertificates(cId);
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetExpiringCertificates_ReturnsOk()
    {
        _certSvc.Setup(s => s.GetExpiringCertificatesAsync(30)).ReturnsAsync(new List<Certificate>());
        var r = await CreateController().GetExpiringCertificates(30);
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task IssueCertificate_Success_Returns201()
    {
        var tid = Guid.NewGuid();
        var req = new IssueCertificateRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var certificate = Certificate.Issue(req.TemplateId, req.EnrollmentId, req.UserId, req.CourseId, "U", "C");
        _sender.Setup(s => s.Send(
                It.Is<IssueCertificateEndpointCommand>(command =>
                    command.TemplateId == req.TemplateId &&
                    command.EnrollmentId == req.EnrollmentId &&
                    command.UserId == req.UserId &&
                    command.CourseId == req.CourseId &&
                    command.TenantId == tid),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(certificate));
        var r = await CreateController(tenantId: tid).IssueCertificate(req);
        r.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task RevokeCertificate_Success_Returns204()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(
                It.Is<RevokeCertificateEndpointCommand>(command => command.CertificateId == id && command.Reason == "V"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var r = await CreateController().RevokeCertificate(id, new RevokeCertificateRequest("V"));
        r.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateCertificateTemplate_Success_ReturnsUpdatedTemplate()
    {
        var template = CertificateTemplate.Create(Guid.NewGuid(), "Original", "<h1>Original</h1>");
        var request = new UpdateCertificateTemplateRequest(
            "Completion",
            "Awarded after completion",
            "<main>{{recipientName}}</main>",
            "main { color: navy; }",
            true,
            true);
        template.Update(
            request.Name,
            request.Description,
            request.TemplateHtml,
            request.TemplateStyles,
            request.IsActive);
        template.SetDefault(request.IsDefault);
        _sender.Setup(sender => sender.Send(
                It.Is<UpdateCertificateTemplateEndpointCommand>(command =>
                    command.TemplateId == template.Id &&
                    command.Name == request.Name &&
                    command.Description == request.Description &&
                    command.TemplateHtml == request.TemplateHtml &&
                    command.TemplateStyles == request.TemplateStyles &&
                    command.IsDefault == request.IsDefault &&
                    command.IsActive == request.IsActive),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(template));

        var result = await CreateController().UpdateCertificateTemplate(template.Id, request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<CertificateTemplateDetailDto>()
            .Which.Name.Should().Be("Completion");
        _sender.Verify(sender => sender.Send(
            It.IsAny<UpdateCertificateTemplateEndpointCommand>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCertificateTemplate_MissingTemplate_Returns404()
    {
        var templateId = Guid.NewGuid();
        _sender.Setup(sender => sender.Send(
                It.Is<UpdateCertificateTemplateEndpointCommand>(command => command.TemplateId == templateId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<CertificateTemplate>(
                Error.NotFound("CertificateTemplate", "Certificate template not found")));

        var result = await CreateController().UpdateCertificateTemplate(
            templateId,
            new UpdateCertificateTemplateRequest("Completion", null, "<main>Certificate</main>", null, false, true));

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }
}
