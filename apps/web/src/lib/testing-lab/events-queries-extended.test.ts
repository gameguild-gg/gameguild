import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  createServerClient: vi.fn(),
  events: {
    getTestingEventsArchived: vi.fn(),
    getTestingEventsForGetTestingEvents: vi.fn(),
    getTestingEventsForGetTestingEventsByEventId: vi.fn(),
    getTestingEventsSlots: vi.fn(),
    getTestingEventsApplicationsForGetTestingEventsByEventIdApplications: vi.fn(),
    getTestingEventsApplicationsAccess: vi.fn(),
    getTestingEventsCommittee: vi.fn(),
    getTestingEventsApplicationsTesterEligibility: vi.fn(),
  },
  participation: {
    getTestingEventsParticipants: vi.fn(),
    getTestingEventsSlotsRegistrations: vi.fn(),
    getTestingEventsFeedback: vi.fn(),
  },
  templates: { getVTestingTemplates: vi.fn() },
}));

vi.mock("@/auth", () => ({ getRequestAuthContext: mocks.auth }));
vi.mock("@game-guild/client", () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    TestingLabTestingEventsModule: vi.fn(function TestingLabTestingEventsModule() { return mocks.events; }),
    TestingLabTestingEventParticipationModule: vi.fn(function TestingLabTestingEventParticipationModule() { return mocks.participation; }),
    TestingLabTestingEventTemplatesModule: vi.fn(function TestingLabTestingEventTemplatesModule() { return mocks.templates; }),
  },
}));

import {
  getArchivedTestingEventsDirectory,
  getTestingApplicationsDirectory,
  getTestingApplicationTesterEligibility,
  getTestingEventFeedbackReview,
  getTestingEventManagerData,
  getTestingEventsDirectory,
  getTestingEventTemplates,
  getTestingEventWorkspaceData,
  getTestingParticipantDirectory,
} from "./events-queries";

const ok = <T,>(data: T) => ({ ok: true, data });
const failed = (status: number | undefined, message: string) => ({
  ok: false,
  error: { status, message },
});

