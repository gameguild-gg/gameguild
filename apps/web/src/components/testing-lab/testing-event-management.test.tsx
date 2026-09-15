import {
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { TestingLabTestingEventTemplateProjection } from "@game-guild/client";
import { renderToString } from "react-dom/server";
import { beforeEach, describe, expect, it, vi } from "vitest";

global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};
Element.prototype.scrollIntoView = vi.fn();

const mocks = vi.hoisted(() => ({
  beginReview: vi.fn(),
  createEvent: vi.fn(),
  createSlot: vi.fn(),
  deleteEvent: vi.fn(),
  push: vi.fn(),
  refresh: vi.fn(),
  transitionEvent: vi.fn(),
}));

vi.mock("next/navigation", () => ({
  usePathname: () => "/workspace/learning",
  useRouter: () => ({ push: mocks.push, refresh: mocks.refresh }),
}));

vi.mock("@/lib/testing-lab/events-actions", () => ({
  addTestingEventCommitteeMember: vi.fn(),
  assignTestedProjectToRegistration: vi.fn(),
  approveTestingEventApplication: vi.fn(),
  beginTestingEventApplicationReview: mocks.beginReview,
  configureTestingEventLearning: vi.fn(),
  createTestingEvent: mocks.createEvent,
  createTestingEventSlot: mocks.createSlot,
  archiveTestingEvent: vi.fn(),
  deleteTestingEvent: mocks.deleteEvent,
  deleteTestingEventSlot: vi.fn(),
  rejectTestingEventApplication: vi.fn(),
  removeTestingEventCommitteeMember: vi.fn(),
  restoreTestingEvent: vi.fn(),
  transitionTestingEvent: mocks.transitionEvent,
  updateTestingEventAttendance: vi.fn(),
  updateTestingEvent: vi.fn(),
  updateTestingEventSlot: vi.fn(),
  voteOnTestingEventApplication: vi.fn(),
  waitlistTestingEventApplication: vi.fn(),
}));

import {
  apiDatetimeLocal,
  createTestingEventSchedule,
  CreateTestingEventDialog,
  CreateTestingEventSlotDialog,
  EditTestingEventDialog,
  ManageTestingEventSlotDialog,
  preferredNewEventTimeZone,
  scheduleDate,
  TestingEventApplications,
  TestingEventLifecycleActions,
  updateTestingEventSchedule,
} from "./testing-event-management";

