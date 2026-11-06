using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using GameGuild.API.Core.Compliance;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Compliance.Consent;
using GameGuild.CQRS;
using GameGuild.Compliance.FERPA;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.API.IntegrationTests;

/// <summary>
///     Compliance Consent/FERPA fix package (issues #1219/#1220) verified through the
///     real pipeline: module DI wiring (N1), re-grant upsert against the live unique
///     index (N2), statutory deadline expiry via the sweeper operation (N3),
///     server-side processor resolution (N4), FERPA ownership checks (N5) and the
///     directory-information opt-out enforcement (N6/spec).
/// </summary>
[Collection(ApiPostgreSqlCollection.Name)]
public sealed class ComplianceConsentFerpaPostgreSqlHttpTests(ApiPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private const string ConsentRoute = "/api/compliance/consent";
    private const string FerpaRoute = "/api/compliance/ferpa";

    [Fact]
    public async Task Consent_module_is_wired_and_endpoints_answer_through_ISender()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var admin = fixture.CreateAuthenticatedClient(adminId, tenantId, isSystemAdmin: true);
        using var self = fixture.CreateAuthenticatedClient(userId, tenantId);

        // N1: the host must resolve IConsentService behind the CQRS pipeline — before
        // AddConsentModule was registered every consent endpoint 500ed at DI resolution.
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var activePolicies = await sender.Send(new GetActivePoliciesQuery(tenantId));
        activePolicies.Should().NotBeNull();

        var policyId = await (await admin.PostAsJsonAsync($"{ConsentRoute}/policies", new
        {
            name = "Privacy",
            policyType = "PrivacyPolicy",
            isMandatory = true,
            description = "integration"
        })).Content.ReadFromJsonAsync<Guid>();
        policyId.Should().NotBeEmpty();

        var version = await (await admin.PostAsJsonAsync($"{ConsentRoute}/policies/{policyId}/versions", new
        {
            versionNumber = "1.0",
            content = "notice",
            contentType = "Markdown"
        })).Content.ReadFromJsonAsync<PolicyVersionDto>(JsonOptions);
        version.Should().NotBeNull();

        var granted = await (await self.PostAsJsonAsync($"{ConsentRoute}/grant", new
        {
            userId,
            policyVersionId = version!.Id,
            consentMethod = "integration"
        })).Content.ReadFromJsonAsync<UserConsentDto>(JsonOptions);
        granted!.IsGranted.Should().BeTrue();

        var listed = await self.GetFromJsonAsync<List<UserConsentDto>>($"{ConsentRoute}/users/{userId}", JsonOptions);
        listed.Should().ContainSingle().Which.Id.Should().Be(granted.Id);
    }

    [Fact]
    public async Task Grant_revoke_regrant_roundtrips_on_the_live_unique_index()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var admin = fixture.CreateAuthenticatedClient(adminId, tenantId, isSystemAdmin: true);
        using var self = fixture.CreateAuthenticatedClient(userId, tenantId);

        var policyId = await (await admin.PostAsJsonAsync($"{ConsentRoute}/policies", new
        {
            name = "Roundtrip",
            policyType = "MarketingConsent",
            isMandatory = false
        })).Content.ReadFromJsonAsync<Guid>();
        var version = await (await admin.PostAsJsonAsync($"{ConsentRoute}/policies/{policyId}/versions", new
        {
            versionNumber = "1.0",
            content = "notice"
        })).Content.ReadFromJsonAsync<PolicyVersionDto>(JsonOptions);

        var granted = await (await self.PostAsJsonAsync($"{ConsentRoute}/grant", new { userId, policyVersionId = version!.Id }))
            .Content.ReadFromJsonAsync<UserConsentDto>(JsonOptions);

        // N2: revoke → re-grant used to collide with the unique (UserId, PolicyVersionId)
        // index and 500; it must now reactivate the same row.
        var revoke = await self.PostAsJsonAsync($"{ConsentRoute}/revoke", new { userId, policyVersionId = version.Id });
        revoke.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var regrantedResponse = await self.PostAsJsonAsync($"{ConsentRoute}/grant", new
        {
            userId,
            policyVersionId = version.Id,
            consentMethod = "settings"
        });
        regrantedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var regranted = await regrantedResponse.Content.ReadFromJsonAsync<UserConsentDto>(JsonOptions);
        regranted!.Id.Should().Be(granted!.Id);
        regranted.IsGranted.Should().BeTrue();
        regranted.ConsentRevokedAt.Should().BeNull();
        regranted.ConsentMethod.Should().Be("settings");

        // A second full cycle and an idempotent repeat must both stay on the same row.
        await self.PostAsJsonAsync($"{ConsentRoute}/revoke", new { userId, policyVersionId = version.Id });
        var secondCycle = await (await self.PostAsJsonAsync($"{ConsentRoute}/grant", new { userId, policyVersionId = version.Id }))
            .Content.ReadFromJsonAsync<UserConsentDto>(JsonOptions);
        secondCycle!.Id.Should().Be(granted.Id);
        var idempotent = await (await self.PostAsJsonAsync($"{ConsentRoute}/grant", new { userId, policyVersionId = version.Id }))
            .Content.ReadFromJsonAsync<UserConsentDto>(JsonOptions);
        idempotent!.Id.Should().Be(granted.Id);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var rows = await db.Set<UserConsent>().AsNoTracking().Where(consent => consent.UserId == userId).ToListAsync();
        rows.Should().ContainSingle().Which.IsGranted.Should().BeTrue();
    }

    [Fact]
    public async Task DSR_processor_attribution_is_resolved_server_side()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var forgedProcessor = Guid.NewGuid();
        using var admin = fixture.CreateAuthenticatedClient(adminId, tenantId, isSystemAdmin: true);
        using var self = fixture.CreateAuthenticatedClient(userId, tenantId);
        using var member = fixture.CreateAuthenticatedClient(Guid.NewGuid(), tenantId);

        var submitted = await (await self.PostAsJsonAsync($"{ConsentRoute}/data-subject-requests", new
        {
            userId,
            requestType = "Access",
            description = "export my data"
        })).Content.ReadFromJsonAsync<DataSubjectRequestDto>(JsonOptions);
        submitted!.Status.Should().Be(DataSubjectRequestStatus.Pending);

        // N4: a body-supplied ProcessedByUserId must be ignored — attribution comes
        // from the authenticated actor (a non-admin is also rejected outright).
        var memberAttempt = await member.PostAsJsonAsync($"{ConsentRoute}/data-subject-requests/{submitted.Id}/process", new
        {
            processedByUserId = forgedProcessor,
            notes = "forged"
        });
        memberAttempt.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var processedResponse = await admin.PostAsJsonAsync($"{ConsentRoute}/data-subject-requests/{submitted.Id}/process", new
        {
            processedByUserId = forgedProcessor,
            notes = "done"
        });
        processedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var processed = await processedResponse.Content.ReadFromJsonAsync<DataSubjectRequestDto>(JsonOptions);
        processed!.Status.Should().Be(DataSubjectRequestStatus.Completed);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = await db.Set<DataSubjectRequest>().AsNoTracking().SingleAsync(request => request.Id == submitted.Id);
        stored.ProcessedByUserId.Should().Be(adminId, "the processor is resolved server-side from the authenticated actor");
        stored.ProcessedByUserId.Should().NotBe(forgedProcessor);

        // N4: re-processing a terminal request no longer overwrites the attribution.
        var reprocessed = await admin.PostAsJsonAsync($"{ConsentRoute}/data-subject-requests/{submitted.Id}/process", new { notes = "again" });
        reprocessed.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var after = await db.Set<DataSubjectRequest>().AsNoTracking().SingleAsync(request => request.Id == submitted.Id);
        after.ProcessedByUserId.Should().Be(adminId);

        // N5 shape for the consent controller: another member may not read someone
        // else's consent state, and the pending queue is administrator-only.
        var foreignRead = await member.GetAsync($"{ConsentRoute}/users/{userId}");
        foreignRead.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var selfRead = await self.GetAsync($"{ConsentRoute}/users/{userId}");
        selfRead.StatusCode.Should().Be(HttpStatusCode.OK);
        var memberPending = await member.GetAsync($"{ConsentRoute}/data-subject-requests/pending");
        memberPending.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var adminPending = await admin.GetAsync($"{ConsentRoute}/data-subject-requests/pending");
        adminPending.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ferpa_reads_and_writes_enforce_ownership()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        using var admin = fixture.CreateAuthenticatedClient(adminId, tenantId, isSystemAdmin: true);
        using var student = fixture.CreateAuthenticatedClient(studentId, tenantId);
        using var member = fixture.CreateAuthenticatedClient(memberId, tenantId);

        var registered = await (await admin.PostAsJsonAsync($"{FerpaRoute}/records", new
        {
            studentUserId = studentId,
            recordKind = "Grade",
            externalRecordId = "grade-1",
            title = "Final grade",
            protectionLevel = "EducationRecord",
            isDirectoryInformation = true,
            tenantId
        })).Content.ReadFromJsonAsync<FerpaEducationRecordDto>(JsonOptions);
        registered!.StudentUserId.Should().Be(studentId);

        // N5: an unrelated member gets 403 on every student-scoped read/write surface.
        (await member.GetAsync($"{FerpaRoute}/students/{studentId}/records")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await member.GetAsync($"{FerpaRoute}/students/{studentId}/directory-information")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await member.GetAsync($"{FerpaRoute}/students/{studentId}/consents")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await member.GetAsync($"{FerpaRoute}/students/{studentId}/disclosures")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var memberRegistration = await member.PostAsJsonAsync($"{FerpaRoute}/records", new
        {
            studentUserId = Guid.NewGuid(),
            recordKind = "Grade",
            externalRecordId = "grade-2",
            title = "Should not persist"
        });
        memberRegistration.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Self and system administrator still reach the data through the pipeline.
        (await student.GetAsync($"{FerpaRoute}/students/{studentId}/records")).StatusCode.Should().Be(HttpStatusCode.OK);
        var adminView = await admin.GetFromJsonAsync<List<FerpaEducationRecordDto>>($"{FerpaRoute}/students/{studentId}/records", JsonOptions);
        adminView!.Select(record => record.Id).Should().Contain(registered.Id);

        // Non-administrator directory-policy mutation is rejected; administrators may write it.
        var memberPolicy = await member.PutAsJsonAsync($"{FerpaRoute}/directory-policy", new
        {
            tenantId,
            allowedFieldsJson = "[\"name\"]",
            optOutEnabled = true
        });
        memberPolicy.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Inspection completion is administrative: a member naming itself as the
        // processor gets 403; the administrator's completion attributes server-side.
        var inspection = await (await student.PostAsJsonAsync($"{FerpaRoute}/inspection-requests", new
        {
            studentUserId = studentId,
            requestedByUserId = memberId,
            deadline = DateTime.UtcNow.AddDays(45).ToString("O"),
            description = "transcript"
        })).Content.ReadFromJsonAsync<FerpaInspectionRequestDto>(JsonOptions);
        inspection!.RequestedByUserId.Should().Be(studentId, "the requester is resolved server-side from the authenticated actor");
        inspection.Status.Should().Be(FerpaRequestStatus.Pending);

        var forgedProcessor = Guid.NewGuid();
        var memberCompletion = await member.PostAsJsonAsync($"{FerpaRoute}/inspection-requests/{inspection.Id}/complete", new
        {
            processedByUserId = forgedProcessor,
            approved = true
        });
        memberCompletion.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var adminCompletion = await admin.PostAsJsonAsync($"{FerpaRoute}/inspection-requests/{inspection.Id}/complete", new
        {
            processedByUserId = forgedProcessor,
            approved = true,
            notes = "released"
        });
        adminCompletion.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storedInspection = await db.Set<FerpaInspectionRequest>().AsNoTracking().SingleAsync(request => request.Id == inspection.Id);
        storedInspection.ProcessedByUserId.Should().Be(adminId);
        storedInspection.ProcessedByUserId.Should().NotBe(forgedProcessor);
        storedInspection.Status.Should().Be(FerpaRequestStatus.Completed);
    }

    [Fact]
    public async Task Ferpa_directory_information_opt_out_is_enforced()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        using var admin = fixture.CreateAuthenticatedClient(adminId, tenantId, isSystemAdmin: true);
        using var student = fixture.CreateAuthenticatedClient(studentId, tenantId);

        await admin.PostAsJsonAsync($"{FerpaRoute}/records", new
        {
            studentUserId = studentId,
            recordKind = "Attendance",
            externalRecordId = "attendance-1",
            title = "Attendance 2026",
            protectionLevel = "DirectoryInformation",
            isDirectoryInformation = true,
            tenantId
        });

        // Regime closed: no opt-out policy → the directory-information basis is denied.
        var closedRegime = await admin.PostAsJsonAsync($"{FerpaRoute}/disclosures", new
        {
            studentUserId = studentId,
            disclosedByUserId = Guid.NewGuid(),
            recipient = "Yearbook",
            basis = "DirectoryInformation",
            purpose = "Yearbook print",
            scope = "attendance",
            recordIdsJson = "[]",
            disclosedAt = DateTime.UtcNow.ToString("O")
        });
        closedRegime.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // The directory-information read is equally filtered under a closed regime.
        var closedRead = await student.GetFromJsonAsync<List<FerpaEducationRecordDto>>($"{FerpaRoute}/students/{studentId}/directory-information", JsonOptions);
        closedRead.Should().BeEmpty();

        // Regime open: opt-out enabled → directory information is disclosed and logged.
        await admin.PutAsJsonAsync($"{FerpaRoute}/directory-policy", new
        {
            tenantId,
            allowedFieldsJson = "[\"name\",\"attendance\"]",
            optOutEnabled = true
        });
        var openRead = await student.GetFromJsonAsync<List<FerpaEducationRecordDto>>($"{FerpaRoute}/students/{studentId}/directory-information", JsonOptions);
        openRead!.Should().ContainSingle().Which.ExternalRecordId.Should().Be("attendance-1");

        var openDisclosure = await admin.PostAsJsonAsync($"{FerpaRoute}/disclosures", new
        {
            studentUserId = studentId,
            disclosedByUserId = Guid.NewGuid(),
            recipient = "Yearbook",
            basis = "DirectoryInformation",
            purpose = "Yearbook print",
            scope = "attendance",
            recordIdsJson = "[]",
            disclosedAt = DateTime.UtcNow.ToString("O")
        });
        openDisclosure.StatusCode.Should().Be(HttpStatusCode.OK);

        // Closing the regime again hides the directory information.
        await admin.PutAsJsonAsync($"{FerpaRoute}/directory-policy", new
        {
            tenantId,
            allowedFieldsJson = "[]",
            optOutEnabled = false
        });
        var reclosedRead = await student.GetFromJsonAsync<List<FerpaEducationRecordDto>>($"{FerpaRoute}/students/{studentId}/directory-information", JsonOptions);
        reclosedRead.Should().BeEmpty();
    }

    [Fact]
    public async Task Deadline_sweeper_expires_overdue_DSR_and_FERPA_requests()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        Guid dsrId;
        Guid inspectionId;
        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var dsr = new DataSubjectRequest
            {
                UserId = userId,
                RequestType = DataSubjectRequestType.Erasure,
                Status = DataSubjectRequestStatus.Pending,
                Deadline = DateTime.UtcNow.AddDays(-2)
            };
            var inspection = new FerpaInspectionRequest
            {
                StudentUserId = studentId,
                RequestedByUserId = studentId,
                Status = FerpaRequestStatus.Pending,
                Deadline = DateTime.UtcNow.AddDays(-3)
            };
            db.Set<DataSubjectRequest>().Add(dsr);
            db.Set<FerpaInspectionRequest>().Add(inspection);
            await db.SaveChangesAsync();
            dsrId = dsr.Id;
            inspectionId = inspection.Id;
        }

        // N3: run the exact transition the ComplianceDeadlineExpiryWorker performs
        // periodically — Active → Expired becomes reachable through the pipeline.
        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var operation = scope.ServiceProvider.GetRequiredService<IComplianceDeadlineExpiryOperation>();
            var result = await operation.RunAsync();
            result.DataSubjectRequestsExpired.Should().BeGreaterThanOrEqualTo(1);
            result.FerpaInspectionRequestsExpired.Should().BeGreaterThanOrEqualTo(1);
        }

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Set<DataSubjectRequest>().AsNoTracking().SingleAsync(request => request.Id == dsrId))
                .Status.Should().Be(DataSubjectRequestStatus.Expired);
            (await db.Set<FerpaInspectionRequest>().AsNoTracking().SingleAsync(request => request.Id == inspectionId))
                .Status.Should().Be(FerpaRequestStatus.Expired);
        }

        // The sweep is idempotent — nothing transitions twice.
        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var operation = scope.ServiceProvider.GetRequiredService<IComplianceDeadlineExpiryOperation>();
            var second = await operation.RunAsync();
            second.DataSubjectRequestsExpired.Should().Be(0);
            second.FerpaInspectionRequestsExpired.Should().Be(0);
        }

        // Expired DSRs leave the pending queue and can no longer be processed.
        using var admin = fixture.CreateAuthenticatedClient(Guid.NewGuid(), tenantId, isSystemAdmin: true);
        var pending = await admin.GetFromJsonAsync<List<DataSubjectRequestDto>>($"{ConsentRoute}/data-subject-requests/pending", JsonOptions);
        pending!.Should().NotContain(request => request.Id == dsrId);
        var processExpired = await admin.PostAsJsonAsync($"{ConsentRoute}/data-subject-requests/{dsrId}/process", new { notes = "late" });
        processExpired.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
