import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  getRequestAuthContext: vi.fn(),
  auth: vi.fn(),
  createServerClient: vi.fn(),
  events: {
    getTestingEventsArchived: vi.fn(),
    getTestingEventsForGetTestingEvents: vi.fn(),
    getTestingEventsForGetTestingEventsByEventId: vi.fn(),
    getTestingEventsSlots: vi.fn(),
    getTestingEventsApplicationsByApplicationId: vi.fn(),
    getTestingEventsApplicationsForGetTestingEventsByEventIdApplications:
      vi.fn(),
    getTestingEventsApplicationsAccess: vi.fn(),
    getTestingEventsCommittee: vi.fn(),
    getTestingEventsApplicationsTesterEligibility: vi.fn(),
    getTestingEventsPublicForGetTestingEventsPublic: vi.fn(),
    getTestingEventsPublicForGetTestingEventsPublicByEventId: vi.fn(),
    getTestingEventsApplicationsMe: vi.fn(),
    getTestingEventsApplicationsReviewPackage: vi.fn(),
  },
  participation: {
    getTestingEventsParticipants: vi.fn(),
    getTestingEventsSlotsRegistrations: vi.fn(),
    getTestingEventsRegistrationsMe: vi.fn(),
    getTestingEventsFeedbackObligationsMe: vi.fn(),
    getTestingEventsFeedback: vi.fn(),
  },
  templates: {
    getVTestingTemplates: vi.fn(),
  },
}));

vi.mock("@/auth", () => ({
  getRequestAuthContext: mocks.getRequestAuthContext,
  auth: mocks.auth,
}));

vi.mock("@game-guild/client", () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    TestingLabTestingEventsModule: vi.fn(
      function TestingLabTestingEventsModule() {
        return mocks.events;
      },
    ),
    TestingLabTestingEventParticipationModule: vi.fn(
      function TestingLabTestingEventParticipationModule() {
        return mocks.participation;
      },
    ),
    TestingLabTestingEventTemplatesModule: vi.fn(
      function TestingLabTestingEventTemplatesModule() {
        return mocks.templates;
      },
    ),
  },
}));

import {
  getArchivedTestingEventsDirectory,
  getPublicTestingEventExperience,
  getPublicTestingEventsDirectory,
  getTestingEventFeedbackReview,
  getTestingEventManagerData,
  getTestingApplicationsDirectory,
  getTestingEventsDirectory,
  getTestingEventTemplates,
  getTestingParticipantDirectory,
  getTestingApplicationTesterEligibility,
} from "./events-queries";

