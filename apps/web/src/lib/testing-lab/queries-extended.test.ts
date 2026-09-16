import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getToken: vi.fn(),
  createServerClient: vi.fn(),
  client: { request: vi.fn() },
  requests: {
    getTestingRequestsForGetTestingRequests: vi.fn(),
    getTestingRequestsForGetTestingRequestsById: vi.fn(),
  },
  sessions: {
    getTestingSessionsForGetTestingSessions: vi.fn(),
    getTestingPublicSessions: vi.fn(),
    getTestingSessionsByRequest: vi.fn(),
    getTestingSessionsForGetTestingSessionsById: vi.fn(),
    getTestingSessionsProjects: vi.fn(),
  },
  locations: { getTestingLocationsForGetTestingLocations: vi.fn() },
  participants: {
    getTestingRequestsParticipants: vi.fn(),
    getTestingSessionsRegistrations: vi.fn(),
    getTestingSessionsWaitlist: vi.fn(),
  },
  feedback: {
    getTestingFeedback: vi.fn(),
    getTestingRequestsFeedback: vi.fn(),
  },
  analytics: {
    getTestingAnalytics: vi.fn(),
    getTestingAnalyticsExport: vi.fn(),
  },
  settings: { getApiTestingLabSettings: vi.fn() },
  permissions: { getApiTestingLabPermissionsRoleTemplates: vi.fn() },
  projects: {
    getProjectsForGetProjects: vi.fn(),
    getProjectsForGetProjectsById: vi.fn(),
  },
}));

vi.mock("@/auth", () => ({
  auth: mocks.auth,
  getToken: mocks.getToken,
}));

vi.mock("@game-guild/client", () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    TestingLabTestingRequestsModule: vi.fn(function TestingLabTestingRequestsModule() { return mocks.requests; }),
    TestingLabTestingSessionsModule: vi.fn(function TestingLabTestingSessionsModule() { return mocks.sessions; }),
    TestingLabTestingLocationsModule: vi.fn(function TestingLabTestingLocationsModule() { return mocks.locations; }),
    TestingLabTestingParticipantsModule: vi.fn(function TestingLabTestingParticipantsModule() { return mocks.participants; }),
    TestingLabTestingFeedbackModule: vi.fn(function TestingLabTestingFeedbackModule() { return mocks.feedback; }),
    TestingLabTestingAnalyticsModule: vi.fn(function TestingLabTestingAnalyticsModule() { return mocks.analytics; }),
    TestingLabSettingsModule: vi.fn(function TestingLabSettingsModule() { return mocks.settings; }),
    TestingLabPermissionModule: vi.fn(function TestingLabPermissionModule() { return mocks.permissions; }),
    ProjectsModule: vi.fn(function ProjectsModule() { return mocks.projects; }),
  },
}));

vi.mock("./testing-request-detail", () => ({
  mapTestingRequestDetail: (value: { id?: string } | null | undefined) =>
    value?.id ? value : null,
}));

import {
  countAvailableTesterSlots,
  filterTestingLabLocations,
  getPublicTestingLabDirectory,
  getTestingFeedbackDirectory,
  getTestingLabAdministration,
  getTestingLabAnalytics,
  getTestingLabAnalyticsCsv,
  getTestingLabDashboard,
  getTestingLabLocations,
  getTestingLabProjectDetail,
  getTestingLabSettings,
  getTestingProjectOptions,
  getTestingProjectVersionOptions,
  getTestingRequestDetail,
  getTestingSessionDetail,
  normalizeTestingLocationStatus,
  normalizeTestingRequestStatus,
  normalizeTestingSessionStatus,
} from "./queries";

const ok = <T,>(data: T) => ({ ok: true, data });
const failed = (status: number | undefined, message: string) => ({
  ok: false,
  error: { status, message },
});

