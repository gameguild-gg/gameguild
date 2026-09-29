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
  approveApplication: vi.fn(),
  beginReview: vi.fn(),
  createEvent: vi.fn(),
  createSlot: vi.fn(),
  createSlots: vi.fn(),
  deleteEvent: vi.fn(),
  push: vi.fn(),
  refresh: vi.fn(),
  rejectApplication: vi.fn(),
  transitionEvent: vi.fn(),
  voteApplication: vi.fn(),
  waitlistApplication: vi.fn(),
}));

vi.mock("next/navigation", () => ({
  usePathname: () => "/workspace/learning",
  useRouter: () => ({ push: mocks.push, refresh: mocks.refresh }),
}));

vi.mock("@/lib/testing-lab/events-actions", () => ({
  addTestingEventCommitteeMember: vi.fn(),
  assignTestedProjectToRegistration: vi.fn(),
  approveTestingEventApplication: mocks.approveApplication,
  beginTestingEventApplicationReview: mocks.beginReview,
  configureTestingEventLearning: vi.fn(),
  createTestingEvent: mocks.createEvent,
  createTestingEventSlot: mocks.createSlot,
  createTestingEventSlots: mocks.createSlots,
  archiveTestingEvent: vi.fn(),
  deleteTestingEvent: mocks.deleteEvent,
  deleteTestingEventSlot: vi.fn(),
  rejectTestingEventApplication: mocks.rejectApplication,
  removeTestingEventCommitteeMember: vi.fn(),
  restoreTestingEvent: vi.fn(),
  transitionTestingEvent: mocks.transitionEvent,
  updateTestingEventAttendance: vi.fn(),
  updateTestingEvent: vi.fn(),
  updateTestingEventSlot: vi.fn(),
  voteOnTestingEventApplication: mocks.voteApplication,
  waitlistTestingEventApplication: mocks.waitlistApplication,
}));