describe("extended Testing Lab event queries", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({ token: "token-1", tenantId: "tenant-1" });
    mocks.createServerClient.mockReturnValue({ kind: "client" });
    mocks.events.getTestingEventsArchived.mockResolvedValue(ok([]));
    mocks.events.getTestingEventsForGetTestingEvents.mockResolvedValue(ok([]));
    mocks.events.getTestingEventsForGetTestingEventsByEventId.mockResolvedValue(ok({ id: "event-1" }));
    mocks.events.getTestingEventsSlots.mockResolvedValue(ok([]));
    mocks.events.getTestingEventsApplicationsForGetTestingEventsByEventIdApplications.mockResolvedValue(ok([]));
    mocks.events.getTestingEventsApplicationsAccess.mockResolvedValue(ok({ canManageApplications: false }));
    mocks.events.getTestingEventsCommittee.mockResolvedValue(ok([]));
    mocks.events.getTestingEventsApplicationsTesterEligibility.mockResolvedValue(ok([]));
    mocks.participation.getTestingEventsParticipants.mockResolvedValue(ok({ items: [], totalCount: 0 }));
    mocks.participation.getTestingEventsSlotsRegistrations.mockResolvedValue(ok([]));
    mocks.participation.getTestingEventsFeedback.mockResolvedValue(ok([]));
    mocks.templates.getVTestingTemplates.mockResolvedValue(ok([]));
  });

  afterEach(() => vi.unstubAllEnvs());

  it("binds token, tenant, and API URL precedence to generated clients", async () => {
    vi.stubEnv("API_URL", "http://api.internal");
    vi.stubEnv("NEXT_PUBLIC_API_URL", "http://api.public");
    await getTestingEventTemplates();
    const options = mocks.createServerClient.mock.calls[0]![0];
    expect(options.baseUrl).toBe("http://api.internal");
    await expect(options.auth.getAccessToken()).resolves.toBe("token-1");
    await expect(options.tenant.getTenantId()).resolves.toBe("tenant-1");

    vi.stubEnv("API_URL", "");
    await getTestingEventTemplates(true);
    expect(mocks.createServerClient.mock.calls[1]![0].baseUrl).toBe("http://api.public");
  });

  it("uses event directory defaults and pagination clamps", async () => {
    await getTestingEventsDirectory();
    expect(mocks.events.getTestingEventsForGetTestingEvents).toHaveBeenLastCalledWith({
      status: undefined,
      skip: 0,
      take: 50,
    });
    await getTestingEventsDirectory({ skip: -5, take: 500 });
    expect(mocks.events.getTestingEventsForGetTestingEvents).toHaveBeenLastCalledWith({
      status: undefined,
      skip: 0,
      take: 100,
    });
    await getTestingEventsDirectory({ take: 0 });
    expect(mocks.events.getTestingEventsForGetTestingEvents).toHaveBeenLastCalledWith(
      expect.objectContaining({ take: 1 }),
    );
  });

  it.each([
    [failed(undefined, "Unavailable"), "Events returned an error: Unavailable"],
    [new Error("Network offline"), "Events failed: Network offline"],
    [{ message: "Structured failure" }, "Events failed: Structured failure"],
    [{ message: "   " }, "Events failed: Unknown error"],
    ["closed", "Events failed: Unknown error"],
  ])("preserves directory failure detail", async (outcome, issue) => {
    if (typeof outcome === "object" && outcome !== null && "ok" in outcome) {
      mocks.events.getTestingEventsForGetTestingEvents.mockResolvedValueOnce(outcome);
    } else {
      mocks.events.getTestingEventsForGetTestingEvents.mockRejectedValueOnce(outcome);
    }
    await expect(getTestingEventsDirectory()).resolves.toEqual({
      events: [],
      accessIssues: [issue],
    });
  });

  it("loads templates and archived events with defaults and failures", async () => {
    mocks.templates.getVTestingTemplates.mockResolvedValueOnce(failed(403, "Forbidden"));
    await expect(getTestingEventTemplates()).resolves.toEqual({
      templates: [],
      accessIssues: ["Event templates returned 403: Forbidden"],
    });
    expect(mocks.templates.getVTestingTemplates).toHaveBeenCalledWith("1", { includeArchived: false });

    await getArchivedTestingEventsDirectory();
    expect(mocks.events.getTestingEventsArchived).toHaveBeenLastCalledWith({ skip: 0, take: 50 });
    await getArchivedTestingEventsDirectory({ skip: -1, take: 0 });
    expect(mocks.events.getTestingEventsArchived).toHaveBeenLastCalledWith({ skip: 0, take: 1 });
    mocks.events.getTestingEventsArchived.mockResolvedValueOnce(failed(503, "Offline"));
    expect((await getArchivedTestingEventsDirectory()).accessIssues).toEqual([
      "Archived events returned 503: Offline",
    ]);
  });

  it("skips invalid or empty events and reports per-event application failures", async () => {
    mocks.events.getTestingEventsForGetTestingEvents.mockResolvedValueOnce(ok([
      { name: "Missing id", applicationCount: 1 },
      { id: "empty", applicationCount: 0 },
      { id: "event-1", name: null, applicationCount: undefined },
      { id: "event-2", name: "Second", applicationCount: 2 },
    ]));
    mocks.events.getTestingEventsApplicationsForGetTestingEventsByEventIdApplications
      .mockResolvedValueOnce(ok([{ id: "application-1" }]))
      .mockResolvedValueOnce(failed(403, "Forbidden"));

    const result = await getTestingApplicationsDirectory();
    expect(result.entries).toEqual([
      { event: expect.objectContaining({ id: "event-1" }), application: { id: "application-1" } },
    ]);
    expect(result.accessIssues).toEqual(["Applications for Second returned 403: Forbidden"]);
    expect(mocks.events.getTestingEventsApplicationsForGetTestingEventsByEventIdApplications).toHaveBeenNthCalledWith(
      1,
      "event-1",
      { status: undefined, skip: 0, take: 100 },
    );
  });

  it("returns no application work when the event directory fails", async () => {
    mocks.events.getTestingEventsForGetTestingEvents.mockResolvedValueOnce(failed(403, "Forbidden"));
    await expect(getTestingApplicationsDirectory()).resolves.toEqual({
      entries: [],
      accessIssues: ["Events returned 403: Forbidden"],
    });
  });

  it("normalizes participant filters and reports failures", async () => {
    await getTestingParticipantDirectory({ search: "   ", skip: -2, take: 500 });
    expect(mocks.participation.getTestingEventsParticipants).toHaveBeenLastCalledWith({
      search: undefined,
      status: undefined,
      skip: 0,
      take: 100,
    });
    await getTestingParticipantDirectory({ search: "  Ada  ", take: 0 });
    expect(mocks.participation.getTestingEventsParticipants).toHaveBeenLastCalledWith(
      expect.objectContaining({ search: "Ada", take: 1 }),
    );
    mocks.participation.getTestingEventsParticipants.mockResolvedValueOnce(failed(403, "Forbidden"));
    await expect(getTestingParticipantDirectory()).resolves.toEqual({
      directory: null,
      accessIssues: ["Participants returned 403: Forbidden"],
    });
  });

  it("returns safe manager defaults when application access is unavailable", async () => {
    mocks.events.getTestingEventsForGetTestingEventsByEventId.mockResolvedValueOnce(failed(404, "Missing"));
    mocks.events.getTestingEventsApplicationsForGetTestingEventsByEventIdApplications.mockResolvedValueOnce(failed(403, "Applications"));
    mocks.events.getTestingEventsApplicationsAccess.mockResolvedValueOnce(failed(403, "Access"));
    const result = await getTestingEventManagerData("event-1");
    expect(result).toMatchObject({
      event: null,
      slots: [],
      applications: [],
      applicationAccess: null,
      committee: [],
      registrationsBySlot: {},
    });
    expect(result.accessIssues).toHaveLength(3);
    expect(mocks.events.getTestingEventsSlots).not.toHaveBeenCalled();
  });

  it("loads manager-only slots and keeps registration failures isolated", async () => {
    mocks.events.getTestingEventsApplicationsAccess.mockResolvedValueOnce(ok({ canManageApplications: true }));
    mocks.events.getTestingEventsSlots.mockResolvedValueOnce(ok([
      { id: "slot-1" },
      { id: "slot-2" },
      { name: "Missing id" },
    ]));
    mocks.events.getTestingEventsCommittee.mockResolvedValueOnce(failed(403, "Committee"));
    mocks.participation.getTestingEventsSlotsRegistrations
      .mockResolvedValueOnce(ok([{ id: "registration-1" }]))
      .mockResolvedValueOnce(failed(503, "Registrations"));

    const result = await getTestingEventManagerData("event-1", { applicationStatus: "Approved" });
    expect(result.slots).toHaveLength(3);
    expect(result.committee).toEqual([]);
    expect(result.registrationsBySlot).toEqual({
      "slot-1": [{ id: "registration-1" }],
      "slot-2": [],
    });
    expect(result.accessIssues).toEqual([
      "Committee returned 403: Committee",
      "Registrations for slot slot-2 returned 503: Registrations",
    ]);
  });

  it("keeps manager data usable when slots cannot be loaded", async () => {
    mocks.events.getTestingEventsApplicationsAccess.mockResolvedValueOnce(ok({ canManageApplications: true }));
    mocks.events.getTestingEventsSlots.mockResolvedValueOnce(failed(503, "Slots offline"));
    const result = await getTestingEventManagerData("event-1");
    expect(result.slots).toEqual([]);
    expect(result.registrationsBySlot).toEqual({});
    expect(result.accessIssues).toContain("Slots returned 503: Slots offline");
  });

  it("reports feedback failures and returns successful feedback", async () => {
    mocks.participation.getTestingEventsFeedback.mockResolvedValueOnce(failed(403, "Forbidden"));
    await expect(getTestingEventFeedbackReview("event-1")).resolves.toEqual({
      feedback: [],
      accessIssues: ["Event feedback returned 403: Forbidden"],
    });
    mocks.participation.getTestingEventsFeedback.mockResolvedValueOnce(ok([{ id: "feedback-1" }]));
    expect((await getTestingEventFeedbackReview("event-1")).feedback).toEqual([{ id: "feedback-1" }]);
  });

  it("short-circuits empty eligibility and normalizes the bounded tester set", async () => {
    await expect(getTestingApplicationTesterEligibility("event-1", ["", ""])).resolves.toEqual({
      eligibility: [],
      accessIssues: [],
    });
    expect(mocks.events.getTestingEventsApplicationsTesterEligibility).not.toHaveBeenCalled();

    const ids = Array.from({ length: 105 }, (_, index) => `tester-${index}`);
    mocks.events.getTestingEventsApplicationsTesterEligibility.mockResolvedValueOnce(failed(403, "Forbidden"));
    const result = await getTestingApplicationTesterEligibility("event-1", [ids[0]!, "", ...ids]);
    expect(result).toEqual({ eligibility: [], accessIssues: ["Tester eligibility returned 403: Forbidden"] });
    expect(mocks.events.getTestingEventsApplicationsTesterEligibility.mock.calls[0]![1].testerUserIds).toHaveLength(100);
    expect(new Set(mocks.events.getTestingEventsApplicationsTesterEligibility.mock.calls[0]![1].testerUserIds).size).toBe(100);
  });

  it("delegates the event workspace query to manager data", async () => {
    const result = await getTestingEventWorkspaceData("event-1");
    expect(result.event).toEqual({ id: "event-1" });
    expect(mocks.events.getTestingEventsForGetTestingEventsByEventId).toHaveBeenCalledWith("event-1");
  });
});