describe("extended Testing Lab queries", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({ tenantId: "tenant-1" });
    mocks.getToken.mockResolvedValue("token-1");
    mocks.createServerClient.mockReturnValue(mocks.client);
    mocks.client.request.mockResolvedValue(ok([]));
    mocks.requests.getTestingRequestsForGetTestingRequests.mockResolvedValue(ok([]));
    mocks.requests.getTestingRequestsForGetTestingRequestsById.mockResolvedValue(ok({ id: "request-1", title: "Request", status: "Open" }));
    mocks.sessions.getTestingSessionsForGetTestingSessions.mockResolvedValue(ok([]));
    mocks.sessions.getTestingPublicSessions.mockResolvedValue(ok([]));
    mocks.sessions.getTestingSessionsByRequest.mockResolvedValue(ok([]));
    mocks.sessions.getTestingSessionsForGetTestingSessionsById.mockResolvedValue(ok({ id: "session-1", sessionName: "Session", status: "Scheduled" }));
    mocks.sessions.getTestingSessionsProjects.mockResolvedValue(ok([]));
    mocks.locations.getTestingLocationsForGetTestingLocations.mockResolvedValue(ok([]));
    mocks.participants.getTestingRequestsParticipants.mockResolvedValue(ok([]));
    mocks.participants.getTestingSessionsRegistrations.mockResolvedValue(ok([]));
    mocks.participants.getTestingSessionsWaitlist.mockResolvedValue(ok([]));
    mocks.feedback.getTestingFeedback.mockResolvedValue(ok({ items: [], totalCount: 0, skip: 0, take: 20 }));
    mocks.feedback.getTestingRequestsFeedback.mockResolvedValue(ok([]));
    mocks.analytics.getTestingAnalytics.mockResolvedValue(ok(null));
    mocks.analytics.getTestingAnalyticsExport.mockResolvedValue(ok(null));
    mocks.settings.getApiTestingLabSettings.mockResolvedValue(ok({ allowPublicAccess: true }));
    mocks.permissions.getApiTestingLabPermissionsRoleTemplates.mockResolvedValue(ok([]));
    mocks.projects.getProjectsForGetProjects.mockResolvedValue(ok([]));
    mocks.projects.getProjectsForGetProjectsById.mockResolvedValue(failed(404, "Not found"));
  });

  afterEach(() => vi.unstubAllEnvs());

  it("maps complete and malformed dashboard records while preserving partial failures", async () => {
    mocks.requests.getTestingRequestsForGetTestingRequests.mockResolvedValueOnce(ok([
      { id: "request-1", title: "Request", status: "Open" },
      { title: "Missing id", status: "Open" },
    ]));
    const location = {
      id: "location-1",
      name: "Campus",
      description: "Main lab",
      address: "One Street",
      equipmentAvailable: "PC",
      city: "São Paulo",
      state: "SP",
      postalCode: "01000-000",
      country: "Brazil",
      contactEmail: "lab@example.com",
      contactPhone: "+55 11",
      isVirtual: false,
      virtualUrl: null,
      maxTestersCapacity: 20,
      maxProjectsCapacity: 4,
      capacity: 20,
      status: undefined,
      isDeleted: false,
    };
    mocks.locations.getTestingLocationsForGetTestingLocations.mockResolvedValueOnce(ok([location, { name: "Missing id" }]));
    mocks.sessions.getTestingSessionsForGetTestingSessions.mockResolvedValueOnce(ok([
      {
        id: "session-1",
        sessionName: "Campus session",
        status: "Scheduled",
        location,
        testingRequest: { id: "request-1", title: "Request", status: "Open" },
      },
      { sessionName: "Missing id", status: "Scheduled" },
    ]));
    mocks.sessions.getTestingPublicSessions.mockRejectedValueOnce({ message: "Public offline" });

    const result = await getTestingLabDashboard();

    expect(result.requests).toHaveLength(1);
    expect(result.locations[0]).toMatchObject({ id: "location-1", status: "Inactive" });
    expect(result.sessions[0]).toMatchObject({
      id: "session-1",
      location: { id: "location-1" },
      testingRequest: { id: "request-1" },
    });
    expect(result.publicSessions).toEqual([]);
    expect(result.accessIssues).toEqual(["Public testing sessions failed: Public offline"]);
  });

  it("defaults every failed dashboard collection independently", async () => {
    mocks.requests.getTestingRequestsForGetTestingRequests.mockResolvedValueOnce(failed(403, "Requests"));
    mocks.sessions.getTestingSessionsForGetTestingSessions.mockResolvedValueOnce(failed(403, "Sessions"));
    mocks.locations.getTestingLocationsForGetTestingLocations.mockResolvedValueOnce(failed(403, "Locations"));
    mocks.sessions.getTestingPublicSessions.mockResolvedValueOnce(failed(403, "Public"));

    const result = await getTestingLabDashboard();
    expect(result).toMatchObject({ requests: [], sessions: [], locations: [], publicSessions: [] });
    expect(result.accessIssues).toHaveLength(4);
  });

  it("returns a failed location directory without fabricated records", async () => {
    mocks.locations.getTestingLocationsForGetTestingLocations.mockResolvedValueOnce(
      failed(undefined, "Unavailable"),
    );
    await expect(getTestingLabLocations()).resolves.toEqual({
      locations: [],
      accessIssues: ["Testing locations returned an error: Unavailable"],
    });
  });

  it("binds token and optional tenant resolution to the generated client", async () => {
    await getTestingLabSettings();
    const clientOptions = mocks.createServerClient.mock.calls[0]![0];
    await expect(clientOptions.auth.getAccessToken()).resolves.toBe("token-1");
    await expect(clientOptions.tenant.getTenantId()).resolves.toBe("tenant-1");

    mocks.auth.mockRejectedValueOnce(new Error("Auth offline"));
    await expect(clientOptions.tenant.getTenantId()).resolves.toBeNull();
  });

  it("uses a safe unknown message for non-object operation failures", async () => {
    mocks.settings.getApiTestingLabSettings.mockRejectedValueOnce("connection closed");
    await expect(getTestingLabSettings()).resolves.toEqual({
      settings: null,
      accessIssues: ["Testing Lab settings failed: Unknown error"],
    });
  });

  it("rejects empty structured failure messages", async () => {
    mocks.settings.getApiTestingLabSettings.mockRejectedValueOnce({ message: "   " });
    await expect(getTestingLabSettings()).resolves.toMatchObject({
      settings: null,
      accessIssues: ["Testing Lab settings failed: Unknown error"],
    });
  });

  it("keeps only published public projects and valid public sessions", async () => {
    mocks.sessions.getTestingPublicSessions.mockResolvedValueOnce(ok([
      { id: "session-1", sessionName: "Public", status: "Scheduled" },
      { sessionName: "Invalid", status: "Scheduled" },
    ]));
    mocks.projects.getProjectsForGetProjects.mockResolvedValueOnce(ok([
      { id: "published", title: "Published", status: "Published" },
      { id: "numeric", slug: "numeric-project", status: 1 },
      { id: "draft", title: "Draft", status: "Draft" },
      { title: "Invalid", status: "Published" },
    ]));

    const result = await getPublicTestingLabDirectory();
    expect(result.sessions).toHaveLength(1);
    expect(result.projects.map((project) => project.id)).toEqual(["published", "numeric"]);
  });

  it("defaults both public collections when their operations fail", async () => {
    mocks.sessions.getTestingPublicSessions.mockResolvedValueOnce(failed(503, "Sessions"));
    mocks.projects.getProjectsForGetProjects.mockResolvedValueOnce(failed(503, "Projects"));
    const result = await getPublicTestingLabDirectory();
    expect(result.sessions).toEqual([]);
    expect(result.projects).toEqual([]);
    expect(result.accessIssues).toHaveLength(2);
  });

  it.each([[null], [new Error("Auth offline")]])(
    "does not query project options without tenant context",
    async (authResult) => {
      if (authResult instanceof Error) mocks.auth.mockRejectedValueOnce(authResult);
      else mocks.auth.mockResolvedValueOnce(authResult);
      await expect(getTestingProjectOptions()).resolves.toEqual([]);
      expect(mocks.projects.getProjectsForGetProjects).not.toHaveBeenCalled();
    },
  );

  it("maps only current-tenant project options with title fallbacks", async () => {
    mocks.projects.getProjectsForGetProjects.mockResolvedValueOnce(ok([
      { id: "title", tenantId: "tenant-1", title: "Title", status: "Published" },
      { id: "slug", tenantId: "tenant-1", slug: "slug-title", status: "Draft" },
      { id: "id-only", tenantId: "tenant-1" },
      { tenantId: "tenant-1", title: "Invalid" },
      { id: "other", tenantId: "tenant-2", title: "Other" },
    ]));
    expect((await getTestingProjectOptions()).map((project) => project.title)).toEqual([
      "Title",
      "slug-title",
      "id-only",
    ]);
  });

  it("returns no project options when the project operation fails", async () => {
    mocks.projects.getProjectsForGetProjects.mockResolvedValueOnce(failed(403, "Forbidden"));
    await expect(getTestingProjectOptions()).resolves.toEqual([]);
  });

  it("loads accessible versions only for a tenant and handles client failures", async () => {
    const versions = [{ id: "version-1", projectId: "project-1", projectTitle: "Project", versionNumber: "1.0", status: "ReadyForTesting" }];
    mocks.client.request.mockResolvedValueOnce(ok(versions));
    await expect(getTestingProjectVersionOptions()).resolves.toEqual(versions);
    expect(mocks.client.request).toHaveBeenCalledWith({
      method: "GET",
      path: "/v1/projects/accessible-versions",
      params: { take: 100 },
      requiresAuth: true,
    });

    mocks.client.request.mockResolvedValueOnce(failed(403, "Forbidden"));
    await expect(getTestingProjectVersionOptions()).resolves.toEqual([]);
    mocks.auth.mockResolvedValueOnce(null);
    await expect(getTestingProjectVersionOptions()).resolves.toEqual([]);
    mocks.auth.mockRejectedValueOnce(new Error("Auth offline"));
    await expect(getTestingProjectVersionOptions()).resolves.toEqual([]);
  });

  it("normalizes feedback filters, pagination, and response fallbacks", async () => {
    mocks.feedback.getTestingFeedback.mockResolvedValueOnce(ok({
      items: [{ id: "feedback-1" }],
      totalCount: 1,
    }));
    const result = await getTestingFeedbackDirectory({
      q: "  useful  ",
      source: "request",
      eventId: "event-1",
      requestId: "request-1",
      userId: "user-1",
      reported: true,
      quality: "High",
      skip: -5,
      take: 500,
    });
    expect(result).toMatchObject({ items: [{ id: "feedback-1" }], totalCount: 1, skip: -5, take: 500 });
    expect(mocks.feedback.getTestingFeedback).toHaveBeenCalledWith({
      Search: "useful",
      Source: "Request",
      EventId: "event-1",
      RequestId: "request-1",
      UserId: "user-1",
      Reported: true,
      Quality: "High",
      Skip: 0,
      Take: 100,
    });

    mocks.feedback.getTestingFeedback.mockResolvedValueOnce(failed(403, "Forbidden"));
    await expect(getTestingFeedbackDirectory({ source: "all" })).resolves.toMatchObject({
      items: [],
      totalCount: 0,
      skip: 0,
      take: 20,
      accessIssues: ["Testing feedback returned 403: Forbidden"],
    });
  });

  it("uses API feedback pagination and event source when supplied", async () => {
    mocks.feedback.getTestingFeedback.mockResolvedValueOnce(ok({
      items: [],
      totalCount: 0,
      skip: 40,
      take: 10,
    }));
    const result = await getTestingFeedbackDirectory({ source: "event" });
    expect(result).toMatchObject({ skip: 40, take: 10 });
    expect(mocks.feedback.getTestingFeedback).toHaveBeenCalledWith(
      expect.objectContaining({ Search: undefined, Source: "Event" }),
    );
  });

  it("loads request detail collections and reports independent failures", async () => {
    mocks.sessions.getTestingSessionsByRequest.mockResolvedValueOnce(ok([
      { id: "session-1", sessionName: "Session", status: "Scheduled" },
      { sessionName: "Invalid", status: "Scheduled" },
    ]));
    mocks.participants.getTestingRequestsParticipants.mockResolvedValueOnce(failed(403, "Forbidden"));
    mocks.feedback.getTestingRequestsFeedback.mockRejectedValueOnce(new Error("Feedback offline"));

    const result = await getTestingRequestDetail("request-1");
    expect(result.request).toMatchObject({ id: "request-1" });
    expect(result.sessions).toHaveLength(1);
    expect(result.participants).toEqual([]);
    expect(result.feedback).toEqual([]);
    expect(result.accessIssues).toEqual([
      "Request participants returned 403: Forbidden",
      "Request feedback failed: Feedback offline",
    ]);
  });

  it("returns an empty request detail when every operation fails", async () => {
    mocks.requests.getTestingRequestsForGetTestingRequestsById.mockResolvedValueOnce(failed(404, "Missing"));
    mocks.sessions.getTestingSessionsByRequest.mockResolvedValueOnce(failed(404, "Sessions"));
    mocks.participants.getTestingRequestsParticipants.mockResolvedValueOnce(failed(404, "Participants"));
    mocks.feedback.getTestingRequestsFeedback.mockResolvedValueOnce(failed(404, "Feedback"));
    const result = await getTestingRequestDetail("missing");
    expect(result).toMatchObject({ request: null, sessions: [], participants: [], feedback: [] });
    expect(result.accessIssues).toHaveLength(4);
  });

  it("resolves a project linked to a request and merges discovery issues", async () => {
    mocks.projects.getProjectsForGetProjectsById.mockResolvedValueOnce(ok({
      id: "project-1",
      slug: "project-slug",
      description: "Long description",
      downloadUrl: "https://example.com/build",
      status: "Published",
    }));
    mocks.requests.getTestingRequestsForGetTestingRequests.mockResolvedValueOnce(ok([
      { id: "request-1", projectVersion: { projectId: "project-1" } },
    ]));
    mocks.requests.getTestingRequestsForGetTestingRequestsById.mockResolvedValueOnce(ok({ id: "request-1", title: "Request", status: "Open" }));

    const result = await getTestingLabProjectDetail("project-1");
    expect(result.project).toMatchObject({
      id: "project-1",
      title: "project-slug",
      description: "Long description",
      downloadUrl: "https://example.com/build",
      developmentStatus: null,
    });
    expect(result.request).toMatchObject({ id: "request-1" });
    expect(result.accessIssues).toEqual([]);
  });

  it("supports a legacy request id and derives its shared project", async () => {
    mocks.projects.getProjectsForGetProjectsById.mockResolvedValueOnce(failed(404, "Not found"));
    const request = {
      id: "request-1",
      title: "Request",
      status: "Open",
      projectVersion: {
        project: { id: "project-1", slug: "project-slug", status: "Published" },
      },
    };
    mocks.requests.getTestingRequestsForGetTestingRequestsById.mockResolvedValue(ok(request));

    const result = await getTestingLabProjectDetail("request-1");
    expect(result.project).toEqual({
      id: "project-1",
      title: "project-slug",
      slug: "project-slug",
      status: "Published",
    });
    expect(result.request).toMatchObject({ id: "request-1" });
  });

  it("handles malformed shared projects and legacy requests without project context", async () => {
    mocks.projects.getProjectsForGetProjectsById.mockResolvedValueOnce(ok({ title: "Missing id" }));
    mocks.requests.getTestingRequestsForGetTestingRequests.mockResolvedValueOnce(ok([]));
    await expect(getTestingLabProjectDetail("malformed")).resolves.toMatchObject({
      project: null,
      request: null,
    });

    mocks.projects.getProjectsForGetProjectsById.mockResolvedValueOnce(failed(404, "Not found"));
    mocks.requests.getTestingRequestsForGetTestingRequestsById.mockResolvedValue(ok({
      id: "request-without-project",
      status: "Open",
    }));
    await expect(getTestingLabProjectDetail("request-without-project")).resolves.toMatchObject({
      project: null,
      request: { id: "request-without-project" },
    });
  });

  it("falls back to the project id and retains request-directory failures", async () => {
    mocks.projects.getProjectsForGetProjectsById.mockResolvedValueOnce(ok({ id: "id-only" }));
    mocks.requests.getTestingRequestsForGetTestingRequests.mockResolvedValueOnce(
      failed(503, "Directory offline"),
    );
    await expect(getTestingLabProjectDetail("id-only")).resolves.toMatchObject({
      project: { id: "id-only", title: "id-only" },
      request: null,
      accessIssues: ["Testing requests returned 503: Directory offline"],
    });
  });

  it("falls back to the shared project id in a legacy request", async () => {
    mocks.projects.getProjectsForGetProjectsById.mockResolvedValueOnce(failed(404, "Not found"));
    mocks.requests.getTestingRequestsForGetTestingRequestsById.mockResolvedValue(ok({
      id: "request-1",
      status: "Open",
      projectVersion: { project: { id: "project-id-only" } },
    }));
    await expect(getTestingLabProjectDetail("request-1")).resolves.toMatchObject({
      project: { id: "project-id-only", title: "project-id-only" },
    });
  });

  it("suppresses expected 404s but reports unexpected project lookup failures", async () => {
    mocks.projects.getProjectsForGetProjectsById.mockResolvedValueOnce(failed(500, "Project failed"));
    mocks.requests.getTestingRequestsForGetTestingRequestsById.mockResolvedValueOnce(failed(404, "Missing"));
    await expect(getTestingLabProjectDetail("missing")).resolves.toMatchObject({
      project: null,
      request: null,
      accessIssues: ["Project returned 500: Project failed"],
    });
  });

  it("loads session detail and preserves failed collections", async () => {
    mocks.participants.getTestingSessionsRegistrations.mockResolvedValueOnce(ok([{ id: "registration-1" }]));
    mocks.participants.getTestingSessionsWaitlist.mockResolvedValueOnce(failed(403, "Waitlist forbidden"));
    mocks.sessions.getTestingSessionsProjects.mockResolvedValueOnce(ok([{ id: "project-1" }]));
    const result = await getTestingSessionDetail("session-1");
    expect(result.session).toMatchObject({ id: "session-1" });
    expect(result.registrations).toEqual([{ id: "registration-1" }]);
    expect(result.waitlist).toEqual([]);
    expect(result.projects).toEqual([{ id: "project-1" }]);
    expect(result.accessIssues).toEqual(["Session waitlist returned 403: Waitlist forbidden"]);
  });

  it("returns empty session collections when all operations fail", async () => {
    mocks.sessions.getTestingSessionsForGetTestingSessionsById.mockResolvedValueOnce(failed(404, "Missing"));
    mocks.participants.getTestingSessionsRegistrations.mockResolvedValueOnce(failed(403, "Registrations"));
    mocks.participants.getTestingSessionsWaitlist.mockResolvedValueOnce(failed(403, "Waitlist"));
    mocks.sessions.getTestingSessionsProjects.mockResolvedValueOnce(failed(403, "Projects"));
    const result = await getTestingSessionDetail("missing");
    expect(result).toMatchObject({ session: null, registrations: [], waitlist: [], projects: [] });
    expect(result.accessIssues).toHaveLength(4);
  });

  it("loads settings and administration with partial authorization", async () => {
    await expect(getTestingLabSettings()).resolves.toEqual({
      settings: { allowPublicAccess: true },
      accessIssues: [],
    });
    mocks.permissions.getApiTestingLabPermissionsRoleTemplates.mockResolvedValueOnce(
      failed(403, "Roles forbidden"),
    );
    await expect(getTestingLabAdministration()).resolves.toEqual({
      settings: { allowPublicAccess: true },
      roles: [],
      accessIssues: ["Testing Lab roles returned 403: Roles forbidden"],
    });
  });

  it("preserves a settings failure while returning available role templates", async () => {
    mocks.settings.getApiTestingLabSettings.mockResolvedValueOnce(failed(503, "Settings offline"));
    mocks.permissions.getApiTestingLabPermissionsRoleTemplates.mockResolvedValueOnce(ok([{ id: "manager" }]));
    await expect(getTestingLabAdministration()).resolves.toEqual({
      settings: null,
      roles: [{ id: "manager" }],
      accessIssues: ["Testing Lab settings returned 503: Settings offline"],
    });
  });

  it("maps complete analytics, defaulting absent fields and rejecting invalid events", async () => {
    mocks.analytics.getTestingAnalytics.mockResolvedValueOnce(ok({
      fromDate: "2026-09-01T00:00:00Z",
      toDate: "2026-10-01T00:00:00Z",
      generatedAt: "2026-10-01T01:00:00Z",
      current: { events: 2, averageRating: 8.5, capacity: 20 },
      previous: { completedEvents: 1, recommendationRate: 75 },
      locations: { total: 3, active: 2 },
      trend: [{ date: "2026-09-01", events: 1 }, {}],
      events: [
        { eventId: "event-1", name: "Event", status: "Completed", mode: "Hybrid", startsAt: "2026-09-20", applications: 3 },
        { eventId: "event-2" },
        { name: "Invalid" },
      ],
    }));
    const result = await getTestingLabAnalytics({ includeComparison: true });
    expect(result.current).toMatchObject({ events: 2, averageRating: 8.5, capacity: 20, feedback: 0 });
    expect(result.previous).toMatchObject({ completedEvents: 1, recommendationRate: 75, events: 0 });
    expect(result.trend).toHaveLength(2);
    expect(result.events).toEqual([
      expect.objectContaining({ eventId: "event-1", name: "Event", status: "Completed", mode: "Hybrid" }),
      expect.objectContaining({ eventId: "event-2", name: "Untitled event", status: "Draft", mode: "Online" }),
    ]);
    expect(result.accessIssues).toEqual([]);
  });

  it("returns an empty analytics model and requested dates on failure", async () => {
    mocks.analytics.getTestingAnalytics.mockResolvedValueOnce(failed(403, "Forbidden"));
    const result = await getTestingLabAnalytics({ fromDate: "from", toDate: "to" });
    expect(result).toMatchObject({
      fromDate: "from",
      toDate: "to",
      generatedAt: null,
      current: { events: 0, fillRate: 0 },
      previous: null,
      locations: { total: 0, active: 0 },
      trend: [],
      events: [],
      accessIssues: ["Testing Lab analytics returned 403: Forbidden"],
    });
  });

  it("uses empty date defaults when analytics and options omit the period", async () => {
    const result = await getTestingLabAnalytics();
    expect(result.fromDate).toBe("");
    expect(result.toDate).toBe("");
  });

  it("reads analytics CSV and handles missing, Error, and non-Error payload failures", async () => {
    mocks.analytics.getTestingAnalyticsExport.mockResolvedValueOnce(ok(new Blob(["event,count\nA,1"])))
      .mockResolvedValueOnce(ok(null))
      .mockResolvedValueOnce(ok({ text: () => Promise.reject(new Error("Decode failed")) }))
      .mockResolvedValueOnce(ok({ text: () => Promise.reject("Decode failed") }));

    await expect(getTestingLabAnalyticsCsv({})).resolves.toEqual({ data: "event,count\nA,1" });
    await expect(getTestingLabAnalyticsCsv({})).resolves.toEqual({ data: null, issue: undefined });
    await expect(getTestingLabAnalyticsCsv({})).resolves.toEqual({
      data: null,
      issue: "Testing Lab analytics export could not be read: Decode failed",
    });
    await expect(getTestingLabAnalyticsCsv({})).resolves.toEqual({
      data: null,
      issue: "Testing Lab analytics export could not be read: Unknown error",
    });
  });

  it("covers status, location, and capacity edge cases", () => {
    expect(normalizeTestingRequestStatus("InProgress")).toBe("In Progress");
    expect(normalizeTestingRequestStatus(99)).toBe("Unknown");
    expect(normalizeTestingSessionStatus("Cancelled")).toBe("Cancelled");
    expect(normalizeTestingSessionStatus(99)).toBe("Unknown");
    expect(normalizeTestingLocationStatus("Maintenance")).toBe("Maintenance");
    expect(normalizeTestingLocationStatus(99)).toBe("Unknown");
    expect(countAvailableTesterSlots([
      { id: "one", title: "One", status: "Open", maxTesters: 5 },
      { id: "two", title: "Two", status: "Open", maxTesters: undefined },
    ])).toBe(5);

    const location = {
      id: "remote",
      name: "Remote lab",
      description: "Global",
      address: "Online",
      city: "World",
      state: "Remote",
      postalCode: "00000",
      country: "Brazil",
      contactEmail: "lab@example.com",
      isVirtual: true,
      status: "Active" as const,
    };
    expect(filterTestingLabLocations([location], {})).toEqual([location]);
    expect(filterTestingLabLocations([location], { q: "missing" })).toEqual([]);
    expect(filterTestingLabLocations([location], { mode: "physical" })).toEqual([]);
  });
});
