import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  getRequestAuthContext: vi.fn(),
  createServerClient: vi.fn(() => ({})),
  revalidatePath: vi.fn(),
  events: {
    postTestingEventsArchive: vi.fn(),
    postTestingEvents: vi.fn(),
    putTestingEvents: vi.fn(),
    deleteTestingEvents: vi.fn(),
    postTestingEventsSlots: vi.fn(),
    postTestingEventsSlotsBatch: vi.fn(),
    putTestingEventsSlots: vi.fn(),
    deleteTestingEventsSlots: vi.fn(),
    postTestingEventsApplicationsReject: vi.fn(),
    postTestingEventsOpenApplications: vi.fn(),
    postTestingEventsCloseApplications: vi.fn(),
    postTestingEventsSchedule: vi.fn(),
    postTestingEventsActivate: vi.fn(),
    postTestingEventsComplete: vi.fn(),
    postTestingEventsCancel: vi.fn(),
    postTestingEventsCommittee: vi.fn(),
    deleteTestingEventsCommittee: vi.fn(),
    putTestingEventsLearning: vi.fn(),
    postTestingEventsApplications: vi.fn(),
    postTestingEventsApplicationsDrafts: vi.fn(),
    putTestingEventsApplicationsDraft: vi.fn(),
    postTestingEventsApplicationsSubmit: vi.fn(),
    putTestingEventsApplications: vi.fn(),
    postTestingEventsApplicationsWithdraw: vi.fn(),
    postTestingEventsApplicationsReview: vi.fn(),
    postTestingEventsApplicationsVotes: vi.fn(),
    postTestingEventsApplicationsApprove: vi.fn(),
    postTestingEventsApplicationsWaitlist: vi.fn(),
    putTestingEventsApplicationsSlot: vi.fn(),
    postTestingEventsRestore: vi.fn(),
    putTestingEventsConfiguration: vi.fn(),
  },
  participation: {
    deleteTestingEventsRegistrations: vi.fn(),
    postTestingEventsSlotsRegistrations: vi.fn(),
    postTestingEventsRegistrationsCheckIn: vi.fn(),
    postTestingEventsRegistrationsCheckOut: vi.fn(),
    postTestingEventsRegistrationsNoShow: vi.fn(),
    postTestingEventsRegistrationsComplete: vi.fn(),
    postTestingEventsRegistrationsTestedProjects: vi.fn(),
    postTestingEventsFeedbackObligationsFeedback: vi.fn(),
  },
  templates: {
    postVTestingTemplates: vi.fn(),
    putVTestingTemplates: vi.fn(),
    postVTestingTemplatesArchive: vi.fn(),
    postVTestingTemplatesRestore: vi.fn(),
  },
}));

vi.mock("@/auth", () => ({
  getRequestAuthContext: mocks.getRequestAuthContext,
}));