import {
  apiDatetimeLocal,
  buildTestingTimeSlots,
  createTestingEventSchedule,
  CreateTestingEventDialog,
  CreateTestingEventSlotDialog,
  defaultTestingSessionEnd,
  EditTestingEventDialog,
  ManageTestingEventSlotDialog,
  preferredNewEventTimeZone,
  scheduleDate,
  testingEventRecurrenceStart,
  TestingEventApplications,
  TestingEventLifecycleActions,
  TestingTimeSlotPlanner,
  updateTestingEventSchedule,
  validateTestingEventSchedule,
  validateTestingSlotWindow,
} from "./testing-event-management";
import { wallClockToUtcIso } from "@/lib/date-time-zone";

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
    mocks.createSlots.mockResolvedValue({
      success: true,
      data: [{ id: "slot-created" }],
      message: "Time slots created.",
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
    mocks.approveApplication.mockResolvedValue({
      success: true,
      data: null,
      message: "Application approved.",
    });
    mocks.rejectApplication.mockResolvedValue({
      success: true,
      data: null,
      message: "Application rejected.",
    });
    mocks.voteApplication.mockResolvedValue({
      success: true,
      data: null,
      message: "Vote recorded.",
    });
    mocks.waitlistApplication.mockResolvedValue({
      success: true,
      data: null,
      message: "Application waitlisted.",
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
    expect(scheduleDate("2030-01-01T12:00")?.toISOString()).toBe(
      "2030-01-01T12:00:00.000Z",
    );
  });

  it("keeps the selected date and default schedule in the chosen time zone", () => {
    const timeZoneId = "America/Los_Angeles";
    const schedule = createTestingEventSchedule(
      new Date("2030-01-01T16:17:00.000Z"),
      new Date(2030, 0, 5),
      timeZoneId,
    );

    expect(schedule.startsAt).toBe("2030-01-05T10:00");
    expect(schedule.applicationsOpenAt).toBe("2030-01-01T09:00");
    expect(
      Date.parse(wallClockToUtcIso(schedule.applicationsCloseAt, timeZoneId)!),
    ).toBeGreaterThan(
      Date.parse(wallClockToUtcIso(schedule.applicationsOpenAt, timeZoneId)!),
    );
    expect(
      Date.parse(wallClockToUtcIso(schedule.startsAt, timeZoneId)!),
    ).toBeGreaterThanOrEqual(
      Date.parse(wallClockToUtcIso(schedule.applicationsCloseAt, timeZoneId)!),
    );
    expect(
      Date.parse(wallClockToUtcIso(schedule.endsAt, timeZoneId)!) -
        Date.parse(wallClockToUtcIso(schedule.startsAt, timeZoneId)!),
    ).toBe(2 * 60 * 60 * 1000);
  });

  it("does not silently change other schedule values when one date moves", () => {
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
    expect(openMoved).toEqual({
      ...original,
      applicationsOpenAt: "2030-01-02T09:00",
    });
    expect(validateTestingEventSchedule(openMoved, "UTC")).toBe(
      "Applications must close after they open.",
    );

    const startMoved = updateTestingEventSchedule(
      original,
      "startsAt",
      "2030-01-05T12:00",
    );
    expect(startMoved.endsAt).toBe(original.endsAt);

    const validEnd = updateTestingEventSchedule(
      original,
      "endsAt",
      "2030-01-01T14:00",
    );
    expect(validEnd.endsAt).toBe("2030-01-01T14:00");
  });

  it("validates schedule order and rejects nonexistent timezone wall times", () => {
    const validSchedule = {
      applicationsOpenAt: "2026-03-07T10:00",
      applicationsCloseAt: "2026-03-08T01:30",
      startsAt: "2026-03-08T03:30",
      endsAt: "2026-03-08T04:30",
    };

    expect(validateTestingEventSchedule(validSchedule, "America/New_York")).toBe(
      null,
    );
    expect(
      validateTestingEventSchedule(
        { ...validSchedule, startsAt: "2026-03-08T02:30" },
        "America/New_York",
      ),
    ).toContain("does not exist in America/New_York");
    expect(
      validateTestingEventSchedule(
        { ...validSchedule, endsAt: "2026-03-08T03:00" },
        "America/New_York",
      ),
    ).toBe("The playtest end must be later than its start.");
  });

  it("uses explicit non-UTC event time zones", () => {
    expect(preferredNewEventTimeZone("Europe/Paris")).toBe("Europe/Paris");
    expect(testingEventRecurrenceStart("invalid")).toEqual({
      day: "Monday",
      dayOfMonth: 1,
    });
    expect(testingEventRecurrenceStart("2030-08-19T10:00")).toEqual({
      day: "Monday",
      dayOfMonth: 19,
    });
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

  it("keeps the schedule compact and exposes labeled 24-hour times in the picker", async () => {
    const user = userEvent.setup();
    render(<CreateTestingEventDialog defaultTimeZone="America/Sao_Paulo" />);

    fireEvent.click(screen.getByRole("button", { name: "New event" }));
    const field = (name: string) =>
      document.querySelector<HTMLInputElement>(`input[name="${name}"]`)!;
    const applicationsOpenAt = field("applicationsOpenAt");
    const applicationsCloseAt = field("applicationsCloseAt");
    const startsAt = field("startsAt");
    const endsAt = field("endsAt");

    expect(document.querySelector('input[type="date"]')).not.toBeInTheDocument();
    expect(document.querySelector('input[type="time"]')).not.toBeInTheDocument();
    expect(applicationsOpenAt.value).not.toBe("");
    expect(applicationsCloseAt.value).not.toBe("");
    expect(startsAt.value).not.toBe("");
    expect(endsAt.value).not.toBe("");
    expect(applicationsOpenAt.value).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/);
    expect(applicationsCloseAt.value).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/);
    expect(startsAt.value).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/);
    expect(endsAt.value).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/);
    expect(screen.queryByLabelText("Session starts time")).not.toBeInTheDocument();

    const originalStart = startsAt.value;
    fireEvent.click(screen.getByRole("combobox", { name: "Time zone" }));
    fireEvent.change(screen.getByPlaceholderText("Search time zones…"), {
      target: { value: "America/New_York" },
    });
    fireEvent.click(screen.getByText("America/New_York"));
    expect(field("timeZoneId").value).toBe("America/New_York");
    expect(startsAt.value).toBe(originalStart);

    const originalEnd = endsAt.value;
    await user.click(screen.getByRole("button", { name: "Event schedule" }));
    expect(await screen.findByLabelText("Session starts time")).toHaveValue(startsAt.value.slice(11));
    expect(screen.getByLabelText("Session ends time")).toHaveValue(endsAt.value.slice(11));
    fireEvent.change(screen.getByLabelText("Session starts time"), {
      target: { value: "11:30" },
    });
    expect(startsAt.value).toBe(originalStart);
    await user.click(screen.getByRole("button", { name: "Apply event schedule" }));
    await waitFor(() => expect(screen.queryByLabelText("Session starts time")).not.toBeInTheDocument());
    expect(startsAt.value.endsWith("T11:30")).toBe(true);
    expect(endsAt.value).toBe(originalEnd);
    expect(screen.getByText("All times use America/New_York and a 24-hour clock.")).toBeInTheDocument();

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

  it("submits the selected timezone even when a native form reset clears the hidden input", async () => {
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

      const hiddenTimeZone = document.querySelector<HTMLInputElement>(
        'input[name="timeZoneId"]',
      );
      expect(hiddenTimeZone).toHaveValue("America/Sao_Paulo");

      // Reproduces the browser behavior seen in production after form.reset().
      if (hiddenTimeZone) hiddenTimeZone.value = "";
      fireEvent.submit(hiddenTimeZone?.closest("form") as HTMLFormElement);

      await waitFor(() => expect(mocks.createEvent).toHaveBeenCalledTimes(1));
      const submitted = mocks.createEvent.mock.calls[0]?.[0] as FormData;
      expect(submitted.get("timeZoneId")).toBe("America/Sao_Paulo");
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

  it("keeps the compact schedule in one column without implicit grid tracks", () => {
    render(<CreateTestingEventDialog />);
    fireEvent.click(screen.getByRole("button", { name: "New event" }));

    const applications = screen.getByRole("group", { name: /^Application window,/ });
    const session = screen.getByRole("group", { name: /^Event schedule,/ });
    const timeline = applications.parentElement!.parentElement!;

    expect(session.parentElement!.parentElement).toBe(timeline);
    expect(timeline).not.toHaveClass("md:grid-cols-2");
    expect(timeline.querySelector("p")).not.toHaveClass("md:col-span-2");
    expect(session.parentElement).toHaveClass("sm:grid-cols-[7rem_minmax(0,1fr)]");
    expect(session.parentElement?.children).toHaveLength(2);
  });

  it("uses named and untitled event calendar templates", async () => {
    const user = userEvent.setup();
    const templates = [
      {
        id: "template-1",
        name: "Community playtests",
        currentRevision: { id: "revision-1" },
      },
      {
        id: "template-2",
        name: "   ",
        currentRevision: { id: "revision-2" },
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
    await user.click(screen.getByRole("combobox", { name: "Event calendar" }));
    await user.click(
      await screen.findByRole("option", { name: "Untitled calendar" }),
    );
    expect(
      document.querySelector<HTMLInputElement>(
        'input[name="templateRevisionId"]',
      ),
    ).toHaveValue("revision-2");
  });

  it("restores compact aligned decisions and calendar triggers without expanding schedule fields", async () => {
    const user = userEvent.setup();
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
    expect(eventName.closest("div")).toHaveClass("mb-1");

    const timeline = screen.getByRole("region", { name: "Schedule" });
    expect(
      within(timeline).getByRole("group", {
        name: "Application window, America/Sao_Paulo, 24-hour clock",
      }),
    ).toBeInTheDocument();
    expect(
      within(timeline).getByRole("group", {
        name: "Event schedule, America/Sao_Paulo, 24-hour clock",
      }),
    ).toBeInTheDocument();
    expect(within(timeline).queryByLabelText("Applications open date")).not.toBeInTheDocument();
    expect(within(timeline).queryByLabelText("Session starts time")).not.toBeInTheDocument();
    expect(within(timeline).getByRole("button", { name: "Application window" })).toBeInTheDocument();
    expect(within(timeline).getByRole("button", { name: "Event schedule" })).toBeInTheDocument();
    expect(within(timeline).getByText("Applications", { exact: true })).toBeInTheDocument();
    expect(within(timeline).getByText("Session", { exact: true })).toBeInTheDocument();
    expect(
      within(timeline).getByRole("combobox", { name: "Time zone" }),
    ).toHaveTextContent("Sao Paulo");

    expect(
      screen.getByRole("combobox", { name: "Repeats" }),
    ).toBeInTheDocument();
    await user.click(eventFormat);
    await user.click(await screen.findByRole("option", { name: "In person" }));
    expect(eventFormat).toHaveTextContent("In person");
    await user.click(eventFormat);
    await user.click(await screen.findByRole("option", { name: "Hybrid" }));
    expect(eventFormat).toHaveTextContent("Hybrid");
    await user.click(projectReview);
    await user.click(
      await screen.findByRole("option", { name: "Review committee votes" }),
    );
    expect(projectReview).toHaveTextContent("Review committee votes");
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
      await screen.findByRole("option", { name: "Weekly on Monday" }),
    ).toBeInTheDocument();
    await user.click(screen.getByRole("option", { name: "Custom…" }));

    expect(screen.getByLabelText("Repeat every")).toBeInTheDocument();
    expect(
      screen.getByRole("combobox", { name: "Repeat unit" }),
    ).toHaveTextContent("Week(s)");
    expect(screen.getByLabelText("Number of events")).toHaveValue(4);
  });

  it("updates each application boundary independently", async () => {
    const user = userEvent.setup();
    render(<CreateTestingEventDialog />);
    fireEvent.click(screen.getByRole("button", { name: "New event" }));

    const openAt = document.querySelector<HTMLInputElement>(
      'input[name="applicationsOpenAt"]',
    )!;
    const closeAt = document.querySelector<HTMLInputElement>(
      'input[name="applicationsCloseAt"]',
    )!;
    const unchangedClose = closeAt.value;
    await user.click(screen.getByRole("button", { name: "Application window" }));
    await screen.findByLabelText("Applications open time");
    fireEvent.change(screen.getByLabelText("Applications open time"), {
      target: { value: "13:15" },
    });
    await user.click(screen.getByRole("button", { name: "Apply application window" }));
    await waitFor(() => expect(screen.queryByLabelText("Applications open time")).not.toBeInTheDocument());
    expect(openAt.value).toMatch(/T13:15$/);
    expect(closeAt.value).toBe(unchangedClose);

    await user.click(screen.getByRole("button", { name: "Application window" }));
    await screen.findByLabelText("Applications close time");
    fireEvent.change(screen.getByLabelText("Applications close time"), {
      target: { value: "14:45" },
    });
    await user.click(screen.getByRole("button", { name: "Apply application window" }));
    expect(closeAt.value).toMatch(/T14:45$/);
  });

  it("supports every repeat preset, custom unit, weekday, and end mode", async () => {
    const user = userEvent.setup();
    render(<CreateTestingEventDialog initialDate={new Date(2030, 7, 19)} />);
    await user.click(screen.getByRole("button", { name: "New event" }));
    const repeats = screen.getByRole("combobox", { name: "Repeats" });

    for (const option of ["Daily", "Weekly on Monday", "Monthly on day 19"]) {
      await user.click(repeats);
      await user.click(await screen.findByRole("option", { name: option }));
      expect(
        document.querySelector<HTMLInputElement>(
          'input[name="recurrenceFrequency"]',
        )?.value,
      ).not.toBe("");
    }

    await user.click(repeats);
    await user.click(await screen.findByRole("option", { name: "Custom…" }));
    const repeatUnit = screen.getByRole("combobox", { name: "Repeat unit" });
    await user.click(repeatUnit);
    await user.click(await screen.findByRole("option", { name: "Day(s)" }));
    expect(repeatUnit).toHaveTextContent("Day(s)");
    await user.click(repeatUnit);
    await user.click(await screen.findByRole("option", { name: "Month(s)" }));
    expect(repeatUnit).toHaveTextContent("Month(s)");
    await user.click(repeatUnit);
    await user.click(await screen.findByRole("option", { name: "Week(s)" }));

    const monday = document.querySelector<HTMLInputElement>(
      'input[name="recurrenceDaysOfWeek"][value="Monday"]',
    )!;
    const tuesday = document.querySelector<HTMLInputElement>(
      'input[name="recurrenceDaysOfWeek"][value="Tuesday"]',
    )!;
    expect(monday).toBeChecked();
    await user.click(monday);
    expect(monday).not.toBeChecked();
    await user.click(tuesday);
    expect(tuesday).toBeChecked();

    const ends = screen.getByRole("combobox", { name: "Ends" });
    await user.click(ends);
    await user.click(await screen.findByRole("option", { name: "On a date" }));
    expect(screen.getByLabelText("End date")).toBeInTheDocument();
  });

  it("closes a pristine create dialog without a discard warning", async () => {
    const user = userEvent.setup();
    render(<CreateTestingEventDialog />);
    await user.click(screen.getByRole("button", { name: "New event" }));
    await user.click(screen.getByRole("button", { name: "Cancel" }));
    expect(screen.queryByText("New testing event")).not.toBeInTheDocument();
    expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
  });

  it("replaces the open action with a setup link while a draft is incomplete", () => {
    render(
      <TestingEventLifecycleActions
        event={{ id: "event-1", status: "Draft", configuration: undefined }}
      />,
    );

    expect(
      screen.getByRole("link", { name: "Finish event setup" }),
    ).toHaveAttribute(
      "href",
      "/workspace/testing-lab/events/event-1/overview#event-configuration-heading",
    );
    expect(
      screen.queryByRole("button", { name: "Publish and open sign-ups" }),
    ).not.toBeInTheDocument();
  });

  it("shows an empty application state and safe labels for incomplete records", () => {
    const { rerender } = render(
      <TestingEventApplications
        eventId="event-1"
        access={null}
        applications={[]}
        slots={[]}
      />,
    );
    expect(
      screen.getByText("No project applications yet."),
    ).toBeInTheDocument();

    rerender(
      <TestingEventApplications
        eventId="event-1"
        access={{ canManageApplications: true, canVote: true }}
        applications={[
          {
            id: undefined,
            projectId: undefined,
            submittedByUserId: undefined,
            status: undefined,
            decisionRationale: "Awaiting project metadata.",
          },
          {
            id: "application-unmapped",
            projectId: "project-missing",
            submittedByUserId: "user-missing",
            status: "Approved",
          },
        ]}
        slots={[]}
        projectLabels={{}}
        memberLabels={{}}
        readOnly
      />,
    );
    expect(screen.getAllByText("Project details unavailable")).toHaveLength(2);
    expect(screen.getAllByText(/Member details unavailable/)).toHaveLength(2);
    expect(screen.getByText("Awaiting project metadata.")).toBeInTheDocument();
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });

  it("keeps review failures visible and clears its pending marker", async () => {
    const user = userEvent.setup();
    mocks.beginReview.mockResolvedValueOnce({
      success: false,
      error: "Review cannot start yet.",
    });
    const { unmount } = render(
      <TestingEventApplications
        eventId="event-1"
        access={{ canManageApplications: true }}
        applications={[{ id: "application-1", status: "Pending" }]}
        slots={[]}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Review" }));
    expect(
      await screen.findByText("Review cannot start yet."),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Review" })).toBeEnabled();
    expect(mocks.refresh).not.toHaveBeenCalled();
    unmount();

    mocks.beginReview.mockRejectedValueOnce(
      new Error("Review API unavailable"),
    );
    render(
      <TestingEventApplications
        eventId="event-1"
        access={{ canManageApplications: true }}
        applications={[{ id: "application-1", status: "Pending" }]}
        slots={[]}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Review" }));
    expect(
      await screen.findByText("Review API unavailable"),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Review" })).toBeEnabled();
  });

  it.each([
    {
      trigger: "Approve",
      submit: "Approve project",
      status: "Approved",
      message: "Application approved.",
    },
    {
      trigger: "Reject",
      submit: "Reject project",
      status: "Rejected",
      message: "Application rejected.",
    },
    {
      trigger: "Waitlist",
      submit: "Add to waitlist",
      status: "Waitlisted",
      message: "Application waitlisted.",
    },
    {
      trigger: "Vote",
      submit: "Record vote",
      status: "UnderReview",
      message: "Vote recorded.",
    },
  ])(
    "keeps the $trigger confirmation visible after the dialog closes and the application refreshes",
    async ({ trigger, submit, status, message }) => {
      const user = userEvent.setup();
      const props = {
        eventId: "event-1",
        access: { canManageApplications: true, canVote: true },
        applications: [{ id: "application-1", status: "UnderReview" }],
        slots: [
          {
            id: "slot-1",
            startsAt: "2026-08-02T12:00:00Z",
            campusName: "Campus A",
          },
        ],
      };
      const { rerender } = render(<TestingEventApplications {...props} />);
      await user.click(
        screen.getByRole("button", { name: trigger, exact: true }),
      );
      if (trigger === "Approve") {
        await user.click(
          screen.getByRole("combobox", { name: "Testing slot" }),
        );
        await user.click(
          await screen.findByRole("option", { name: /Campus A/ }),
        );
      }
      if (trigger === "Reject") {
        await user.type(
          screen.getByLabelText("Rejection rationale"),
          "Not ready",
        );
      }
      if (trigger === "Vote") {
        await user.click(screen.getByRole("combobox"));
        await user.click(
          await screen.findByRole("option", { name: "Approve", exact: true }),
        );
      }
      await user.click(
        screen.getByRole("button", { name: submit, exact: true }),
      );
      await waitFor(() =>
        expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
      );
      rerender(
        <TestingEventApplications
          {...props}
          applications={[{ id: "application-1", status }]}
        />,
      );
      expect(screen.getByText(message)).toBeVisible();
      expect(
        screen.getByText(message).closest('[aria-live="polite"]'),
      ).not.toBeNull();
      expect(mocks.refresh).toHaveBeenCalledOnce();
    },
  );

  it("executes every application decision with meaningful slot fallbacks", async () => {
    const user = userEvent.setup();
    render(
      <TestingEventApplications
        eventId="event-1"
        access={{ canManageApplications: true, canVote: true }}
        applications={[
          {
            id: "application-1",
            projectId: "project-1",
            submittedByUserId: "user-1",
            status: "UnderReview",
          },
        ]}
        slots={[
          { id: undefined, startsAt: "2026-08-01T12:00:00Z" },
          {
            id: "slot-campus",
            startsAt: "2026-08-02T12:00:00Z",
            campusName: "Campus A",
          },
          {
            id: "slot-online",
            startsAt: "2026-08-03T12:00:00Z",
            meetingUrl: "https://meet.example.test",
          },
          {
            id: "slot-mode",
            startsAt: "2026-08-04T12:00:00Z",
            mode: "Hybrid",
          },
        ]}
      />,
    );

    await user.click(screen.getByRole("button", { name: "Approve" }));
    await user.click(screen.getByRole("combobox", { name: "Testing slot" }));
    expect(await screen.findByText(/Campus A/)).toBeInTheDocument();
    expect(
      screen.getByText(/https:\/\/meet\.example\.test/),
    ).toBeInTheDocument();
    expect(screen.getByText(/Hybrid/)).toBeInTheDocument();
    const approveForm = screen.getByRole("dialog").querySelector("form")!;
    const selectedSlot = document.createElement("input");
    selectedSlot.name = "slotId";
    selectedSlot.value = "slot-campus";
    approveForm.appendChild(selectedSlot);
    fireEvent.submit(approveForm);
    await waitFor(() =>
      expect(mocks.approveApplication).toHaveBeenCalledOnce(),
    );
    await waitFor(() =>
      expect(
        screen.queryByRole("dialog", { name: "Approve project application" }),
      ).not.toBeInTheDocument(),
    );

    await user.click(screen.getByRole("button", { name: "Waitlist" }));
    await user.click(screen.getByRole("button", { name: "Add to waitlist" }));
    await waitFor(() =>
      expect(mocks.waitlistApplication).toHaveBeenCalledOnce(),
    );
    await waitFor(() =>
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
    );

    await user.click(screen.getByRole("button", { name: "Reject" }));
    await user.type(screen.getByLabelText("Rejection rationale"), "Not ready");
    await user.click(screen.getByRole("button", { name: "Reject project" }));
    await waitFor(() => expect(mocks.rejectApplication).toHaveBeenCalledOnce());
    await waitFor(() =>
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
    );

    await user.click(screen.getByRole("button", { name: "Vote" }));
    fireEvent.submit(screen.getByRole("dialog").querySelector("form")!);
    await waitFor(() => expect(mocks.voteApplication).toHaveBeenCalledOnce());
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
  it("opens and requests closure as a controlled dialog with the calendar day prefilled", async () => {
    const user = userEvent.setup();
    const onOpenChange = vi.fn();
    render(
      <CreateTestingEventDialog
        open
        showTrigger={false}
        initialDate={new Date(2030, 7, 19)}
        onOpenChange={onOpenChange}
      />,
    );

    expect(
      screen.queryByRole("button", { name: "New event" }),
    ).not.toBeInTheDocument();
    expect(
      document.querySelector<HTMLInputElement>('input[name="startsAt"]')?.value,
    ).toMatch(/^2030-08-19T/);
    await user.click(screen.getByRole("button", { name: "Cancel" }));
    expect(onOpenChange).toHaveBeenCalledWith(false);
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
          requiresFeedback: false,
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
    expect(
      document.querySelector<HTMLInputElement>(
        'input[name="requiresFeedback"]',
      ),
    ).not.toBeChecked();
  });

  it("defaults an edited event without timezone to UTC", async () => {
    const user = userEvent.setup();
    render(<EditTestingEventDialog event={{ id: "event-1", name: "Draft" }} />);
    await user.click(screen.getByRole("button", { name: "Edit" }));
    expect(
      document.querySelector<HTMLInputElement>('input[name="timeZoneId"]'),
    ).toHaveValue("UTC");
    expect(
      document.querySelector<HTMLInputElement>(
        'input[name="requiresFeedback"]',
      ),
    ).toBeChecked();
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

    fireEvent.click(screen.getByRole("button", { name: "Edit time slot" }));

    expect(
      document.querySelector<HTMLInputElement>('input[name="startsAt"]')?.value,
    ).toBe("2026-08-13T17:30");
    expect(
      document.querySelector<HTMLInputElement>('input[name="endsAt"]')?.value,
    ).toBe("2026-08-13T18:30");
    timezoneOffset.mockRestore();
  });

  it("preconfigures the timebox builder from the event schedule and timezone", async () => {
    const user = userEvent.setup();
    render(
      <CreateTestingEventSlotDialog
        event={{
          id: "event-1",
          mode: "Online",
          startsAt: "2026-08-13T17:00:00Z",
          endsAt: "2026-08-13T19:00:00Z",
          timeZoneId: "America/Sao_Paulo",
        }}
      />,
    );

    await user.click(screen.getByRole("button", { name: "Build time slots" }));

    expect(
      screen.getByRole("dialog", { name: "Build testing time slots" }),
    ).toBeInTheDocument();
    expect(
      document.querySelector<HTMLInputElement>('input[name="planStartsAt"]'),
    ).toHaveValue("2026-08-13T14:00");
    expect(
      document.querySelector<HTMLInputElement>('input[name="planEndsAt"]'),
    ).toHaveValue("2026-08-13T16:00");
    expect(
      document.querySelector<HTMLInputElement>('input[name="timeZoneId"]'),
    ).toHaveValue("America/Sao_Paulo");
    expect(screen.getByText("Event window").parentElement).toHaveTextContent(
      "America/Sao_Paulo",
    );
    expect(
      JSON.parse(
        document.querySelector<HTMLInputElement>('input[name="slotsJson"]')!
          .value,
      ),
    ).toHaveLength(2);
  });

  it("shows the schedule planner inline and creates the previewed slots", async () => {
    const user = userEvent.setup();
    render(
      <TestingTimeSlotPlanner
        event={{
          id: "event-inline",
          mode: "Online",
          startsAt: "2026-08-13T17:00:00Z",
          endsAt: "2026-08-13T19:00:00Z",
          timeZoneId: "America/Sao_Paulo",
        }}
      />,
    );

    expect(
      screen.getByRole("heading", { name: "Plan a testing session" }),
    ).toBeInTheDocument();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "2 slots ready" }),
    ).toBeInTheDocument();

    await user.type(
      screen.getByRole("textbox", { name: "Meeting URL" }),
      "https://meet.example/session",
    );
    await user.click(
      screen.getByRole("button", { name: "Create 2 time slots" }),
    );

    await waitFor(() => expect(mocks.createSlots).toHaveBeenCalledOnce());
    expect(mocks.refresh).toHaveBeenCalledOnce();
  });

  it("splits a session into full timeboxes and leaves partial time unused", () => {
    expect(
      buildTestingTimeSlots("2026-09-03T14:00", "2026-09-03T18:00", 45, 15),
    ).toEqual([
      { startsAt: "2026-09-03T14:00", endsAt: "2026-09-03T14:45" },
      { startsAt: "2026-09-03T15:00", endsAt: "2026-09-03T15:45" },
      { startsAt: "2026-09-03T16:00", endsAt: "2026-09-03T16:45" },
      { startsAt: "2026-09-03T17:00", endsAt: "2026-09-03T17:45" },
    ]);
    expect(
      buildTestingTimeSlots("2026-09-03T14:00", "2026-09-03T14:30", 45, 15),
    ).toEqual([]);
  });

  it("blocks a session window outside the event instead of offering invalid slots", async () => {
    render(<TestingTimeSlotPlanner event={{ id: "bounded", startsAt: "2026-10-06T17:00:00Z", endsAt: "2026-10-06T19:00:00Z", timeZoneId: "America/Sao_Paulo" }} />);
    fireEvent.change(screen.getByLabelText("Ends time"), { target: { value: "17:00" } });
    expect(screen.getByRole("alert")).toHaveTextContent("Keep the session within the event");
    expect(screen.getByRole("button", { name: "Create time slots" })).toBeDisabled();
    fireEvent.submit(screen.getByRole("button", { name: "Create time slots" }).closest("form")!);
    expect(mocks.createSlots).not.toHaveBeenCalled();
    fireEvent.change(screen.getByLabelText("Ends time"), { target: { value: "16:00" } });
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Create 2 time slots" })).toBeEnabled();
  });

  it("validates full session dates against event bounds and DST gaps", () => {
    expect(validateTestingSlotWindow("2026-10-06T14:00", "2026-10-06T16:00", "2026-10-06T14:00", "2026-10-06T16:00", "America/Sao_Paulo")).toBeNull();
    expect(validateTestingSlotWindow("2026-10-05T14:00", "2026-10-06T16:00", "2026-10-06T14:00", "2026-10-06T16:00", "America/Sao_Paulo")).toMatch(/within the event/);
    expect(validateTestingSlotWindow("2026-10-06T14:00", "2026-10-06T13:00", "2026-10-06T14:00", "2026-10-06T16:00", "America/Sao_Paulo")).toMatch(/end after/);
    expect(validateTestingSlotWindow("2026-03-08T02:30", "2026-03-08T04:00", "2026-03-08T01:00", "2026-03-08T05:00", "America/New_York")).toMatch(/valid start and end/);
  });

  it("shows saved schedule state rather than offering to create zero slots", () => {
    render(<TestingTimeSlotPlanner
      event={{ id: "saved", startsAt: "2026-10-06T17:00:00Z", endsAt: "2026-10-06T17:45:00Z", timeZoneId: "America/Sao_Paulo" }}
      existingSlots={[{ id: "saved-slot", startsAt: "2026-10-06T17:00:00Z", endsAt: "2026-10-06T17:45:00Z" }]}
    />);
    expect(screen.getByRole("heading", { name: "Schedule saved" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Slots already saved" })).toBeDisabled();
    expect(screen.queryByRole("button", { name: "Create 0 time slots" })).not.toBeInTheDocument();
  });

  it("starts multi-day events with one practical four-hour session window", () => {
    expect(
      defaultTestingSessionEnd("2026-09-18T18:00", "2026-09-20T20:00"),
    ).toBe("2026-09-18T22:00");
    expect(
      defaultTestingSessionEnd("2026-09-18T18:00", "2026-09-18T20:00"),
    ).toBe("2026-09-18T20:00");
  });

  it("omits malformed slots and safely defaults optional slot fields", async () => {
    const user = userEvent.setup();
    const { rerender } = render(
      <ManageTestingEventSlotDialog eventId="event-1" slot={{}} />,
    );
    expect(
      screen.queryByRole("button", { name: "Edit time slot" }),
    ).not.toBeInTheDocument();

    rerender(
      <ManageTestingEventSlotDialog
        eventId="event-1"
        slot={{ id: "slot-empty", eventId: "event-1" }}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Edit time slot" }));
    expect(
      document.querySelector<HTMLInputElement>('input[name="locationId"]'),
    ).toHaveValue("");
    expect(
      document.querySelector<HTMLInputElement>('input[name="campusName"]'),
    ).not.toBeInTheDocument();
    expect(
      document.querySelector<HTMLInputElement>('input[name="roomName"]'),
    ).not.toBeInTheDocument();
    expect(
      document.querySelector<HTMLInputElement>('input[name="meetingUrl"]'),
    ).toHaveValue("");
    expect(
      document.querySelector<HTMLInputElement>('input[name="maxTesters"]'),
    ).toHaveValue(null);
    expect(
      document.querySelector<HTMLInputElement>('input[name="maxProjects"]'),
    ).toHaveValue(null);
  });

  it("creates a multi-day event through the calendar without changing the application window", async () => {
    const user = userEvent.setup();
    render(<CreateTestingEventDialog initialDate={new Date(2030, 0, 5)} defaultTimeZone="America/Sao_Paulo" />);
    await user.click(screen.getByRole("button", { name: "New event" }));
    await user.type(screen.getByRole("textbox", { name: "Event name" }), "Weekend playtest");
    const form = screen.getByRole("button", { name: "Create event" }).closest("form")!;
    const originalApplications = new FormData(form);

    await user.click(screen.getByRole("button", { name: "Event schedule" }));
    await user.click(await screen.findByRole("button", { name: "Thursday, January 10th, 2030", exact: true }));
    await user.click(screen.getByRole("button", { name: "Sunday, January 13th, 2030", exact: true }));
    await user.clear(screen.getByLabelText("Session starts time"));
    await user.type(screen.getByLabelText("Session starts time"), "18:00");
    await user.clear(screen.getByLabelText("Session ends time"));
    await user.type(screen.getByLabelText("Session ends time"), "21:00");
    await user.click(screen.getByRole("button", { name: "Apply event schedule" }));

    expect(screen.getByRole("button", { name: "Event schedule" }))
      .toHaveTextContent("10/01/2030 · 18:00 → 13/01/2030 · 21:00");
    await user.click(screen.getByRole("button", { name: "Create event" }));
    await waitFor(() => expect(mocks.createEvent).toHaveBeenCalledOnce());
    const submitted = mocks.createEvent.mock.calls[0]![0] as FormData;
    expect(submitted.get("startsAt")).toBe("2030-01-10T18:00");
    expect(submitted.get("endsAt")).toBe("2030-01-13T21:00");
    expect(submitted.get("timeZoneId")).toBe("America/Sao_Paulo");
    expect(submitted.get("applicationsOpenAt")).toBe(originalApplications.get("applicationsOpenAt"));
    expect(submitted.get("applicationsCloseAt")).toBe(originalApplications.get("applicationsCloseAt"));
    await waitFor(() => expect(mocks.push).toHaveBeenCalledWith("/workspace/testing-lab/events/event-created/overview"));
  });

  it("submits a new event, closes the dialog, and refreshes the route", async () => {
    const user = userEvent.setup();
    render(<CreateTestingEventDialog />);
    await user.click(screen.getByRole("button", { name: "New event" }));
    await user.type(
      screen.getByRole("textbox", { name: "Event name" }),
      "Ship night",
    );
    await user.click(screen.getByRole("button", { name: "Create event" }));

    await waitFor(() => expect(mocks.createEvent).toHaveBeenCalledOnce());
    await waitFor(() => expect(mocks.refresh).toHaveBeenCalledOnce());
    expect(screen.queryByText("New testing event")).not.toBeInTheDocument();
  });

  it("shows only relevant slot delivery fields and resets cancelled format changes", async () => {
    const user = userEvent.setup();
    render(<ManageTestingEventSlotDialog eventId="event-1" timeZoneId="America/Sao_Paulo" slot={{
      id: "delivery-slot", mode: "Online", locationId: "stored-location",
      startsAt: "2026-10-09T02:30:00Z", endsAt: "2026-10-09T03:15:00Z",
      meetingUrl: "https://meet.example/session", maxTesters: 8, maxProjects: 3,
    }} />);
    await user.click(screen.getByRole("button", { name: "Edit time slot" }));
    expect(screen.queryByLabelText("Saved location id")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Campus")).not.toBeInTheDocument();
    expect(screen.getByLabelText("Meeting URL")).toBeRequired();
    expect(screen.getByLabelText("Slot starts date")).toHaveValue("2026-10-08");
    expect(screen.getByLabelText("Slot ends date")).toHaveValue("2026-10-09");
    expect(screen.getByLabelText("Project capacity")).toHaveValue(3);
    await user.click(screen.getByRole("combobox", { name: "Format" }));
    await user.click(await screen.findByRole("option", { name: "In person" }));
    expect(screen.getByLabelText("Campus")).toBeRequired();
    expect(screen.getByLabelText("Room")).toBeRequired();
    expect(screen.queryByLabelText("Meeting URL")).not.toBeInTheDocument();
    expect(document.querySelector('input[name="locationId"]')).toHaveValue("stored-location");
    await user.click(screen.getByRole("button", { name: "Cancel", exact: true }));
    await user.click(screen.getByRole("button", { name: "Edit time slot" }));
    expect(screen.getByRole("combobox", { name: "Format" })).toHaveTextContent("Online");
    expect(screen.queryByLabelText("Campus")).not.toBeInTheDocument();
  });

  it("keeps create errors visible for returned and rejected failures", async () => {
    const user = userEvent.setup();
    mocks.createEvent.mockResolvedValueOnce({
      success: false,
      error: "Event name is already in use.",
    });
    const { unmount } = render(<CreateTestingEventDialog />);
    await user.click(screen.getByRole("button", { name: "New event" }));
    await user.type(
      screen.getByRole("textbox", { name: "Event name" }),
      "Duplicate",
    );
    await user.click(screen.getByRole("button", { name: "Create event" }));
    expect(
      await screen.findByText("Event name is already in use."),
    ).toBeInTheDocument();
    unmount();

    mocks.createEvent.mockRejectedValueOnce("offline");
    render(<CreateTestingEventDialog />);
    await user.click(screen.getByRole("button", { name: "New event" }));
    await user.type(
      screen.getByRole("textbox", { name: "Event name" }),
      "Offline event",
    );
    await user.click(screen.getByRole("button", { name: "Create event" }));
    expect(
      await screen.findByText("The Testing Lab operation failed."),
    ).toBeInTheDocument();
  });

  it("runs generic event dialogs and reports action exceptions", async () => {
    const user = userEvent.setup();
    mocks.createSlots.mockRejectedValueOnce(new Error("Slot API unavailable"));
    const event = {
      id: "event-1",
      startsAt: "2026-08-13T17:00:00Z",
      endsAt: "2026-08-13T19:00:00Z",
      mode: "Online" as const,
    };
    const { unmount } = render(<CreateTestingEventSlotDialog event={event} />);
    await user.click(screen.getByRole("button", { name: "Build time slots" }));
    const dialog = screen.getByRole("dialog", {
      name: "Build testing time slots",
    });
    fireEvent.submit(dialog.querySelector("form")!);

    expect(await screen.findByText("Slot API unavailable")).toBeInTheDocument();
    await user.click(within(dialog).getByRole("button", { name: "Cancel" }));
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    unmount();

    mocks.createSlots.mockResolvedValueOnce({
      success: false,
      error: "Slot rejected",
    });
    render(
      <CreateTestingEventSlotDialog event={{ ...event, id: "event-2" }} />,
    );
    await user.click(screen.getByRole("button", { name: "Build time slots" }));
    fireEvent.submit(screen.getByRole("dialog").querySelector("form")!);
    expect(await screen.findByText("Slot rejected")).toBeInTheDocument();
  });

  it("does not close quick create while its request is pending", async () => {
    const user = userEvent.setup();
    let finish!: (value: {
      success: true;
      data: { id: string };
      message: string;
    }) => void;
    mocks.createEvent.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          finish = resolve;
        }),
    );
    render(<CreateTestingEventDialog />);
    await user.click(screen.getByRole("button", { name: "New event" }));
    await user.type(
      screen.getByRole("textbox", { name: "Event name" }),
      "Pending event",
    );
    await user.click(screen.getByRole("button", { name: "Create event" }));
    await waitFor(() =>
      expect(
        screen.getByRole("button", { name: "Creating event..." }),
      ).toBeDisabled(),
    );
    await user.click(screen.getByRole("button", { name: "Close" }));
    expect(
      screen.getByRole("dialog", { name: "New testing event" }),
    ).toBeInTheDocument();
    finish({
      success: true,
      data: { id: "event-created" },
      message: "Created",
    });
    await waitFor(() => expect(mocks.createEvent).toHaveBeenCalledOnce());
  });

  it("navigates after deleting a draft through a successful destructive dialog", async () => {
    const user = userEvent.setup();
    render(
      <TestingEventLifecycleActions
        event={{ id: "event-1", status: "Draft", configuration: undefined }}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Delete draft" }));
    const dialog = screen.getByRole("dialog", {
      name: "Delete this draft event?",
    });
    await user.click(
      within(dialog).getByRole("button", { name: "Delete draft" }),
    );

    await waitFor(() => expect(mocks.deleteEvent).toHaveBeenCalledOnce());
    await waitFor(() =>
      expect(mocks.push).toHaveBeenCalledWith("/workspace/testing-lab/events"),
    );
  });

  it("keeps cancellation confirmation visible after the event becomes read-only", async () => {
    const user = userEvent.setup();
    mocks.transitionEvent.mockResolvedValueOnce({
      success: true,
      data: null,
      message: "Event status updated.",
    });
    const { rerender } = render(
      <TestingEventLifecycleActions event={{ id: "event-1", status: "Active" }} />,
    );

    await user.click(screen.getByRole("button", { name: "Cancel event" }));
    const dialog = screen.getByRole("dialog", { name: "Cancel this testing event?" });
    await user.type(within(dialog).getByLabelText("Cancellation reason"), "Session finished early.");
    await user.click(within(dialog).getByRole("button", { name: "Cancel event" }));

    await waitFor(() => expect(mocks.refresh).toHaveBeenCalledOnce());
    rerender(<TestingEventLifecycleActions event={{ id: "event-1", status: "Cancelled" }} />);

    expect(screen.queryByRole("button", { name: "Cancel event" })).not.toBeInTheDocument();
    expect(screen.getByText("Event status updated.")).toBeInTheDocument();
  });

  it.each([
    ["Draft", "Publish and open sign-ups", "open-applications"],
    ["ApplicationsOpen", "Close game submissions", "close-applications"],
    ["ApplicationsClosed", "Publish session schedule", "schedule"],
    ["Scheduled", "Start playtest", "activate"],
    ["Active", "Complete playtest", "complete"],
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
      expect(
        await screen.findByText("Event transitioned."),
      ).toBeInTheDocument();
    },
  );

  it("refreshes lifecycle state only after the transition succeeds", async () => {
    const user = userEvent.setup();
    let resolveTransition: (
      result: { success: true; data: { id: string }; message: string },
    ) => void = () => {};
    mocks.transitionEvent.mockReturnValueOnce(
      new Promise<{ success: true; data: { id: string }; message: string }>(
        (resolve) => {
          resolveTransition = resolve;
        },
      ),
    );

    render(
      <TestingEventLifecycleActions
        event={{
          id: "event-1",
          status: "Draft",
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

    await user.click(screen.getByRole("button", { name: "Publish and open sign-ups" }));
    await waitFor(() => expect(mocks.transitionEvent).toHaveBeenCalledOnce());
    expect(mocks.refresh).not.toHaveBeenCalled();

    resolveTransition({
      success: true,
      data: { id: "event-1" },
      message: "Event transitioned.",
    });

    await waitFor(() => expect(mocks.refresh).toHaveBeenCalledOnce());
  });

  it("reports lifecycle failures and omits actions for events without identity", async () => {
    const user = userEvent.setup();
    mocks.transitionEvent.mockRejectedValueOnce(
      new Error("Transition unavailable"),
    );
    const { rerender } = render(
      <TestingEventLifecycleActions
        event={{
          id: "event-1",
          status: "Scheduled",
          configuration: undefined,
        }}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Start playtest" }));
    expect(
      await screen.findByText("Transition unavailable"),
    ).toBeInTheDocument();

    rerender(<TestingEventLifecycleActions event={{ status: "Draft" }} />);
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });

  it("treats an unidentified lifecycle status as a draft", () => {
    render(
      <TestingEventLifecycleActions
        event={{
          id: "event-1",
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
    expect(
      screen.getByRole("button", { name: "Publish and open sign-ups" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Cancel event" }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Archive event" }),
    ).not.toBeInTheDocument();
  });
});
