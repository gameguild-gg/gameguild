import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  createServerClient: vi.fn(),
  events: {
    getTestingEventsPublicForGetTestingEventsPublic: vi.fn(),
    getTestingEventsPublicForGetTestingEventsPublicByEventId: vi.fn(),
    getTestingEventsApplicationsMe: vi.fn(),
    getTestingEventsApplicationsReviewPackage: vi.fn(),
  },
  participation: {
    getTestingEventsRegistrationsMe: vi.fn(),
    getTestingEventsFeedbackObligationsMe: vi.fn(),
  },
}));

vi.mock("@/auth", () => ({
  getRequestAuthContext: mocks.auth,
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
  },
}));

import {
  getPublicTestingEventExperience,
  getPublicTestingEventsDirectory,
  getTestingParticipationOverview,
} from "./events-public-queries";

const signedIn = {
  token: "access-token",
  tenantId: "tenant-1",
  session: { user: { id: "user-1" }, tenantId: "tenant-1" },
};

describe("public Testing Lab queries", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.createServerClient.mockReturnValue({ kind: "server-client" });
    mocks.auth.mockResolvedValue(signedIn);
    mocks.events.getTestingEventsPublicForGetTestingEventsPublic.mockResolvedValue({
      ok: true,
      data: [{ id: "event-1", name: "Campus lab" }],
    });
    mocks.events.getTestingEventsPublicForGetTestingEventsPublicByEventId.mockResolvedValue({
      ok: true,
      data: { id: "event-1", name: "Campus lab" },
    });
    mocks.events.getTestingEventsApplicationsMe.mockResolvedValue({
      ok: true,
      data: [{ id: "application-1" }],
    });
    mocks.participation.getTestingEventsRegistrationsMe.mockResolvedValue({
      ok: true,
      data: [{ id: "registration-1" }],
    });
    mocks.participation.getTestingEventsFeedbackObligationsMe.mockResolvedValue({
      ok: true,
      data: [],
    });
    mocks.events.getTestingEventsApplicationsReviewPackage.mockResolvedValue({
      ok: true,
      data: { applicationId: "application-1" },
    });
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("uses safe directory defaults and the unauthenticated client", async () => {
    const result = await getPublicTestingEventsDirectory();

    expect(result.events).toEqual([{ id: "event-1", name: "Campus lab" }]);
    expect(result.accessIssues).toEqual([]);
    expect(mocks.events.getTestingEventsPublicForGetTestingEventsPublic).toHaveBeenCalledWith({
      skip: 0,
      take: 50,
    });
    expect(mocks.createServerClient).toHaveBeenCalledWith({
      baseUrl: "http://localhost:8080",
      cache: "no-store",
    });
  });

  it("clamps directory pagination and preserves API failures", async () => {
    mocks.events.getTestingEventsPublicForGetTestingEventsPublic.mockResolvedValueOnce({
      ok: false,
      error: { message: "Unavailable" },
    });

    const result = await getPublicTestingEventsDirectory({ skip: -10, take: 0 });

    expect(result).toEqual({
      events: [],
      accessIssues: ["Public events returned an error: Unavailable"],
    });
    expect(mocks.events.getTestingEventsPublicForGetTestingEventsPublic).toHaveBeenCalledWith({
      skip: 0,
      take: 1,
    });
  });

  it.each([
    [{ message: "Network unavailable" }, "Network unavailable"],
    ["socket closed", "Unknown error"],
  ])("turns thrown directory failures into access issues", async (failure, message) => {
    mocks.events.getTestingEventsPublicForGetTestingEventsPublic.mockRejectedValueOnce(failure);

    await expect(getPublicTestingEventsDirectory()).resolves.toEqual({
      events: [],
      accessIssues: [`Public events failed: ${message}`],
    });
  });

  it("uses the configured public API URL precedence", async () => {
    vi.stubEnv("API_URL", "http://api.internal");
    vi.stubEnv("NEXT_PUBLIC_API_URL", "http://api.public");
    await getPublicTestingEventsDirectory();
    expect(mocks.createServerClient).toHaveBeenLastCalledWith({
      baseUrl: "http://api.internal",
      cache: "no-store",
    });

    vi.stubEnv("API_URL", "");
    await getPublicTestingEventsDirectory();
    expect(mocks.createServerClient).toHaveBeenLastCalledWith({
      baseUrl: "http://api.public",
      cache: "no-store",
    });
  });

  it("keeps the public event available to anonymous users", async () => {
    mocks.auth.mockResolvedValueOnce({ session: null, token: null, tenantId: null });

    const result = await getPublicTestingEventExperience("event-1");

    expect(result).toMatchObject({
      event: { id: "event-1" },
      applications: [],
      registrations: [],
      feedbackObligations: [],
      isAuthenticated: false,
      accessIssues: [],
    });
    expect(mocks.events.getTestingEventsApplicationsMe).not.toHaveBeenCalled();
  });

  it("treats unavailable auth as anonymous and still reports a public event failure", async () => {
    mocks.auth.mockRejectedValueOnce(new Error("Auth unavailable"));
    mocks.events.getTestingEventsPublicForGetTestingEventsPublicByEventId.mockResolvedValueOnce({
      ok: false,
      error: { status: 404, message: "Not found" },
    });

    await expect(getPublicTestingEventExperience("missing")).resolves.toMatchObject({
      event: null,
      isAuthenticated: false,
      accessIssues: ["Public event returned 404: Not found"],
    });
  });

  it("enriches only pending obligations that identify an application", async () => {
    mocks.participation.getTestingEventsFeedbackObligationsMe.mockResolvedValueOnce({
      ok: true,
      data: [
        { id: "pending", applicationId: "application-1", status: "Pending" },
        { id: "fulfilled", applicationId: "application-2", status: "Fulfilled" },
        { id: "missing", status: "Pending" },
      ],
    });

    const result = await getPublicTestingEventExperience("event-1");

    expect(result.feedbackObligations).toEqual([
      expect.objectContaining({ id: "pending", reviewPackage: { applicationId: "application-1" } }),
      expect.objectContaining({ id: "fulfilled", reviewPackage: null }),
      expect.objectContaining({ id: "missing", reviewPackage: null }),
    ]);
    expect(mocks.events.getTestingEventsApplicationsReviewPackage).toHaveBeenCalledOnce();
    const clientOptions = mocks.createServerClient.mock.calls[1]![0];
    await expect(clientOptions.auth.getAccessToken()).resolves.toBe("access-token");
    await expect(clientOptions.tenant.getTenantId()).resolves.toBe("tenant-1");
  });

  it("returns partial private data and every actionable access issue", async () => {
    mocks.events.getTestingEventsPublicForGetTestingEventsPublicByEventId.mockResolvedValueOnce({
      ok: false,
      error: { status: 503, message: "Public unavailable" },
    });
    mocks.events.getTestingEventsApplicationsMe.mockResolvedValueOnce({
      ok: false,
      error: { status: 403, message: "Applications forbidden" },
    });
    mocks.participation.getTestingEventsRegistrationsMe.mockRejectedValueOnce(
      new Error("Registrations offline"),
    );
    mocks.participation.getTestingEventsFeedbackObligationsMe.mockResolvedValueOnce({
      ok: true,
      data: [{ id: "pending", applicationId: "application-1", status: "Pending" }],
    });
    mocks.events.getTestingEventsApplicationsReviewPackage.mockResolvedValueOnce({
      ok: false,
      error: { status: 404, message: "Package missing" },
    });

    const result = await getPublicTestingEventExperience("event-1");

    expect(result.event).toBeNull();
    expect(result.applications).toEqual([]);
    expect(result.registrations).toEqual([]);
    expect(result.feedbackObligations[0]).toMatchObject({
      id: "pending",
      reviewPackage: null,
    });
    expect(result.accessIssues).toEqual([
      "Public event returned 503: Public unavailable",
      "Your project applications returned 403: Applications forbidden",
      "Your tester registrations failed: Registrations offline",
      "Review package for application application-1 returned 404: Package missing",
    ]);
  });

  it("does not attempt package enrichment when obligations cannot be loaded", async () => {
    mocks.participation.getTestingEventsFeedbackObligationsMe.mockResolvedValueOnce({
      ok: false,
      error: { status: 403, message: "Obligations forbidden" },
    });

    const result = await getPublicTestingEventExperience("event-1");

    expect(result.feedbackObligations).toEqual([]);
    expect(result.accessIssues).toContain(
      "Your feedback obligations returned 403: Obligations forbidden",
    );
    expect(mocks.events.getTestingEventsApplicationsReviewPackage).not.toHaveBeenCalled();
  });

  it.each([
    [{ session: null, token: null, tenantId: null }],
    [new Error("Auth offline")],
  ])("returns an anonymous participation overview without private calls", async (auth) => {
    if (auth instanceof Error) mocks.auth.mockRejectedValueOnce(auth);
    else mocks.auth.mockResolvedValueOnce(auth);

    await expect(getTestingParticipationOverview()).resolves.toEqual({
      applications: [],
      registrations: [],
      feedbackObligations: [],
      isAuthenticated: false,
      accessIssues: [],
    });
  });

  it("loads the authenticated actor participation overview", async () => {
    mocks.participation.getTestingEventsFeedbackObligationsMe.mockResolvedValueOnce({
      ok: true,
      data: [{ id: "obligation-1" }],
    });

    await expect(getTestingParticipationOverview()).resolves.toMatchObject({
      applications: [{ id: "application-1" }],
      registrations: [{ id: "registration-1" }],
      feedbackObligations: [{ id: "obligation-1" }],
      isAuthenticated: true,
      accessIssues: [],
    });
    expect(mocks.events.getTestingEventsApplicationsMe).toHaveBeenCalledWith();
    expect(mocks.participation.getTestingEventsRegistrationsMe).toHaveBeenCalledWith();
  });

  it("preserves participation failures without inventing data", async () => {
    mocks.events.getTestingEventsApplicationsMe.mockResolvedValueOnce({
      ok: false,
      error: { status: 401, message: "Unauthorized" },
    });
    mocks.participation.getTestingEventsRegistrationsMe.mockResolvedValueOnce({
      ok: false,
      error: { status: 503, message: "Unavailable" },
    });
    mocks.participation.getTestingEventsFeedbackObligationsMe.mockRejectedValueOnce(
      "connection closed",
    );

    await expect(getTestingParticipationOverview()).resolves.toEqual({
      applications: [],
      registrations: [],
      feedbackObligations: [],
      isAuthenticated: true,
      accessIssues: [
        "Your project applications returned 401: Unauthorized",
        "Your tester registrations returned 503: Unavailable",
        "Your feedback obligations failed: Unknown error",
      ],
    });
  });
});