vi.mock("next/cache", () => ({
  revalidatePath: mocks.revalidatePath,
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
  addTestingEventCommitteeMember,
  approveTestingEventApplication,
  archiveTestingEvent,
  assignTestedProjectToRegistration,
  assignTestingEventApplicationSlot,
  beginTestingEventApplicationReview,
  cancelTestingEventRegistration,
  configureTestingEventLearning,
  configureTestingEvent,
  createTestingEvent,
  createTestingEventSlot,
  createTestingEventSlots,
  deleteTestingEvent,
  deleteTestingEventSlot,
  rejectTestingEventApplication,
  registerForTestingEventSlot,
  removeTestingEventCommitteeMember,
  restoreTestingEvent,
  saveTestingProjectApplicationDraft,
  saveTestingEventTemplate,
  setTestingEventTemplateArchived,
  submitTestingProjectApplication,
  submitTestingEventFeedback,
  transitionTestingEvent,
  updateTestingEvent,
  updateTestingEventAttendance,
  updateTestingEventSlot,
  updateTestingProjectApplication,
  voteOnTestingEventApplication,
  waitlistTestingEventApplication,
  withdrawTestingProjectApplication,
} from "./events-actions";

function form(values: Record<string, string>) {
  const data = new FormData();
  data.set("generalRules", "Respect the code of conduct.");
  data.set("candidateInstructions", "Provide a playable build.");
  data.set("testerInstructions", "Complete the assigned tasks.");
  Object.entries(values).forEach(([key, value]) => data.set(key, value));
  return data;
}

describe("Testing Lab event actions", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getRequestAuthContext.mockResolvedValue({
      token: "access-token",
      tenantId: "tenant-1",
      session: { tenantId: "tenant-1" },
    });
  });

  it("binds one authenticated actor and tenant to every generated client", async () => {
    mocks.events.postTestingEvents.mockResolvedValue({ ok: true, data: { id: "event-1" } });
    await createTestingEvent(
      form({
        name: "Authenticated event",
        applicationsOpenAt: "2026-10-01T09:00",
        applicationsCloseAt: "2026-10-02T09:00",
        startsAt: "2026-10-03T09:00",
        endsAt: "2026-10-03T12:00",
      }),
    );

    const options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    await expect(options.auth.getAccessToken()).resolves.toBe("access-token");
    await expect(options.tenant.getTenantId()).resolves.toBe("tenant-1");
    expect(mocks.getRequestAuthContext).toHaveBeenCalledOnce();
  });

  it("creates a draft event without requiring its participation configuration", async () => {
    mocks.events.postTestingEvents.mockResolvedValue({
      ok: true,
      data: { id: "event-1", name: "Campus showcase" },
    });

    const data = form({
      name: "Campus showcase",
      timeZoneId: "America/Sao_Paulo",
      mode: "InPerson",
      approvalMode: "Committee",
      applicationsOpenAt: "2026-08-01T09:00",
      applicationsCloseAt: "2026-08-05T18:00",
      startsAt: "2026-08-08T18:00",
      endsAt: "2026-08-08T21:00",
      requiresFeedback: "true",
    });
    data.delete("generalRules");
    data.delete("candidateInstructions");
    data.delete("testerInstructions");

    const result = await createTestingEvent(data);

    expect(result.success).toBe(true);
    expect(mocks.events.postTestingEvents).toHaveBeenCalledWith(
      expect.objectContaining({
        name: "Campus showcase",
        mode: "InPerson",
        approvalMode: "Committee",
        timeZoneId: "America/Sao_Paulo",
        requiresFeedback: true,
        applicationsOpenAt: "2026-08-01T12:00:00.000Z",
        applicationsCloseAt: "2026-08-05T21:00:00.000Z",
        startsAt: "2026-08-08T21:00:00.000Z",
        endsAt: "2026-08-09T00:00:00.000Z",
        configuration: undefined,
      }),
    );
    expect(mocks.revalidatePath).toHaveBeenCalledWith(
      "/workspace/testing-lab/events",
    );
  });

  it("archives and restores an event through the generated client", async () => {
    mocks.events.postTestingEventsArchive.mockResolvedValue({
      ok: true,
      data: true,
    });
    mocks.events.postTestingEventsRestore.mockResolvedValue({
      ok: true,
      data: true,
    });

    const archived = await archiveTestingEvent(form({ eventId: "event-1" }));
    const restored = await restoreTestingEvent(form({ eventId: "event-1" }));

    expect(archived).toMatchObject({
      success: true,
      data: true,
      message: "Testing event archived.",
    });
    expect(restored).toMatchObject({
      success: true,
      data: true,
      message: "Testing event restored.",
    });
    expect(mocks.events.postTestingEventsArchive).toHaveBeenCalledWith(
      "event-1",
    );
    expect(mocks.events.postTestingEventsRestore).toHaveBeenCalledWith(
      "event-1",
    );
  });

  it("shows the API validation detail instead of its generic validation code", async () => {
    mocks.events.postTestingEvents.mockResolvedValue({
      ok: false,
      error: {
        message: "TestingLab.Validation",
        detail: "The event must start after applications close.",
      },
    });

    const result = await createTestingEvent(
      form({
        name: "Campus showcase",
        applicationsOpenAt: "2026-08-01T09:00",
        applicationsCloseAt: "2026-08-05T18:00",
        startsAt: "2026-08-08T18:00",
        endsAt: "2026-08-08T21:00",
      }),
    );

    expect(result).toEqual({
      success: false,
      error: "The event must start after applications close.",
    });
  });

  it("replaces a generic TestingLab.Validation response with actionable guidance", async () => {
    mocks.events.postTestingEvents.mockResolvedValue({
      ok: false,
      error: { message: "TestingLab.Validation" },
    });

    const result = await createTestingEvent(
      form({
        name: "Campus showcase",
        applicationsOpenAt: "2026-08-01T09:00",
        applicationsCloseAt: "2026-08-05T18:00",
        startsAt: "2026-08-08T18:00",
        endsAt: "2026-08-08T21:00",
      }),
    );

    expect(result).toEqual({
      success: false,
      error:
        "Check that applications close before the event starts and the event ends after it starts.",
    });
  });

  it("shows validation detail when the generated client rejects the request", async () => {
    mocks.events.postTestingEvents.mockRejectedValue({
      message: "TestingLab.Validation",
      detail: "Recurrence end must not precede the event start.",
    });

    const result = await createTestingEvent(
      form({
        name: "Recurring playtest",
        applicationsOpenAt: "2026-08-01T09:00",
        applicationsCloseAt: "2026-08-05T18:00",
        startsAt: "2026-08-08T18:00",
        endsAt: "2026-08-08T21:00",
      }),
    );

    expect(result).toEqual({
      success: false,
      error: "Recurrence end must not precede the event start.",
    });
  });

  it("rejects an application window that closes before it opens", async () => {
    const result = await createTestingEvent(
      form({
        name: "Invalid application window",
        applicationsOpenAt: "2026-08-05T18:00",
        applicationsCloseAt: "2026-08-05T09:00",
        startsAt: "2026-08-08T18:00",
        endsAt: "2026-08-08T21:00",
      }),
    );

    expect(result).toEqual({
      success: false,
      error: "Applications must close after they open.",
    });
    expect(mocks.events.postTestingEvents).not.toHaveBeenCalled();
  });

  it("rejects an event that starts before applications close", async () => {
    const result = await createTestingEvent(
      form({
        name: "Invalid event schedule",
        applicationsOpenAt: "2026-08-01T09:00",
        applicationsCloseAt: "2026-08-08T18:00",
        startsAt: "2026-08-08T09:00",
        endsAt: "2026-08-08T21:00",
      }),
    );

    expect(result).toEqual({
      success: false,
      error: "The event must start after applications close.",
    });
    expect(mocks.events.postTestingEvents).not.toHaveBeenCalled();
  });

  it("forwards a weekly recurrence to the generated client", async () => {
    mocks.events.postTestingEvents.mockResolvedValue({
      ok: true,
      data: { id: "event-2" },
    });
    const input = form({
      name: "Weekly playtest",
      mode: "Online",
      approvalMode: "ManagerOnly",
      applicationsOpenAt: "2026-08-01T09:00",
      applicationsCloseAt: "2026-08-02T18:00",
      startsAt: "2026-08-03T18:00",
      endsAt: "2026-08-03T20:00",
      recurrenceFrequency: "Weekly",
      recurrenceInterval: "1",
      recurrenceEndMode: "count",
      recurrenceOccurrenceCount: "3",
    });
    input.append("recurrenceDaysOfWeek", "Monday");

    const result = await createTestingEvent(input);

    expect(result.success).toBe(true);
    expect(mocks.events.postTestingEvents).toHaveBeenCalledWith(
      expect.objectContaining({
        recurrence: {
          frequency: "Weekly",
          interval: 1,
          daysOfWeek: ["Monday"],
          occurrenceCount: 3,
          endsAt: null,
        },
      }),
    );
  });

  it("rejects a recurrence end before the first event starts", async () => {
    const result = await createTestingEvent(
      form({
        name: "Invalid recurring playtest",
        applicationsOpenAt: "2026-08-01T09:00",
        applicationsCloseAt: "2026-08-02T18:00",
        startsAt: "2026-08-03T18:00",
        endsAt: "2026-08-03T20:00",
        recurrenceFrequency: "Daily",
        recurrenceInterval: "1",
        recurrenceEndMode: "date",
        recurrenceEndsAt: "2026-08-03T17:00",
      }),
    );

    expect(result).toEqual({
      success: false,
      error: "Recurrence end must not precede the event start.",
    });
    expect(mocks.events.postTestingEvents).not.toHaveBeenCalled();
  });

  it("requires campus and room for an in-person slot before calling the API", async () => {
    const result = await createTestingEventSlot(
      form({
        eventId: "event-1",
        mode: "InPerson",
        startsAt: "2026-08-08T18:00",
        endsAt: "2026-08-08T20:00",
      }),
    );

    expect(result).toEqual({
      success: false,
      error: "Campus and room are required for in-person slots.",
    });
    expect(mocks.events.postTestingEventsSlots).not.toHaveBeenCalled();
  });

  it("converts a session schedule from the event timezone before calling the API", async () => {
    mocks.events.postTestingEventsSlots.mockResolvedValueOnce({
      ok: true,
      data: { id: "slot-1" },
    });

    await createTestingEventSlot(
      form({
        eventId: "event-1",
        mode: "Online",
        meetingUrl: "https://meet.example.test/playtest",
        timeZoneId: "America/Sao_Paulo",
        startsAt: "2026-10-03T09:00",
        endsAt: "2026-10-03T10:00",
      }),
    );

    expect(mocks.events.postTestingEventsSlots).toHaveBeenCalledWith(
      "event-1",
      expect.objectContaining({
        startsAt: "2026-10-03T12:00:00.000Z",
        endsAt: "2026-10-03T13:00:00.000Z",
      }),
    );
  });

  it("creates a complete timebox plan through the atomic batch endpoint", async () => {
    mocks.events.postTestingEventsSlotsBatch.mockResolvedValueOnce({
      ok: true,
      data: [{ id: "slot-1" }, { id: "slot-2" }],
    });
    const data = form({
      eventId: "event-1",
      mode: "Online",
      meetingUrl: "https://meet.example.test/playtest",
      timeZoneId: "America/Sao_Paulo",
      slotsJson: JSON.stringify([
        { startsAt: "2026-10-03T09:00", endsAt: "2026-10-03T09:45" },
        { startsAt: "2026-10-03T10:00", endsAt: "2026-10-03T10:45" },
      ]),
    });

    const result = await createTestingEventSlots(data);

    expect(result.success).toBe(true);
    expect(mocks.events.postTestingEventsSlotsBatch).toHaveBeenCalledWith(
      "event-1",
      {
        slots: [
          expect.objectContaining({
            startsAt: "2026-10-03T12:00:00.000Z",
            endsAt: "2026-10-03T12:45:00.000Z",
          }),
          expect.objectContaining({
            startsAt: "2026-10-03T13:00:00.000Z",
            endsAt: "2026-10-03T13:45:00.000Z",
          }),
        ],
      },
    );
  });

  it("requires a rationale to reject a project application", async () => {
    const result = await rejectTestingEventApplication(
      form({ applicationId: "application-1", rationale: " " }),
    );

    expect(result).toEqual({
      success: false,
      error: "A rejection rationale is required.",
    });
    expect(
      mocks.events.postTestingEventsApplicationsReject,
    ).not.toHaveBeenCalled();
  });

  it("maps an event lifecycle operation to its generated client method", async () => {
    mocks.events.postTestingEventsOpenApplications.mockResolvedValue({
      ok: true,
      data: { id: "event-1", status: "ApplicationsOpen" },
    });

    const result = await transitionTestingEvent(
      form({ eventId: "event-1", transition: "open-applications" }),
    );

    expect(result.success).toBe(true);
    expect(mocks.events.postTestingEventsOpenApplications).toHaveBeenCalledWith(
      "event-1",
    );
  });

  it("adds a committee member through the generated event client", async () => {
    mocks.events.postTestingEventsCommittee.mockResolvedValue({
      ok: true,
      data: { id: "member-1", userId: "user-1", isChair: true },
    });

    const result = await addTestingEventCommitteeMember(
      form({ eventId: "event-1", userId: "user-1", isChair: "on" }),
    );

    expect(result.success).toBe(true);
    expect(mocks.events.postTestingEventsCommittee).toHaveBeenCalledWith(
      "event-1",
      {
        userId: "user-1",
        isChair: true,
      },
    );
  });

  it("cancels the current tester registration through the participation client", async () => {
    mocks.participation.deleteTestingEventsRegistrations.mockResolvedValue({
      ok: true,
      data: true,
    });

    const result = await cancelTestingEventRegistration(
      form({ eventId: "event-1", registrationId: "registration-1" }),
    );

    expect(result.success).toBe(true);
    expect(
      mocks.participation.deleteTestingEventsRegistrations,
    ).toHaveBeenCalledWith("registration-1");
    expect(mocks.revalidatePath).toHaveBeenCalledWith(
      "/testing-lab/events/event-1",
    );
  });

  it("submits required structured feedback for an assigned project", async () => {
    mocks.participation.postTestingEventsFeedbackObligationsFeedback.mockResolvedValue(
      {
        ok: true,
        data: { id: "feedback-1" },
      },
    );

    const result = await submitTestingEventFeedback(
      form({
        eventId: "event-1",
        obligationId: "obligation-1",
        questionnaireRevisionId: "11111111-1111-1111-1111-111111111111",
        responsesJson: JSON.stringify({
          answers: [
            {
              questionId: "clarity",
              textValue: "The onboarding and controls are clear.",
            },
          ],
        }),
        overallRating: "8",
        wouldRecommend: "true",
        additionalNotes: "Retest after the tutorial polish.",
      }),
    );

    expect(result.success).toBe(true);
    expect(
      mocks.participation.postTestingEventsFeedbackObligationsFeedback,
    ).toHaveBeenCalledWith("obligation-1", {
      questionnaireRevisionId: "11111111-1111-1111-1111-111111111111",
      responses: {
        answers: [
          {
            questionId: "clarity",
            textValue: "The onboarding and controls are clear.",
          },
        ],
      },
      overallRating: 8,
      wouldRecommend: true,
      additionalNotes: "Retest after the tutorial polish.",
    });
  });

  it("saves a complete draft event configuration through the generated client", async () => {
    mocks.events.putTestingEventsConfiguration.mockResolvedValue({
      ok: true,
      data: { id: "event-1" },
    });
    const schema = JSON.stringify({ title: "Application", questions: [] });
    const result = await configureTestingEvent(
      form({
        eventId: "event-1",
        generalRules: "Respect the code of conduct.",
        candidateInstructions: "Provide a playable build.",
        testerInstructions: "Complete the assigned tasks.",
        projectApplicationSchemaJson: schema,
        testerRegistrationSchemaJson: schema,
      }),
    );

    expect(result.success).toBe(true);
    expect(mocks.events.putTestingEventsConfiguration).toHaveBeenCalledWith(
      "event-1",
      expect.objectContaining({
        generalRules: "Respect the code of conduct.",
        projectApplicationSchema: { title: "Application", questions: [] },
      }),
    );
  });

  it("creates and archives a versioned event template", async () => {
    mocks.templates.postVTestingTemplates.mockResolvedValue({
      ok: true,
      data: { id: "template-1", currentRevisionNumber: 1 },
    });
    mocks.templates.postVTestingTemplatesArchive.mockResolvedValue({
      ok: true,
      data: { id: "template-1", isArchived: true },
    });
    const schema = JSON.stringify({ title: "Application", questions: [] });
    const created = await saveTestingEventTemplate(
      form({
        name: "Campus playtest",
        generalRules: "Respect the code of conduct.",
        candidateInstructions: "Provide a playable build.",
        testerInstructions: "Complete the assigned tasks.",
        projectApplicationSchemaJson: schema,
        testerRegistrationSchemaJson: schema,
        defaultMode: "InPerson",
        defaultApprovalMode: "Committee",
        defaultRequiresFeedback: "on",
      }),
    );
    const archived = await setTestingEventTemplateArchived(
      form({ templateId: "template-1" }),
    );

    expect(created.success).toBe(true);
    expect(archived.success).toBe(true);
    expect(mocks.templates.postVTestingTemplates).toHaveBeenCalledWith(
      "1",
      expect.objectContaining({ defaultRequiresFeedback: true }),
    );
    expect(mocks.templates.postVTestingTemplatesArchive).toHaveBeenCalledWith(
      "template-1",
      "1",
    );
  });

  it("validates feedback before calling the participation client", async () => {
    const result = await submitTestingEventFeedback(
      form({
        obligationId: "obligation-1",
        responsesJson: '{"answers":[]}',
        overallRating: "11",
      }),
    );

    expect(result).toEqual({
      success: false,
      error:
        "Complete the assigned questionnaire and provide a rating from 1 to 10.",
    });
    expect(
      mocks.participation.postTestingEventsFeedbackObligationsFeedback,
    ).not.toHaveBeenCalled();
  });

  it("executes the complete event, slot, committee, application, and attendance lifecycle", async () => {
    const ok = { ok: true, data: { id: "result-1" } };
    [
      mocks.events.putTestingEvents,
      mocks.events.deleteTestingEvents,
      mocks.events.postTestingEventsCloseApplications,
      mocks.events.postTestingEventsSchedule,
      mocks.events.postTestingEventsActivate,
      mocks.events.postTestingEventsComplete,
      mocks.events.postTestingEventsCancel,
      mocks.events.postTestingEventsSlots,
      mocks.events.putTestingEventsSlots,
      mocks.events.deleteTestingEventsSlots,
      mocks.events.deleteTestingEventsCommittee,
      mocks.events.putTestingEventsLearning,
      mocks.events.postTestingEventsApplications,
      mocks.events.putTestingEventsApplications,
      mocks.events.postTestingEventsApplicationsWithdraw,
      mocks.events.postTestingEventsApplicationsReview,
      mocks.events.postTestingEventsApplicationsVotes,
      mocks.events.postTestingEventsApplicationsApprove,
      mocks.events.postTestingEventsApplicationsReject,
      mocks.events.postTestingEventsApplicationsWaitlist,
      mocks.events.putTestingEventsApplicationsSlot,
      mocks.participation.postTestingEventsSlotsRegistrations,
      mocks.participation.postTestingEventsRegistrationsCheckIn,
      mocks.participation.postTestingEventsRegistrationsCheckOut,
      mocks.participation.postTestingEventsRegistrationsNoShow,
      mocks.participation.postTestingEventsRegistrationsComplete,
      mocks.participation.postTestingEventsRegistrationsTestedProjects,
    ].forEach((mock) => mock.mockResolvedValue(ok));

    const eventFields = {
      eventId: "event-1",
      name: "Updated playtest",
      applicationsOpenAt: "2026-10-01T09:00",
      applicationsCloseAt: "2026-10-02T09:00",
      startsAt: "2026-10-03T09:00",
      endsAt: "2026-10-03T12:00",
      timeZoneId: "UTC",
    };
    const slotFields = {
      eventId: "event-1",
      slotId: "slot-1",
      mode: "Online",
      meetingUrl: "https://meet.example.test/playtest",
      startsAt: "2026-10-03T09:00:00.000Z",
      endsAt: "2026-10-03T12:00:00.000Z",
      maxTesters: "20",
      maxProjects: "5",
    };

    const results = await Promise.all([
      updateTestingEvent(form(eventFields)),
      deleteTestingEvent(form({ eventId: "event-1" })),
      ...["close-applications", "schedule", "activate", "complete"].map((transition) =>
        transitionTestingEvent(form({ eventId: "event-1", transition })),
      ),
      transitionTestingEvent(form({ eventId: "event-1", transition: "cancel", reason: "Weather" })),
      createTestingEventSlot(form(slotFields)),
      updateTestingEventSlot(form(slotFields)),
      deleteTestingEventSlot(form({ eventId: "event-1", slotId: "slot-1" })),
      removeTestingEventCommitteeMember(form({ eventId: "event-1", userId: "reviewer-1" })),
      configureTestingEventLearning(
        form({
          eventId: "event-1",
          courseId: "course-1",
          cohortId: "cohort-1",
          learningActivityId: "activity-1",
          requirement: "Completed",
        }),
      ),
      submitTestingProjectApplication(
        form({
          eventId: "event-1",
          projectId: "project-1",
          projectVersionId: "version-1",
          preferredAvailability: "Morning",
        }),
      ),
      updateTestingProjectApplication(
        form({
          eventId: "event-1",
          applicationId: "application-1",
          projectVersionId: "version-2",
        }),
      ),
      withdrawTestingProjectApplication(form({ eventId: "event-1", applicationId: "application-1" })),
      beginTestingEventApplicationReview(form({ eventId: "event-1", applicationId: "application-1" })),
      voteOnTestingEventApplication(
        form({ eventId: "event-1", applicationId: "application-1", decision: "Approve", comments: "Ready" }),
      ),
      approveTestingEventApplication(
        form({ eventId: "event-1", applicationId: "application-1", slotId: "slot-1", rationale: "Fit" }),
      ),
      rejectTestingEventApplication(
        form({ eventId: "event-1", applicationId: "application-2", rationale: "Build is incomplete" }),
      ),
      waitlistTestingEventApplication(
        form({ eventId: "event-1", applicationId: "application-3", rationale: "Capacity" }),
      ),
      assignTestingEventApplicationSlot(
        form({ eventId: "event-1", applicationId: "application-1", slotId: "slot-2" }),
      ),
      registerForTestingEventSlot(
        form({
          eventId: "event-1",
          slotId: "slot-1",
          notes: "Accessibility support requested",
          acceptedRules: "true",
          registrationResponseJson: '{"answers":[]}',
        }),
      ),
      ...["check-in", "check-out", "no-show", "complete"].map((attendance) =>
        updateTestingEventAttendance(form({ eventId: "event-1", registrationId: "registration-1", attendance })),
      ),
      assignTestedProjectToRegistration(
        form({ eventId: "event-1", registrationId: "registration-1", applicationId: "application-1" }),
      ),
    ]);

    expect(results).toHaveLength(27);
    expect(results.every((result) => result.success)).toBe(true);
    expect(mocks.events.putTestingEvents).toHaveBeenCalledWith(
      "event-1",
      expect.not.objectContaining({ recurrence: expect.anything() }),
    );
    expect(mocks.events.postTestingEventsCancel).toHaveBeenCalledWith("event-1", { reason: "Weather" });
    expect(mocks.events.putTestingEventsLearning).toHaveBeenCalledWith(
      "event-1",
      expect.objectContaining({ courseId: "course-1", cohortId: "cohort-1", requirement: "Completed" }),
    );
    expect(mocks.participation.postTestingEventsSlotsRegistrations).toHaveBeenCalledWith(
      "slot-1",
      expect.objectContaining({ acceptedRules: true, registrationResponse: { answers: [] } }),
    );
  });

  it("creates, updates, submits, and resumes project application drafts", async () => {
    mocks.events.postTestingEventsApplicationsDrafts.mockResolvedValue({
      ok: true,
      data: { id: "application-1", status: "Draft" },
    });
    mocks.events.putTestingEventsApplicationsDraft.mockResolvedValue({
      ok: true,
      data: { id: "application-1", status: "Draft" },
    });
    mocks.events.postTestingEventsApplicationsSubmit.mockResolvedValue({
      ok: true,
      data: { id: "application-1", status: "Submitted" },
    });

    const submitted = await saveTestingProjectApplicationDraft(
      form({
        eventId: "event-1",
        projectId: "project-1",
        projectVersionId: "version-1",
        briefJson: '{"summary":"Vertical slice"}',
        feedbackQuestionnaireJson: '{"title":"Feedback","questions":[]}',
        eventApplicationResponseJson: '{"answers":[]}',
        acceptedRules: "true",
        preferredAvailability: "Evening",
        submittedAssetReferenceIdsJson: '["asset-1"]',
        intent: "submit",
      }),
    );
    expect(submitted).toMatchObject({ success: true, message: "Project application submitted." });

    mocks.events.putTestingEventsApplicationsDraft.mockResolvedValueOnce({
      ok: true,
      data: { id: "application-1", status: "Submitted" },
    });
    const resumed = await saveTestingProjectApplicationDraft(
      form({ eventId: "event-1", projectId: "project-1", applicationId: "application-1" }),
    );
    expect(resumed).toMatchObject({ success: true, message: "Application draft saved." });
    expect(mocks.events.putTestingEventsApplicationsDraft).toHaveBeenLastCalledWith(
      "application-1",
      expect.objectContaining({
        projectVersionId: undefined,
        brief: undefined,
        feedbackQuestionnaire: undefined,
        eventApplicationResponse: undefined,
        acceptedRules: undefined,
        submittedAssetReferenceIds: [],
      }),
    );
  });

  it("updates and restores event calendars through the versioned template client", async () => {
    mocks.templates.putVTestingTemplates.mockResolvedValue({ ok: true, data: { id: "template-1" } });
    mocks.templates.postVTestingTemplatesRestore.mockResolvedValue({ ok: true, data: { id: "template-1", isArchived: false } });
    const schema = JSON.stringify({ title: "Application", questions: [] });

    const updated = await saveTestingEventTemplate(
      form({
        templateId: "template-1",
        name: "Revised calendar",
        projectApplicationSchemaJson: schema,
        testerRegistrationSchemaJson: schema,
      }),
    );
    const restored = await setTestingEventTemplateArchived(
      form({ templateId: "template-1", restore: "true" }),
    );

    expect(updated).toMatchObject({ success: true, message: "New calendar revision saved." });
    expect(restored).toMatchObject({ success: true, message: "Calendar restored." });
    expect(mocks.templates.putVTestingTemplates).toHaveBeenCalledWith(
      "template-1",
      "1",
      expect.objectContaining({ defaultMode: "Online", defaultApprovalMode: "ManagerOnly" }),
    );
  });

  it("rejects incomplete lifecycle commands before invoking a client", async () => {
    const empty = new FormData();
    const invalid = await Promise.all([
      configureTestingEvent(empty),
      saveTestingEventTemplate(empty),
      setTestingEventTemplateArchived(empty),
      createTestingEvent(empty),
      updateTestingEvent(empty),
      deleteTestingEvent(empty),
      archiveTestingEvent(empty),
      restoreTestingEvent(empty),
      transitionTestingEvent(empty),
      createTestingEventSlot(empty),
      updateTestingEventSlot(empty),
      deleteTestingEventSlot(empty),
      addTestingEventCommitteeMember(empty),
      removeTestingEventCommitteeMember(empty),
      configureTestingEventLearning(empty),
      submitTestingProjectApplication(empty),
      saveTestingProjectApplicationDraft(empty),
      updateTestingProjectApplication(empty),
      withdrawTestingProjectApplication(empty),
      beginTestingEventApplicationReview(empty),
      voteOnTestingEventApplication(empty),
      approveTestingEventApplication(empty),
      rejectTestingEventApplication(empty),
      waitlistTestingEventApplication(empty),
      assignTestingEventApplicationSlot(empty),
      registerForTestingEventSlot(empty),
      updateTestingEventAttendance(empty),
      assignTestedProjectToRegistration(empty),
      cancelTestingEventRegistration(empty),
      submitTestingEventFeedback(empty),
    ]);

    expect(invalid).toHaveLength(30);
    expect(invalid.every((result) => !result.success && result.error.length > 0)).toBe(true);
  });

  it("validates time zones, schedules, recurrence limits, slots, transitions, attendance, and feedback bounds", async () => {
    const base = {
      name: "Playtest",
      applicationsOpenAt: "2026-10-01T09:00",
      applicationsCloseAt: "2026-10-02T09:00",
      startsAt: "2026-10-03T09:00",
      endsAt: "2026-10-03T12:00",
    };
    const outcomes = await Promise.all([
      createTestingEvent(form({ ...base, timeZoneId: "Mars/Olympus" })),
      createTestingEvent(form({ ...base, endsAt: "2026-10-03T08:00" })),
      createTestingEvent(form({ ...base, startsAt: "invalid" })),
      createTestingEvent(form({ ...base, recurrenceFrequency: "Daily", recurrenceInterval: "0", recurrenceOccurrenceCount: "2" })),
      createTestingEvent(form({ ...base, recurrenceFrequency: "Weekly", recurrenceOccurrenceCount: "2" })),
      createTestingEvent(form({ ...base, recurrenceFrequency: "Daily", recurrenceEndMode: "date", recurrenceEndsAt: "invalid" })),
      createTestingEvent(form({ ...base, recurrenceFrequency: "Daily", recurrenceOccurrenceCount: "0" })),
      createTestingEventSlot(form({ eventId: "event-1", startsAt: "invalid", endsAt: "invalid" })),
      createTestingEventSlot(
        form({ eventId: "event-1", mode: "Online", startsAt: "2026-10-03T09:00Z", endsAt: "2026-10-03T10:00Z" }),
      ),
      transitionTestingEvent(form({ eventId: "event-1", transition: "publish" })),
      transitionTestingEvent(form({ eventId: "event-1", transition: "cancel" })),
      updateTestingEventAttendance(form({ registrationId: "registration-1", attendance: "late" })),
      submitTestingEventFeedback(
        form({ obligationId: "obligation-1", questionnaireRevisionId: "revision-1", responsesJson: "invalid", overallRating: "5" }),
      ),
      submitTestingEventFeedback(
        form({ obligationId: "obligation-1", questionnaireRevisionId: "revision-1", responsesJson: '{"answers":[]}', overallRating: "0" }),
      ),
    ]);

    expect(outcomes.every((result) => !result.success)).toBe(true);
    expect(outcomes.map((result) => (result.success ? "" : result.error))).toEqual(
      expect.arrayContaining([
        "Choose a valid time zone.",
        "Event end must be after its start.",
        "Repeat interval must be between 1 and 52.",
        "Choose at least one weekday for a weekly event.",
        "Choose when the recurrence ends.",
        "Choose between 1 and 104 occurrences.",
        "A meeting URL is required for online slots.",
        "Choose a valid event transition.",
        "A cancellation reason is required.",
        "Choose a valid attendance action.",
      ]),
    );
  });

  it("returns actionable errors when draft creation, save, or submission fails", async () => {
    mocks.events.postTestingEventsApplicationsDrafts.mockResolvedValueOnce({
      ok: false,
      error: { message: "TestingLab.Validation", detail: "Project is not eligible." },
    });
    await expect(
      saveTestingProjectApplicationDraft(form({ eventId: "event-1", projectId: "project-1" })),
    ).resolves.toEqual({ success: false, error: "Project is not eligible." });

    mocks.events.postTestingEventsApplicationsDrafts.mockResolvedValueOnce({
      ok: true,
      data: { id: null, status: "Draft" },
    });
    await expect(
      saveTestingProjectApplicationDraft(form({ eventId: "event-1", projectId: "project-1" })),
    ).resolves.toEqual({ success: false, error: "The application draft could not be created." });

    mocks.events.putTestingEventsApplicationsDraft.mockResolvedValueOnce({
      ok: false,
      error: { message: "Draft rejected" },
    });
    await expect(
      saveTestingProjectApplicationDraft(
        form({ eventId: "event-1", projectId: "project-1", applicationId: "application-1" }),
      ),
    ).resolves.toEqual({ success: false, error: "Draft rejected" });

    mocks.events.putTestingEventsApplicationsDraft.mockResolvedValueOnce({
      ok: true,
      data: { id: "application-1", status: "Draft" },
    });
    mocks.events.postTestingEventsApplicationsSubmit.mockResolvedValueOnce({
      ok: false,
      error: { message: "Submit rejected" },
    });
    await expect(
      saveTestingProjectApplicationDraft(
        form({ eventId: "event-1", projectId: "project-1", applicationId: "application-1", intent: "submit" }),
      ),
    ).resolves.toEqual({ success: false, error: "Submit rejected" });

    mocks.events.putTestingEventsApplicationsDraft.mockRejectedValueOnce({ detail: "Network unavailable" });
    await expect(
      saveTestingProjectApplicationDraft(
        form({ eventId: "event-1", projectId: "project-1", applicationId: "application-1" }),
      ),
    ).resolves.toEqual({ success: false, error: "Network unavailable" });
  });

  it("covers optional event context, defensive API errors, and non-finite inputs", async () => {
    const base = {
      name: "Playtest",
      applicationsOpenAt: "2026-10-01T09:00",
      applicationsCloseAt: "2026-10-02T09:00",
      startsAt: "2026-10-03T09:00",
      endsAt: "2026-10-03T12:00",
    };

    const incompleteConfiguration = form(base);
    incompleteConfiguration.delete("candidateInstructions");
    await expect(createTestingEvent(incompleteConfiguration)).resolves.toEqual({
      success: false,
      error: "Choose a template or enter rules and instructions for the event.",
    });

    await expect(updateTestingEvent(form(base))).resolves.toEqual({
      success: false,
      error: "Event and valid event details are required.",
    });
    await expect(
      updateTestingEventSlot(
        form({ eventId: "event-1", slotId: "slot-1", startsAt: "invalid", endsAt: "invalid" }),
      ),
    ).resolves.toEqual({ success: false, error: "Enter a valid slot schedule." });

    const recurrence = form({
      ...base,
      recurrenceFrequency: "Weekly",
      recurrenceOccurrenceCount: "2",
    });
    recurrence.append("recurrenceDaysOfWeek", new Blob(["ignored"]), "ignored.txt");
    recurrence.append("recurrenceDaysOfWeek", "Notaday");
    recurrence.append("recurrenceDaysOfWeek", "Monday");
    mocks.events.postTestingEvents.mockResolvedValueOnce({ ok: true, data: undefined });
    await expect(createTestingEvent(recurrence)).resolves.toMatchObject({ success: true, data: null });

    mocks.events.postTestingEvents.mockRejectedValueOnce(new Error("Provider unavailable"));
    await expect(createTestingEvent(form(base))).resolves.toEqual({
      success: false,
      error: "Provider unavailable",
    });
    mocks.events.postTestingEvents.mockRejectedValueOnce({ detail: 42 });
    await expect(createTestingEvent(form(base))).resolves.toEqual({
      success: false,
      error: "The Testing Lab event operation failed.",
    });
    mocks.events.postTestingEvents.mockRejectedValueOnce(null);
    await expect(createTestingEvent(form(base))).resolves.toEqual({
      success: false,
      error: "The Testing Lab event operation failed.",
    });

    mocks.events.postTestingEventsSlots.mockResolvedValueOnce({ ok: true, data: { id: "slot-1" } });
    await expect(
      createTestingEventSlot(
        form({
          eventId: "event-1",
          mode: "InPerson",
          campusName: "Main campus",
          roomName: "Lab 1",
          startsAt: "2026-10-03T09:00Z",
          endsAt: "2026-10-03T10:00Z",
          maxTesters: "not-a-number",
        }),
      ),
    ).resolves.toMatchObject({ success: true });
    expect(mocks.events.postTestingEventsSlots).toHaveBeenLastCalledWith(
      "event-1",
      expect.objectContaining({ maxTesters: null }),
    );
    await expect(
      createTestingEventSlot(
        form({
          eventId: "event-1",
          mode: "InPerson",
          campusName: "Main campus",
          startsAt: "2026-10-03T09:00Z",
          endsAt: "2026-10-03T10:00Z",
        }),
      ),
    ).resolves.toEqual({ success: false, error: "Campus and room are required for in-person slots." });

    const ok = { ok: true, data: { id: "result-1" } };
    [
      mocks.events.putTestingEventsApplications,
      mocks.events.postTestingEventsApplicationsWithdraw,
      mocks.events.postTestingEventsApplicationsReview,
      mocks.events.postTestingEventsApplicationsVotes,
      mocks.events.postTestingEventsApplicationsApprove,
      mocks.events.postTestingEventsApplicationsReject,
      mocks.events.postTestingEventsApplicationsWaitlist,
      mocks.events.putTestingEventsApplicationsSlot,
      mocks.participation.postTestingEventsSlotsRegistrations,
      mocks.participation.postTestingEventsRegistrationsCheckIn,
      mocks.participation.postTestingEventsRegistrationsTestedProjects,
      mocks.participation.deleteTestingEventsRegistrations,
      mocks.participation.postTestingEventsFeedbackObligationsFeedback,
    ].forEach((mock) => mock.mockResolvedValueOnce(ok));

    const withoutEventContext = await Promise.all([
      updateTestingProjectApplication(form({ applicationId: "application-1", projectVersionId: "version-1" })),
      withdrawTestingProjectApplication(form({ applicationId: "application-1" })),
      beginTestingEventApplicationReview(form({ applicationId: "application-1" })),
      voteOnTestingEventApplication(form({ applicationId: "application-1", decision: "Approve" })),
      approveTestingEventApplication(form({ applicationId: "application-1", slotId: "slot-1" })),
      rejectTestingEventApplication(form({ applicationId: "application-1", rationale: "Incomplete" })),
      waitlistTestingEventApplication(form({ applicationId: "application-1" })),
      assignTestingEventApplicationSlot(form({ applicationId: "application-1", slotId: "slot-1" })),
      registerForTestingEventSlot(form({ slotId: "slot-1" })),
      updateTestingEventAttendance(form({ registrationId: "registration-1", attendance: "check-in" })),
      assignTestedProjectToRegistration(form({ registrationId: "registration-1", applicationId: "application-1" })),
      cancelTestingEventRegistration(form({ registrationId: "registration-1" })),
      submitTestingEventFeedback(
        form({
          obligationId: "obligation-1",
          questionnaireRevisionId: "revision-1",
          responsesJson: '{"answers":[]}',
          overallRating: "10",
        }),
      ),
    ]);
    expect(withoutEventContext.every((result) => result.success)).toBe(true);

    mocks.events.putTestingEventsApplicationsDraft.mockRejectedValueOnce(new Error("Draft network failure"));
    await expect(
      saveTestingProjectApplicationDraft(
        form({ eventId: "event-1", projectId: "project-1", applicationId: "application-1" }),
      ),
    ).resolves.toEqual({ success: false, error: "Draft network failure" });
  });
});