describe("TestingEventApplications", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.createEvent.mockResolvedValue({
      success: true,
      data: { id: "event-created" },
      message: "Event created.",
    });
    mocks.createSlot.mockResolvedValue({
      success: true,
      data: { id: "slot-created" },
      message: "Slot created.",
    });
    mocks.deleteEvent.mockResolvedValue({
      success: true,
      data: null,
      message: "Event deleted.",
    });
    mocks.transitionEvent.mockResolvedValue({
      success: true,
      data: null,
      message: "Event transitioned.",
    });
  });

  it("normalizes API dates and rejects empty or invalid instants", () => {
    expect(apiDatetimeLocal()).toBe("");
    expect(apiDatetimeLocal("not-a-date")).toBe("");
    expect(apiDatetimeLocal("2026-08-11T17:00:00Z", "America/Sao_Paulo")).toBe(
      "2026-08-11T14:00",
    );
    expect(scheduleDate("")).toBeNull();
    expect(scheduleDate("not-a-date")).toBeNull();
    expect(scheduleDate("2030-01-01T12:00")?.getFullYear()).toBe(2030);
  });

  it("creates a chronological default schedule even for an early selected day", () => {
    const schedule = createTestingEventSchedule(
      new Date(2030, 0, 1, 8, 17),
      new Date(2030, 0, 1),
    );

    expect(new Date(schedule.applicationsCloseAt).valueOf()).toBeGreaterThan(
      new Date(schedule.applicationsOpenAt).valueOf(),
    );
    expect(new Date(schedule.startsAt).valueOf()).toBeGreaterThanOrEqual(
      new Date(schedule.applicationsCloseAt).valueOf(),
    );
    expect(
      new Date(schedule.endsAt).valueOf() - new Date(schedule.startsAt).valueOf(),
    ).toBe(2 * 60 * 60 * 1000);
  });

  it("repairs dependent schedule windows when dates move", () => {
    const original = {
      applicationsOpenAt: "2030-01-01T09:00",
      applicationsCloseAt: "2030-01-01T10:00",
      startsAt: "2030-01-01T11:00",
      endsAt: "2030-01-01T13:00",
    };

    const openMoved = updateTestingEventSchedule(
      original,
      "applicationsOpenAt",
      "2030-01-02T09:00",
    );
    expect(openMoved.applicationsCloseAt).toBe("2030-01-03T09:00");
    expect(openMoved.startsAt).toBe("2030-01-04T09:00");
    expect(openMoved.endsAt).toBe("2030-01-04T11:00");

    const startMoved = updateTestingEventSchedule(
      original,
      "startsAt",
      "2030-01-05T12:00",
    );
    expect(startMoved.endsAt).toBe("2030-01-05T14:00");

    const validEnd = updateTestingEventSchedule(
      original,
      "endsAt",
      "2030-01-01T14:00",
    );
    expect(validEnd.endsAt).toBe("2030-01-01T14:00");
  });

  it("uses explicit non-UTC event time zones", () => {
    expect(preferredNewEventTimeZone("Europe/Paris")).toBe("Europe/Paris");
  });
  it("shows human labels and refreshes the SSR view after review starts", async () => {
    mocks.beginReview.mockResolvedValue({
      success: true,
      data: { id: "application-1" },
      message: "Application review started.",
    });

    render(
      <TestingEventApplications
        eventId="event-1"
        access={{ canManageApplications: true, canVote: false }}
        applications={[
          {
            id: "application-1",
            projectId: "project-1",
            submittedByUserId: "user-1",
            status: "Pending",
          },
        ]}
        slots={[]}
        projectLabels={{ "project-1": "Orbit Tactics" }}
        memberLabels={{ "user-1": "Ana Reviewer / ana@example.test" }}
      />,
    );

    expect(screen.getByText("Orbit Tactics")).toBeInTheDocument();
    expect(
      screen.getByText("Submitted by Ana Reviewer / ana@example.test"),
    ).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Review" }));

    await waitFor(() => {
      expect(mocks.beginReview).toHaveBeenCalledOnce();
      expect(mocks.refresh).toHaveBeenCalledOnce();
    });
    expect(screen.getByText("Application review started.")).toBeInTheDocument();
  });

  it("exposes the approval slot selector with an accessible name", async () => {
    render(
      <TestingEventApplications
        eventId="event-1"
        access={{ canManageApplications: true, canVote: false }}
        applications={[
          {
            id: "application-1",
            projectId: "project-1",
            submittedByUserId: "user-1",
            status: "UnderReview",
          },
        ]}
        slots={[
          {
            id: "slot-1",
            startsAt: "2026-08-02T14:00:00.000Z",
            campusName: "GameGuild Campus",
          },
        ]}
        projectLabels={{ "project-1": "Orbit Tactics" }}
        memberLabels={{ "user-1": "Ana Reviewer / ana@example.test" }}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Approve" }));

    expect(
      await screen.findByRole("combobox", { name: "Testing slot" }),
    ).toBeInTheDocument();
  });

  it("gives committee reviewers only the vote action", () => {
    render(
      <TestingEventApplications
        eventId="event-1"
        access={{ canManageApplications: false, canVote: true }}
        applications={[
          {
            id: "application-1",
            projectId: "project-1",
            submittedByUserId: "user-1",
            status: "UnderReview",
          },
        ]}
        slots={[]}
        projectLabels={{ "project-1": "Orbit Tactics" }}
        memberLabels={{ "user-1": "Ana Reviewer / ana@example.test" }}
      />,
    );

    expect(screen.getByRole("button", { name: "Vote" })).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Approve" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Waitlist" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Reject" }),
    ).not.toBeInTheDocument();
  });

  it("does not give an event manager a committee vote action", () => {
    render(
      <TestingEventApplications
        eventId="event-1"
        access={{ canManageApplications: true, canVote: false }}
        applications={[{ id: "application-1", status: "UnderReview" }]}
        slots={[]}
      />,
    );

    expect(screen.getByRole("button", { name: "Approve" })).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Vote" }),
    ).not.toBeInTheDocument();
  });

  it("uses range calendars and keeps the event schedule chronological", () => {
    render(<CreateTestingEventDialog defaultTimeZone="America/Sao_Paulo" />);

    fireEvent.click(screen.getByRole("button", { name: "New event" }));
    const field = (name: string) =>
      document.querySelector<HTMLInputElement>(`input[name="${name}"]`)!;
    const applicationsOpenAt = field("applicationsOpenAt");
    const applicationsCloseAt = field("applicationsCloseAt");
    const startsAt = field("startsAt");
    const endsAt = field("endsAt");

    expect(
      document.querySelector('input[type="datetime-local"]'),
    ).not.toBeInTheDocument();
    expect(applicationsOpenAt.value).not.toBe("");
    expect(applicationsCloseAt.value).not.toBe("");
    expect(startsAt.value).not.toBe("");
    expect(endsAt.value).not.toBe("");
    expect(new Date(applicationsCloseAt.value).valueOf()).toBeGreaterThan(
      new Date(applicationsOpenAt.value).valueOf(),
    );
    expect(new Date(startsAt.value).valueOf()).toBeGreaterThanOrEqual(
      new Date(applicationsCloseAt.value).valueOf(),
    );
    expect(new Date(endsAt.value).valueOf()).toBeGreaterThan(
      new Date(startsAt.value).valueOf(),
    );

    fireEvent.click(screen.getByRole("combobox", { name: "Time zone" }));
    fireEvent.change(screen.getByPlaceholderText("Search time zones…"), {
      target: { value: "America/New_York" },
    });
    fireEvent.click(screen.getByText("America/New_York"));
    expect(field("timeZoneId").value).toBe("America/New_York");

    fireEvent.click(screen.getByRole("button", { name: "Event schedule" }));
    const nextMinute = String(
      new Date(startsAt.value).getMinutes() + 1,
    ).padStart(2, "0");
    fireEvent.change(screen.getByLabelText("Start time"), {
      target: { value: `22:${nextMinute}` },
    });
    fireEvent.click(
      screen.getByRole("button", { name: "Apply event schedule" }),
    );

    expect(
      new Date(endsAt.value).valueOf() - new Date(startsAt.value).valueOf(),
    ).toBe(2 * 60 * 60 * 1000);

    fireEvent.change(screen.getByLabelText("Event name"), {
      target: { value: "Community playtest" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Cancel" }));

    expect(screen.getByRole("alertdialog")).toHaveTextContent(
      "Discard testing event draft?",
    );

    fireEvent.click(screen.getByRole("button", { name: "Keep editing" }));

    expect(screen.getByText("New testing event")).toBeInTheDocument();
  });

  it("uses the user's browser timezone when the lab still uses UTC", () => {
    const resolvedOptions = Intl.DateTimeFormat.prototype.resolvedOptions;
    const timeZone = vi
      .spyOn(Intl.DateTimeFormat.prototype, "resolvedOptions")
      .mockImplementation(function (this: Intl.DateTimeFormat) {
        return {
          ...resolvedOptions.call(this),
          timeZone: "America/Sao_Paulo",
        };
      });

    try {
      render(<CreateTestingEventDialog defaultTimeZone="UTC" />);
      fireEvent.click(screen.getByRole("button", { name: "New event" }));

    expect(
      screen.getByRole("combobox", { name: "Time zone" }),
    ).toHaveTextContent("Sao Paulo");
      expect(
        document.querySelector<HTMLInputElement>('input[name="timeZoneId"]')
          ?.value,
      ).toBe("America/Sao_Paulo");
    } finally {
      timeZone.mockRestore();
    }
  });

  it("keeps the quick-create dialog focused on event identity and schedule", () => {
    render(<CreateTestingEventDialog />);

    fireEvent.click(screen.getByRole("button", { name: "New event" }));

    expect(screen.getByRole("textbox", { name: "Event name" })).toBeRequired();
    expect(
      screen.queryByText("Rules and instructions"),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("textbox", { name: "Purpose and tester brief" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByText("Require tester feedback"),
    ).not.toBeInTheDocument();
    expect(
      document.querySelector<HTMLInputElement>('input[name="requiresFeedback"]')
        ?.value,
    ).toBe("true");
  });

  it("uses an event calendar template when one is available", () => {
    const templates = [
      {
        id: "template-1",
        name: "Community playtests",
        currentRevision: { id: "revision-1" },
      },
    ] as TestingLabTestingEventTemplateProjection[];

    render(<CreateTestingEventDialog templates={templates} />);
    fireEvent.click(screen.getByRole("button", { name: "New event" }));

    expect(
      screen.getByRole("combobox", { name: "Event calendar" }),
    ).toHaveTextContent("Community playtests");
    expect(
      document.querySelector<HTMLInputElement>(
        'input[name="templateRevisionId"]',
      )?.value,
    ).toBe("revision-1");
  });

  it("presents the event decisions first and groups its two time windows", () => {
    render(<CreateTestingEventDialog defaultTimeZone="America/Sao_Paulo" />);

    fireEvent.click(screen.getByRole("button", { name: "New event" }));

    const eventName = screen.getByRole("textbox", { name: "Event name" });
    const eventFormat = screen.getByRole("combobox", {
      name: "Event format",
    });
    const projectReview = screen.getByRole("combobox", {
      name: "Project review",
    });
    expect(
      eventName.compareDocumentPosition(eventFormat) &
        Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
    expect(
      eventFormat.compareDocumentPosition(projectReview) &
        Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
    expect(eventFormat.closest("div")).toHaveClass(
      "sm:grid-cols-[7rem_minmax(0,1fr)]",
    );
    expect(projectReview.closest("div")).toHaveClass(
      "sm:grid-cols-[7rem_minmax(0,1fr)]",
    );

    const timeline = screen.getByRole("region", { name: "Schedule" });
    expect(
      within(timeline).getByRole("button", { name: "Application window" }),
    ).toHaveTextContent(
      /\d{2}\/\d{2}\/\d{4} · \d{2}:\d{2}(?:–\d{2}:\d{2}| → \d{2}\/\d{2}\/\d{4} · \d{2}:\d{2})/,
    );
    expect(
      within(timeline).getByRole("button", { name: "Event schedule" }),
    ).toHaveTextContent(
      /\d{2}\/\d{2}\/\d{4} · \d{2}:\d{2}(?:–\d{2}:\d{2}| → \d{2}\/\d{2}\/\d{4} · \d{2}:\d{2})/,
    );
    expect(within(timeline).getByText("Applications")).toBeInTheDocument();
    expect(within(timeline).getByText("Testing session")).toBeInTheDocument();
    expect(
      within(timeline).getByRole("combobox", { name: "Time zone" }),
    ).toHaveTextContent("Sao Paulo");

    expect(
      screen.getByRole("combobox", { name: "Repeats" }),
    ).toBeInTheDocument();
    expect(screen.queryByLabelText("Start from")).not.toBeInTheDocument();
    expect(screen.getByRole("dialog")).toHaveClass("sm:max-w-lg");
    expect(screen.getByRole("dialog")).toHaveAttribute(
      "data-slot",
      "dialog-content",
    );
  });

  it("uses calendar-style repeat presets and keeps custom controls collapsed", async () => {
    const user = userEvent.setup();
    render(<CreateTestingEventDialog initialDate={new Date(2030, 7, 19)} />);

    await user.click(screen.getByRole("button", { name: "New event" }));

    const repeats = screen.getByRole("combobox", { name: "Repeats" });
    expect(repeats).toHaveTextContent("Does not repeat");
    expect(screen.queryByLabelText("Repeat every")).not.toBeInTheDocument();

    await user.click(repeats);
    expect(
      screen.getByRole("option", { name: "Weekly on Monday" }),
    ).toBeInTheDocument();
    await user.click(screen.getByRole("option", { name: "Custom…" }));

    expect(screen.getByLabelText("Repeat every")).toBeInTheDocument();
    expect(
      screen.getByRole("combobox", { name: "Repeat unit" }),
    ).toHaveTextContent("Week(s)");
    expect(screen.getByLabelText("Number of events")).toHaveValue(4);
  });

  it("replaces the open action with a setup link while a draft is incomplete", () => {
    render(
      <TestingEventLifecycleActions
        event={{ id: "event-1", status: "Draft", configuration: undefined }}
      />,
    );

    expect(
      screen.getByRole("link", { name: "Complete setup" }),
    ).toHaveAttribute(
      "href",
      "/workspace/testing-lab/events/event-1/overview#event-configuration-heading",
    );
    expect(
      screen.queryByRole("button", { name: "Open applications" }),
    ).not.toBeInTheDocument();
  });

  it("server-renders the archive action for a cancelled event", () => {
    expect(() =>
      renderToString(
        <TestingEventLifecycleActions
          event={{ id: "event-1", status: "Cancelled" }}
        />,
      ),
    ).not.toThrow();
  });
  it("opens as a controlled sheet with the calendar day prefilled", () => {
    render(
      <CreateTestingEventDialog
        open
        showTrigger={false}
        initialDate={new Date(2030, 7, 19)}
        onOpenChange={vi.fn()}
      />,
    );

    expect(
      screen.queryByRole("button", { name: "New event" }),
    ).not.toBeInTheDocument();
    expect(
      document.querySelector<HTMLInputElement>('input[name="startsAt"]')?.value,
    ).toMatch(/^2030-08-19T/);
  });

  it("shows API instants in the event timezone when editing an event", () => {
    render(
      <EditTestingEventDialog
        event={{
          id: "event-1",
          name: "Timezone-safe playtest",
          applicationsOpenAt: "2026-08-11T17:00:00Z",
          applicationsCloseAt: "2026-08-13T16:00:00Z",
          startsAt: "2026-08-13T17:00:00Z",
          endsAt: "2026-08-13T19:00:00Z",
          mode: "Online",
          approvalMode: "ManagerOnly",
          status: "Draft",
          requiresFeedback: true,
          timeZoneId: "America/Sao_Paulo",
        }}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Edit" }));

    expect(
      screen.getByRole("textbox", { name: "Purpose and tester brief" }),
    ).toBeInTheDocument();
    expect(screen.getByText("Require tester feedback")).toBeInTheDocument();
    expect(
      document.querySelector<HTMLInputElement>(
        'input[name="applicationsOpenAt"]',
      )?.value,
    ).toBe("2026-08-11T14:00");
    expect(
      document.querySelector<HTMLInputElement>(
        'input[name="applicationsCloseAt"]',
      )?.value,
    ).toBe("2026-08-13T13:00");
    expect(
      document.querySelector<HTMLInputElement>('input[name="startsAt"]')?.value,
    ).toBe("2026-08-13T14:00");
    expect(
      document.querySelector<HTMLInputElement>('input[name="endsAt"]')?.value,
    ).toBe("2026-08-13T16:00");
    expect(
      document.querySelector<HTMLInputElement>('input[name="timeZoneId"]')
        ?.value,
    ).toBe("America/Sao_Paulo");
  });

  it("preserves API wall-clock values when editing a slot", () => {
    const timezoneOffset = vi
      .spyOn(Date.prototype, "getTimezoneOffset")
      .mockReturnValue(180);
    render(
      <ManageTestingEventSlotDialog
        eventId="event-1"
        slot={{
          id: "slot-1",
          eventId: "event-1",
          mode: "InPerson",
          startsAt: "2026-08-13T17:30:00Z",
          endsAt: "2026-08-13T18:30:00Z",
          campusName: "QA Campus",
          roomName: "QA Room 101",
        }}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Edit slot" }));

    expect(
      document.querySelector<HTMLInputElement>('input[name="startsAt"]')?.value,
    ).toBe("2026-08-13T17:30");
    expect(
      document.querySelector<HTMLInputElement>('input[name="endsAt"]')?.value,
    ).toBe("2026-08-13T18:30");
    timezoneOffset.mockRestore();
  });

  it("submits a new event, closes the dialog, and refreshes the route", async () => {
    const user = userEvent.setup();
    render(<CreateTestingEventDialog />);
    await user.click(screen.getByRole("button", { name: "New event" }));
    await user.type(screen.getByRole("textbox", { name: "Event name" }), "Ship night");
    await user.click(screen.getByRole("button", { name: "Create event" }));

    await waitFor(() => expect(mocks.createEvent).toHaveBeenCalledOnce());
    await waitFor(() => expect(mocks.refresh).toHaveBeenCalledOnce());
    expect(screen.queryByText("New testing event")).not.toBeInTheDocument();
  });

  it("keeps create errors visible for returned and rejected failures", async () => {
    const user = userEvent.setup();
    mocks.createEvent.mockResolvedValueOnce({
      success: false,
      error: "Event name is already in use.",
    });
    const { unmount } = render(<CreateTestingEventDialog />);
    await user.click(screen.getByRole("button", { name: "New event" }));
    await user.type(screen.getByRole("textbox", { name: "Event name" }), "Duplicate");
    await user.click(screen.getByRole("button", { name: "Create event" }));
    expect(await screen.findByText("Event name is already in use.")).toBeInTheDocument();
    unmount();

    mocks.createEvent.mockRejectedValueOnce("offline");
    render(<CreateTestingEventDialog />);
    await user.click(screen.getByRole("button", { name: "New event" }));
    await user.type(screen.getByRole("textbox", { name: "Event name" }), "Offline event");
    await user.click(screen.getByRole("button", { name: "Create event" }));
    expect(
      await screen.findByText("The Testing Lab operation failed."),
    ).toBeInTheDocument();
  });

  it("runs generic event dialogs and reports action exceptions", async () => {
    const user = userEvent.setup();
    mocks.createSlot.mockRejectedValueOnce(new Error("Slot API unavailable"));
    render(<CreateTestingEventSlotDialog eventId="event-1" />);
    await user.click(screen.getByRole("button", { name: "Add slot" }));
    const dialog = screen.getByRole("dialog", { name: "Add testing slot" });
    fireEvent.submit(dialog.querySelector("form")!);

    expect(await screen.findByText("Slot API unavailable")).toBeInTheDocument();
    await user.click(within(dialog).getByRole("button", { name: "Cancel" }));
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("navigates after deleting a draft through a successful destructive dialog", async () => {
    const user = userEvent.setup();
    render(
      <TestingEventLifecycleActions
        event={{ id: "event-1", status: "Draft", configuration: undefined }}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Delete draft" }));
    const dialog = screen.getByRole("dialog", { name: "Delete this draft event?" });
    await user.click(within(dialog).getByRole("button", { name: "Delete draft" }));

    await waitFor(() => expect(mocks.deleteEvent).toHaveBeenCalledOnce());
    await waitFor(() =>
      expect(mocks.push).toHaveBeenCalledWith("/workspace/testing-lab/events"),
    );
  });

  it.each([
    ["Draft", "Open applications", "open-applications"],
    ["ApplicationsOpen", "Close applications", "close-applications"],
    ["ApplicationsClosed", "Schedule event", "schedule"],
    ["Scheduled", "Start event", "activate"],
    ["Active", "Complete event", "complete"],
  ] as const)(
    "runs the %s lifecycle transition",
    async (status, label, transition) => {
      const user = userEvent.setup();
      render(
        <TestingEventLifecycleActions
          event={{
            id: "event-1",
            status,
            configuration: {
              generalRules: "Rules",
              candidateInstructions: "Candidates",
              testerInstructions: "Testers",
              projectApplicationSchema: { title: "Apply", questions: [] },
              testerRegistrationSchema: { title: "Register", questions: [] },
            },
          }}
        />,
      );
      await user.click(screen.getByRole("button", { name: label }));

      await waitFor(() => expect(mocks.transitionEvent).toHaveBeenCalledOnce());
      const data = mocks.transitionEvent.mock.calls[0]?.[0] as FormData;
      expect(data.get("transition")).toBe(transition);
      expect(await screen.findByText("Event transitioned.")).toBeInTheDocument();
    },
  );

  it("reports lifecycle failures and omits actions for events without identity", async () => {
    const user = userEvent.setup();
    mocks.transitionEvent.mockRejectedValueOnce(new Error("Transition unavailable"));
    const { rerender } = render(
      <TestingEventLifecycleActions
        event={{
          id: "event-1",
          status: "Scheduled",
          configuration: undefined,
        }}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Start event" }));
    expect(await screen.findByText("Transition unavailable")).toBeInTheDocument();

    rerender(<TestingEventLifecycleActions event={{ status: "Draft" }} />);
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });
});