describe("Testing Lab event queries", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getRequestAuthContext.mockResolvedValue({
      token: "access-token",
      tenantId: "tenant-1",
      session: { tenantId: "tenant-1", user: { id: "user-1" } },
    });
    mocks.auth.mockResolvedValue({
      tenantId: "tenant-1",
      user: { id: "user-1" },
    });
    mocks.createServerClient.mockReturnValue({ kind: "server-client" });
    mocks.events.getTestingEventsApplicationsTesterEligibility.mockResolvedValue(
      {
        ok: true,
        data: [
          { testerUserId: "tester-1", eligibleApplicationIds: [] },
          {
            testerUserId: "tester-2",
            eligibleApplicationIds: ["application-1"],
          },
        ],
      },
    );
    mocks.events.getTestingEventsForGetTestingEvents.mockResolvedValue({
      ok: true,
      data: [
        {
          id: "event-1",
          name: "Friday campus lab",
          status: "ApplicationsOpen",
          mode: "InPerson",
          slotCount: 2,
          applicationCount: 4,
        },
      ],
    });
    mocks.events.getTestingEventsArchived.mockResolvedValue({
      ok: true,
      data: [
        {
          id: "event-archived",
          name: "Archived playtest",
          status: "Completed",
        },
      ],
    });
    mocks.events.getTestingEventsForGetTestingEventsByEventId.mockResolvedValue(
      {
        ok: true,
        data: {
          id: "event-1",
          name: "Friday campus lab",
          status: "ApplicationsOpen",
        },
      },
    );
    mocks.events.getTestingEventsSlots.mockResolvedValue({
      ok: true,
      data: [{ id: "slot-1", eventId: "event-1", registeredTesterCount: 3 }],
    });
    mocks.events.getTestingEventsApplicationsForGetTestingEventsByEventIdApplications.mockResolvedValue(
      {
        ok: true,
        data: [{ id: "application-1", eventId: "event-1", status: "Pending" }],
      },
    );
    mocks.events.getTestingEventsCommittee.mockResolvedValue({
      ok: true,
      data: [
        {
          id: "member-1",
          eventId: "event-1",
          userId: "user-1",
          userName: "Reviewer",
        },
      ],
    });
    mocks.participation.getTestingEventsParticipants.mockResolvedValue({
      ok: true,
      data: {
        items: [
          {
            registrationId: "registration-1",
            eventId: "event-1",
            eventName: "Friday campus lab",
            userId: "user-1",
            userName: "Ada Player",
            userEmail: "ada@example.com",
            status: "Registered",
          },
        ],
        totalCount: 1,
        registeredCount: 1,
        waitlistedCount: 0,
        checkedInCount: 0,
        attendedCount: 0,
        completedCount: 0,
        noShowCount: 0,
      },
    });
    mocks.participation.getTestingEventsSlotsRegistrations.mockResolvedValue({
      ok: true,
      data: [{ id: "registration-1", slotId: "slot-1", status: "Registered" }],
    });
    mocks.events.getTestingEventsPublicForGetTestingEventsPublic.mockResolvedValue(
      {
        ok: true,
        data: [
          {
            id: "event-1",
            name: "Friday campus lab",
            status: "ApplicationsOpen",
            mode: "InPerson",
            applicationCount: 4,
            slots: [
              {
                id: "slot-1",
                availableTesterCount: 7,
                availableProjectCount: 2,
              },
            ],
          },
        ],
      },
    );
    mocks.events.getTestingEventsPublicForGetTestingEventsPublicByEventId.mockResolvedValue(
      {
        ok: true,
        data: {
          id: "event-1",
          name: "Friday campus lab",
          status: "ApplicationsOpen",
          slots: [
            { id: "slot-1", availableTesterCount: 7, availableProjectCount: 2 },
          ],
        },
      },
    );
    mocks.events.getTestingEventsApplicationsMe.mockResolvedValue({
      ok: true,
      data: [{ id: "application-1", eventId: "event-1", status: "Pending" }],
    });
    mocks.events.getTestingEventsApplicationsAccess.mockResolvedValue({
      ok: true,
      data: {
        canViewApplications: true,
        canManageApplications: true,
        canVote: false,
      },
    });
    mocks.events.getTestingEventsApplicationsReviewPackage.mockResolvedValue({
      ok: true,
      data: {
        applicationId: "application-1",
        feedbackQuestionnaire: { title: "Feedback", questions: [] },
      },
    });
    mocks.participation.getTestingEventsRegistrationsMe.mockResolvedValue({
      ok: true,
      data: [
        {
          id: "registration-1",
          eventId: "event-1",
          slotId: "slot-1",
          status: "Registered",
        },
      ],
    });
    mocks.participation.getTestingEventsFeedbackObligationsMe.mockResolvedValue(
      {
        ok: true,
        data: [
          {
            id: "obligation-1",
            eventId: "event-1",
            applicationId: "application-1",
            status: "Pending",
          },
        ],
      },
    );
    mocks.participation.getTestingEventsFeedback.mockResolvedValue({
      ok: true,
      data: [
        {
          obligationId: "obligation-1",
          eventId: "event-1",
          slotId: "slot-1",
          applicationId: "application-1",
          testerUserId: "tester-1",
          status: "Fulfilled",
          feedback: { id: "feedback-1", overallRating: 9 },
        },
      ],
    });
    mocks.templates.getVTestingTemplates.mockResolvedValue({
      ok: true,
      data: [
        { id: "template-1", name: "Campus playtest", currentRevisionNumber: 2 },
      ],
    });
  });

  it("loads the manager event directory through the generated event client", async () => {
    const result = await getTestingEventsDirectory({
      status: "ApplicationsOpen",
      skip: 10,
      take: 25,
    });

    expect(result.events).toHaveLength(1);
    expect(result.accessIssues).toEqual([]);
    expect(mocks.createServerClient).toHaveBeenCalledWith({
      baseUrl: "http://localhost:8080",
      auth: { getAccessToken: expect.any(Function) },
      tenant: { getTenantId: expect.any(Function) },
    });
    expect(
      mocks.events.getTestingEventsForGetTestingEvents,
    ).toHaveBeenCalledWith({
      status: "ApplicationsOpen",
      skip: 10,
      take: 25,
    });
    const clientOptions = mocks.createServerClient.mock.calls[0]?.[0];
    await expect(clientOptions.auth.getAccessToken()).resolves.toBe(
      "access-token",
    );
    await expect(clientOptions.tenant.getTenantId()).resolves.toBe("tenant-1");
    expect(mocks.getRequestAuthContext).toHaveBeenCalledOnce();
  });

  it("loads archived and active tenant event templates when requested", async () => {
    const result = await getTestingEventTemplates(true);
    expect(result.templates).toEqual([
      { id: "template-1", name: "Campus playtest", currentRevisionNumber: 2 },
    ]);
    expect(result.accessIssues).toEqual([]);
    expect(mocks.templates.getVTestingTemplates).toHaveBeenCalledWith("1", {
      includeArchived: true,
    });
  });

  it("loads archived events through the dedicated generated-client operation", async () => {
    const result = await getArchivedTestingEventsDirectory({
      skip: 5,
      take: 20,
    });

    expect(result.events).toEqual([
      { id: "event-archived", name: "Archived playtest", status: "Completed" },
    ]);
    expect(result.accessIssues).toEqual([]);
    expect(mocks.events.getTestingEventsArchived).toHaveBeenCalledWith({
      skip: 5,
      take: 20,
    });
  });

  it("loads the tenant participant directory through one generated-client call", async () => {
    const result = await getTestingParticipantDirectory({
      search: "Ada",
      status: "Registered",
      skip: 25,
      take: 25,
    });

    expect(result.directory?.items).toHaveLength(1);
    expect(result.directory?.items?.[0]?.userName).toBe("Ada Player");
    expect(result.accessIssues).toEqual([]);
    expect(
      mocks.participation.getTestingEventsParticipants,
    ).toHaveBeenCalledTimes(1);
    expect(
      mocks.participation.getTestingEventsParticipants,
    ).toHaveBeenCalledWith({
      search: "Ada",
      status: "Registered",
      skip: 25,
      take: 25,
    });
  });

  it("loads event, slots, applications, committee, and registrations as one manager view", async () => {
    const result = await getTestingEventManagerData("event-1", {
      applicationStatus: "Pending",
    });

    expect(result.event?.id).toBe("event-1");
    expect(result.slots).toHaveLength(1);
    expect(result.applications).toHaveLength(1);
    expect(result.applicationAccess).toEqual({
      canViewApplications: true,
      canManageApplications: true,
      canVote: false,
    });
    expect(result.committee).toHaveLength(1);
    expect(result.registrationsBySlot["slot-1"]).toHaveLength(1);
    expect(result.accessIssues).toEqual([]);
    expect(
      mocks.events
        .getTestingEventsApplicationsForGetTestingEventsByEventIdApplications,
    ).toHaveBeenCalledWith("event-1", {
      status: "Pending",
      skip: 0,
      take: 100,
    });
    expect(
      mocks.events.getTestingEventsApplicationsByApplicationId,
    ).not.toHaveBeenCalled();
    expect(
      mocks.participation.getTestingEventsSlotsRegistrations,
    ).toHaveBeenCalledWith("slot-1");
  });

  it("keeps reviewer application data isolated from slots and other manager-only queries", async () => {
    mocks.events.getTestingEventsApplicationsAccess.mockResolvedValue({
      ok: true,
      data: {
        canViewApplications: true,
        canManageApplications: false,
        canVote: true,
      },
    });

    const result = await getTestingEventManagerData("event-1");

    expect(result.applications).toHaveLength(1);
    expect(result.applicationAccess).toEqual({
      canViewApplications: true,
      canManageApplications: false,
      canVote: true,
    });
    expect(result.slots).toEqual([]);
    expect(result.committee).toEqual([]);
    expect(result.registrationsBySlot).toEqual({});
    expect(mocks.events.getTestingEventsSlots).not.toHaveBeenCalled();
    expect(mocks.events.getTestingEventsCommittee).not.toHaveBeenCalled();
    expect(
      mocks.participation.getTestingEventsSlotsRegistrations,
    ).not.toHaveBeenCalled();
  });

  it("loads manager feedback review through the generated participation client", async () => {
    const result = await getTestingEventFeedbackReview("event-1");

    expect(result.feedback).toHaveLength(1);
    expect(result.feedback[0]?.feedback?.overallRating).toBe(9);
    expect(result.accessIssues).toEqual([]);
    expect(mocks.participation.getTestingEventsFeedback).toHaveBeenCalledWith(
      "event-1",
    );
  });

  it("loads the tenant application directory from only events that contain applications", async () => {
    mocks.events.getTestingEventsForGetTestingEvents.mockResolvedValue({
      ok: true,
      data: [
        { id: "event-1", name: "Friday campus lab", applicationCount: 1 },
        { id: "event-empty", name: "Empty lab", applicationCount: 0 },
      ],
    });

    const result = await getTestingApplicationsDirectory({ status: "Pending" });

    expect(result.entries).toEqual([
      {
        event: {
          id: "event-1",
          name: "Friday campus lab",
          applicationCount: 1,
        },
        application: {
          id: "application-1",
          eventId: "event-1",
          status: "Pending",
        },
      },
    ]);
    expect(result.accessIssues).toEqual([]);
    expect(
      mocks.events
        .getTestingEventsApplicationsForGetTestingEventsByEventIdApplications,
    ).toHaveBeenCalledTimes(1);
    expect(
      mocks.events
        .getTestingEventsApplicationsForGetTestingEventsByEventIdApplications,
    ).toHaveBeenCalledWith("event-1", {
      status: "Pending",
      skip: 0,
      take: 100,
    });
  });

  it("loads tester eligibility in one tenant-scoped request", async () => {
    const result = await getTestingApplicationTesterEligibility("event-1", [
      "tester-1",
      "tester-2",
    ]);

    expect(result.eligibility).toHaveLength(2);
    expect(result.accessIssues).toEqual([]);
    expect(
      mocks.events.getTestingEventsApplicationsTesterEligibility,
    ).toHaveBeenCalledWith("event-1", {
      testerUserIds: ["tester-1", "tester-2"],
    });
  });
  it("keeps partial manager data and reports generated-client failures", async () => {
    mocks.events.getTestingEventsApplicationsForGetTestingEventsByEventIdApplications.mockResolvedValue(
      {
        ok: false,
        error: { message: "Forbidden", status: 403 },
      },
    );

    const result = await getTestingEventManagerData("event-1");

    expect(result.event?.id).toBe("event-1");
    expect(result.applications).toEqual([]);
    expect(result.accessIssues).toContain(
      "Applications returned 403: Forbidden",
    );
  });

  it("retains structured generated-client error messages instead of hiding them", async () => {
    mocks.events.getTestingEventsForGetTestingEventsByEventId.mockRejectedValue(
      {
        name: "ApiError",
        message: "Response validation failed: event details are malformed",
      },
    );

    const result = await getTestingEventManagerData("event-1");

    expect(result.event).toBeNull();
    expect(result.accessIssues).toContain(
      "Event failed: Response validation failed: event details are malformed",
    );
  });

  it("loads the anonymous public event directory without requiring actor state", async () => {
    mocks.auth.mockResolvedValue(null);

    const result = await getPublicTestingEventsDirectory({
      skip: -4,
      take: 400,
    });

    expect(result.events).toHaveLength(1);
    expect(result.accessIssues).toEqual([]);
    expect(mocks.createServerClient).toHaveBeenCalledWith({
      baseUrl: "http://localhost:8080",
      cache: "no-store",
    });
    expect(
      mocks.events.getTestingEventsPublicForGetTestingEventsPublic,
    ).toHaveBeenCalledWith({ skip: 0, take: 100 });
    expect(mocks.events.getTestingEventsApplicationsMe).not.toHaveBeenCalled();
  });

  it("loads a public event together with the signed-in actor application, registration, and obligations", async () => {
    const result = await getPublicTestingEventExperience("event-1");

    expect(result.event?.id).toBe("event-1");
    expect(result.applications).toHaveLength(1);
    expect(result.registrations).toHaveLength(1);
    expect(result.feedbackObligations).toHaveLength(1);
    expect(result.feedbackObligations[0]?.reviewPackage?.applicationId).toBe(
      "application-1",
    );
    expect(result.isAuthenticated).toBe(true);
    expect(result.accessIssues).toEqual([]);
    expect(
      mocks.events.getTestingEventsPublicForGetTestingEventsPublicByEventId.mock
        .invocationCallOrder[0],
    ).toBeLessThan(mocks.getRequestAuthContext.mock.invocationCallOrder[0]!);
    expect(mocks.createServerClient).toHaveBeenNthCalledWith(1, {
      baseUrl: "http://localhost:8080",
      cache: "no-store",
    });
    expect(mocks.createServerClient).toHaveBeenNthCalledWith(2, {
      baseUrl: "http://localhost:8080",
      auth: { getAccessToken: expect.any(Function) },
      tenant: { getTenantId: expect.any(Function) },
    });
    expect(mocks.events.getTestingEventsApplicationsMe).toHaveBeenCalledWith({
      eventId: "event-1",
    });
    expect(
      mocks.participation.getTestingEventsRegistrationsMe,
    ).toHaveBeenCalledWith({ eventId: "event-1" });
    expect(
      mocks.participation.getTestingEventsFeedbackObligationsMe,
    ).toHaveBeenCalledWith({ eventId: "event-1" });
  });

  it("keeps an anonymous public event readable and skips private self-service calls", async () => {
    mocks.getRequestAuthContext.mockResolvedValue({
      session: null,
      token: null,
      tenantId: null,
    });

    const result = await getPublicTestingEventExperience("event-1");

    expect(result.event?.id).toBe("event-1");
    expect(result.applications).toEqual([]);
    expect(result.registrations).toEqual([]);
    expect(result.feedbackObligations).toEqual([]);
    expect(result.isAuthenticated).toBe(false);
    expect(mocks.events.getTestingEventsApplicationsMe).not.toHaveBeenCalled();
    expect(
      mocks.participation.getTestingEventsRegistrationsMe,
    ).not.toHaveBeenCalled();
  });
});
