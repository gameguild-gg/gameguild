import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type {
  TestingLabTestingEventProjection,
  TestingLabTestingEventTemplateProjection,
} from "@game-guild/client";
import { forwardRef, type ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/i18n/navigation", () => ({
  Link: forwardRef<HTMLAnchorElement, { children: ReactNode; href: string }>(
    function MockLink({ children, href, ...rest }, ref) {
      return (
        <a ref={ref} href={href} {...rest}>
          {children}
        </a>
      );
    },
  ),
}));

vi.mock("./testing-event-management", () => ({
  CreateTestingEventDialog: ({
    open,
    initialDate,
  }: {
    open?: boolean;
    initialDate?: Date;
  }) =>
    open ? (
      <div role="dialog">
        Creating event for{" "}
        {initialDate?.toISOString().slice(0, 10) ?? "no date"}
      </div>
    ) : null,
}));

import {
  getServerMobileCalendarSnapshot,
  TestingLabCalendar,
} from "./testing-lab-calendar";

const events = [
  {
    id: "event-1",
    name: "Campus playtest",
    description: "Hands-on lab for the new combat build.",
    status: "Scheduled",
    mode: "InPerson",
    startsAt: "2030-08-10T18:00:00.000Z",
    endsAt: "2030-08-10T20:00:00.000Z",
  },
  {
    id: "event-2",
    name: "Remote build review",
    description: "Online review for approved community projects.",
    status: "Active",
    mode: "Online",
    startsAt: "2030-08-12T18:00:00.000Z",
    endsAt: "2030-08-12T20:00:00.000Z",
  },
] as TestingLabTestingEventProjection[];

const eventAnalytics = [
  { eventId: "event-1", registeredTesters: 3, capacity: 10, fillRate: 30 },
  { eventId: "event-2", registeredTesters: 8, capacity: 8, fillRate: 100 },
];

const templates = [
  {
    id: "template-1",
    name: "Prototype reviews",
    currentRevision: { id: "revision-1" },
  },
] as TestingLabTestingEventTemplateProjection[];

async function selectView(
  user: ReturnType<typeof userEvent.setup>,
  name: string,
) {
  await user.click(screen.getByRole("combobox", { name: "Calendar view" }));
  await user.click(await screen.findByRole("option", { name }));
}

describe("TestingLabCalendar", () => {
  it("opens as a month calendar and identifies event mode and capacity", () => {
    render(
      <TestingLabCalendar
        events={events}
        eventAnalytics={eventAnalytics}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    expect(
      screen.getByRole("combobox", { name: "Calendar view" }),
    ).toHaveTextContent("Month");
    expect(
      screen.getByRole("combobox", { name: "Calendar view" }),
    ).toHaveAttribute("data-slot", "select-trigger");
    expect(
      screen.getByRole("searchbox", { name: "Search events" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("combobox", { name: "Filter events" }),
    ).toHaveTextContent("All statuses");
    expect(
      screen.getByRole("combobox", { name: "Filter events" }),
    ).toHaveAttribute("data-slot", "select-trigger");
    expect(
      screen.queryByRole("button", { name: "New event" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Hide weekends" }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole("toolbar", { name: "Testing Lab calendar controls" }),
    ).toBeInTheDocument();
    expect(screen.getByText("Campus playtest")).toBeInTheDocument();
    expect(screen.getByLabelText("In-person event")).toBeInTheDocument();
    expect(screen.getByLabelText("Online event")).toBeInTheDocument();
    expect(screen.getByText("3/10")).toBeInTheDocument();
    expect(screen.getByText("8/8")).toBeInTheDocument();
  });

  it("fills its work area without an outer card frame", () => {
    render(
      <TestingLabCalendar
        events={events}
        eventAnalytics={eventAnalytics}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    const calendar = screen.getByRole("region", {
      name: "Testing Lab calendar",
    });

    expect(calendar).toHaveClass("h-full");
    expect(calendar).not.toHaveClass("border", "rounded-lg");
  });

  it("shows ISO week numbers beside each row in the month view", () => {
    render(
      <TestingLabCalendar
        events={events}
        eventAnalytics={eventAnalytics}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    expect(screen.getByLabelText("Week numbers")).toHaveTextContent("Wk");
    const monthGrid = screen.getByRole("region", {
      name: "Month Testing Lab calendar",
    });
    expect(within(monthGrid).getByLabelText("Week 31")).toHaveTextContent(
      "31",
    );
    expect(within(monthGrid).getAllByLabelText(/^Week \d+$/)).toHaveLength(5);
  });

  it("keeps an empty month grid unobstructed", () => {
    render(
      <TestingLabCalendar events={[]} initialDate={new Date(2030, 7, 10)} />,
    );

    expect(
      screen.queryByText("No events in this period"),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole("button", {
        name: /Create event on August 19, 2030/,
      }),
    ).toBeInTheDocument();
  });

  it("offers the Google Calendar view set and can switch to the schedule", async () => {
    const user = userEvent.setup();
    render(
      <TestingLabCalendar
        events={events}
        eventAnalytics={eventAnalytics}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    const view = screen.getByRole("combobox", { name: "Calendar view" });
    await user.click(view);
    expect(
      (await screen.findAllByRole("option")).map((option) => option.textContent),
    ).toEqual(["Day", "Week", "Month", "Year", "Schedule", "3 days"]);
    await user.click(screen.getByRole("option", { name: "Schedule" }));

    expect(
      screen.getByRole("heading", { name: "Schedule" }),
    ).toBeInTheDocument();
    expect(
      within(
        screen.getByRole("region", { name: "Testing Lab schedule" }),
      ).getByText("Scheduled"),
    ).toBeInTheDocument();
  });

  it("opens event creation for the selected calendar day", () => {
    render(
      <TestingLabCalendar
        events={events}
        eventAnalytics={eventAnalytics}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    fireEvent.doubleClick(
      screen.getByRole("button", {
        name: /Create event on August 19, 2030/,
      }),
    );

    expect(screen.getByRole("dialog")).toHaveTextContent(
      "Creating event for 2030-08-19",
    );
  });

  it("shows operational event details on hover without opening the event page", async () => {
    const user = userEvent.setup();
    render(
      <TestingLabCalendar
        events={events}
        eventAnalytics={eventAnalytics}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    await user.hover(screen.getByRole("button", { name: /Campus playtest/i }));

    expect(
      await screen.findByText("Hands-on lab for the new combat build."),
    ).toBeInTheDocument();
    expect(screen.getByText("7 tester spots available")).toBeInTheDocument();
  });

  it("opens a compact event summary before navigating to its workspace", async () => {
    const user = userEvent.setup();
    render(
      <TestingLabCalendar
        events={events}
        eventAnalytics={eventAnalytics}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    await user.click(screen.getByRole("button", { name: /Campus playtest/i }));

    const dialog = screen.getByRole("dialog");
    expect(within(dialog).getByText("Campus playtest")).toBeInTheDocument();
    expect(within(dialog).getByText("7 tester spots available")).toBeInTheDocument();
    expect(within(dialog).getByRole("link", { name: "Open event" })).toHaveAttribute(
      "href",
      "/workspace/testing-lab/events/event-1",
    );
  });

  it("groups events by calendar and lets managers hide a calendar", async () => {
    const user = userEvent.setup();
    const categorizedEvents = [
      {
        ...events[0],
        configuration: { sourceTemplateId: "template-1" },
      },
      events[1],
    ] as TestingLabTestingEventProjection[];

    render(
      <TestingLabCalendar
        events={categorizedEvents}
        templates={templates}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    const sidebar = screen.getByRole("complementary", {
      name: "Testing Lab planning",
    });
    const category = within(sidebar).getByRole("checkbox", {
      name: "Prototype reviews",
    });
    expect(category).toBeChecked();

    await user.click(category);

    expect(screen.queryByText("Campus playtest")).not.toBeInTheDocument();
    expect(screen.getByText("Remote build review")).toBeInTheDocument();
  });

  it("expands crowded days without navigating away from the calendar", async () => {
    const user = userEvent.setup();
    const crowdedEvents = Array.from({ length: 5 }, (_, index) => ({
      ...events[0],
      id: `event-${index + 1}`,
      name: `Playtest ${index + 1}`,
    })) as TestingLabTestingEventProjection[];

    render(
      <TestingLabCalendar
        events={crowdedEvents}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    expect(screen.queryByText("Playtest 5")).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "+2 more" }));
    expect(screen.getByText("Playtest 5")).toBeInTheDocument();
  });

  it("filters the visible calendar without changing the event directory", async () => {
    const user = userEvent.setup();
    render(
      <TestingLabCalendar
        events={events}
        eventAnalytics={eventAnalytics}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    await user.type(
      screen.getByRole("searchbox", { name: "Search events" }),
      "remote",
    );

    expect(screen.queryByText("Campus playtest")).not.toBeInTheDocument();
    expect(screen.getByText("Remote build review")).toBeInTheDocument();

    await user.clear(screen.getByRole("searchbox", { name: "Search events" }));
    await user.click(screen.getByRole("combobox", { name: "Filter events" }));
    await user.click(screen.getByRole("option", { name: "Scheduled" }));

    expect(screen.getByText("Campus playtest")).toBeInTheDocument();
    expect(screen.queryByText("Remote build review")).not.toBeInTheDocument();
  });

  it("adds a synchronized details panel with a useful period summary", () => {
    render(
      <TestingLabCalendar
        events={events}
        eventAnalytics={eventAnalytics}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    const sidebar = screen.getByRole("complementary", {
      name: "Testing Lab planning",
    });

    expect(within(sidebar).getByText("August 2030")).toBeInTheDocument();
    expect(within(sidebar).getByText("August summary")).toBeInTheDocument();
    expect(within(sidebar).getByText("2 events")).toBeInTheDocument();
    expect(within(sidebar).getByText("4h scheduled")).toBeInTheDocument();
    expect(
      within(sidebar).getByText("11 of 18 seats filled"),
    ).toBeInTheDocument();
    expect(within(sidebar).queryByText("Display")).not.toBeInTheDocument();
    expect(
      within(sidebar).queryByRole("button", {
        name: "Filter visible event formats",
      }),
    ).not.toBeInTheDocument();
  });

  it("can collapse the details panel", async () => {
    const user = userEvent.setup();
    render(
      <TestingLabCalendar
        events={events}
        eventAnalytics={eventAnalytics}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    await user.click(
      screen.getByRole("button", { name: "Hide details panel" }),
    );
    expect(
      screen.queryByRole("complementary", { name: "Testing Lab planning" }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Show details panel" }),
    ).toBeInTheDocument();
  });

  it("replaces empty metrics with a concise, actionable empty state", () => {
    render(
      <TestingLabCalendar events={[]} initialDate={new Date(2030, 7, 10)} />,
    );

    const sidebar = screen.getByRole("complementary", {
      name: "Testing Lab planning",
    });

    expect(
      within(sidebar).getByText("No scheduled activity"),
    ).toBeInTheDocument();
    expect(
      within(sidebar).getByText(
        "Choose another month or create an event to see testing time and capacity.",
      ),
    ).toBeInTheDocument();
    expect(within(sidebar).queryByRole("progressbar")).not.toBeInTheDocument();
    expect(within(sidebar).getByText("Event calendars")).toBeInTheDocument();
    expect(
      within(sidebar).getByRole("checkbox", { name: "Testing events" }),
    ).toBeChecked();
  });

  it("keeps the mini calendar synchronized with the main calendar", async () => {
    const user = userEvent.setup();
    render(
      <TestingLabCalendar
        events={events}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    const sidebar = screen.getByRole("complementary", {
      name: "Testing Lab planning",
    });
    const previousMonth = within(sidebar).getByRole("button", {
      name: "Go to the Previous Month",
    });
    const nextMonth = within(sidebar).getByRole("button", {
      name: "Go to the Next Month",
    });

    const miniCalendarMonths = previousMonth.parentElement?.parentElement;

    expect(previousMonth).toBeVisible();
    expect(nextMonth).toBeVisible();
    expect(miniCalendarMonths).toHaveClass("relative");

    await user.click(nextMonth);

    expect(
      screen.getByRole("heading", { name: "September 2030" }),
    ).toBeInTheDocument();
  });

  it("uses the schedule as the mobile default view", () => {
    vi.stubGlobal(
      "matchMedia",
      vi.fn(() => ({
        matches: true,
        media: "(max-width: 767px)",
        onchange: null,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        addListener: vi.fn(),
        removeListener: vi.fn(),
        dispatchEvent: vi.fn(),
      })),
    );

    render(
      <TestingLabCalendar
        events={events}
        eventAnalytics={eventAnalytics}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    expect(
      screen.getByRole("combobox", { name: "Calendar view" }),
    ).toHaveTextContent("Schedule");
    vi.unstubAllGlobals();
  });

  it("provides a stable non-mobile server snapshot", () => {
    expect(getServerMobileCalendarSnapshot()).toBe(false);
  });

  it("works without matchMedia and preserves injected toolbar actions", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("matchMedia", undefined);

    render(
      <TestingLabCalendar
        events={[]}
        initialDate={new Date(2030, 7, 10)}
        toolbarStart={<button type="button">Workspace action</button>}
        toolbarEnd={<button type="button">New event</button>}
      />,
    );

    expect(screen.getByText("Workspace action")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "New event" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Previous period" }));
    expect(screen.getByRole("heading", { name: "July 2030" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Next period" }));
    expect(screen.getByRole("heading", { name: "August 2030" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Today" }));

    vi.unstubAllGlobals();
  });

  it("opens creation from the keyboard but ignores unrelated keys", () => {
    render(
      <TestingLabCalendar events={[]} initialDate={new Date(2030, 7, 10)} />,
    );
    const day = screen.getByRole("button", {
      name: /Create event on August 19, 2030/,
    });

    fireEvent.keyDown(day, { key: "Escape" });
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    fireEvent.keyDown(day, { key: "Enter" });
    expect(screen.getByRole("dialog")).toHaveTextContent(
      "Creating event for 2030-08-19",
    );
  });

  it("shows the schedule empty state when no event has a valid date", async () => {
    const user = userEvent.setup();
    render(
      <TestingLabCalendar
        events={[
          { id: "missing-date", name: "Missing date" },
          { id: "invalid-date", name: "Invalid date", startsAt: "invalid" },
        ]}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    await selectView(user, "Schedule");
    expect(screen.getByText("No events in this period")).toBeInTheDocument();
  });

  it("renders the year view and summarizes crowded months", async () => {
    const user = userEvent.setup();
    const yearEvents = Array.from({ length: 5 }, (_, index) => ({
      ...events[0],
      id: `year-${index}`,
      name: `Year event ${index + 1}`,
      startsAt: `2030-08-${String(index + 10).padStart(2, "0")}T18:00:00.000Z`,
      endsAt: `2030-08-${String(index + 10).padStart(2, "0")}T20:00:00.000Z`,
    })) as TestingLabTestingEventProjection[];

    render(
      <TestingLabCalendar
        events={[
          ...yearEvents,
          { id: "year-invalid", name: "Invalid", startsAt: "invalid" },
        ]}
        initialDate={new Date(2030, 7, 10)}
      />,
    );
    await selectView(user, "Year");

    expect(
      screen.getByRole("region", { name: "Testing Lab year" }),
    ).toBeInTheDocument();
    expect(screen.getByText("+1 more events")).toBeInTheDocument();
    expect(screen.getAllByText("No events")).toHaveLength(11);
  });

  it("supports week, day, and three-day grid views", async () => {
    const user = userEvent.setup();
    render(
      <TestingLabCalendar
        events={events}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    await selectView(user, "Year");
    expect(screen.queryByText(/more events/)).not.toBeInTheDocument();

    await selectView(user, "Week");
    expect(
      screen.getByRole("region", { name: "Week Testing Lab calendar" }),
    ).toBeInTheDocument();
    expect(screen.queryByLabelText("Week numbers")).not.toBeInTheDocument();

    await selectView(user, "Day");
    expect(
      screen.getByRole("region", { name: "Day Testing Lab calendar" }),
    ).toBeInTheDocument();

    await selectView(user, "3 days");
    expect(
      screen.getByRole("region", { name: "3 days Testing Lab calendar" }),
    ).toBeInTheDocument();
  });

  it("handles status, capacity, and incomplete event metadata", async () => {
    const user = userEvent.setup();
    const edgeEvents = [
      {
        id: "untitled",
        name: undefined,
        description: undefined,
        status: "Cancelled",
        mode: "Hybrid",
        startsAt: "2030-08-15T18:00:00.000Z",
        endsAt: undefined,
      },
      {
        id: "unlimited",
        name: "Unlimited lab",
        status: "Completed",
        mode: "Online",
        startsAt: "2030-08-16T18:00:00.000Z",
        endsAt: "2030-08-16T20:00:00.000Z",
      },
      {
        id: "one-seat",
        name: "One seat left",
        status: "Scheduled",
        mode: "InPerson",
        startsAt: "2030-08-17T18:00:00.000Z",
        endsAt: "2030-08-17T20:00:00.000Z",
      },
      {
        id: "backwards",
        name: "Invalid duration",
        startsAt: "2030-08-18T20:00:00.000Z",
        endsAt: "2030-08-18T18:00:00.000Z",
      },
      {
        id: undefined,
        name: "Missing identity",
        startsAt: "2030-08-19T18:00:00.000Z",
      },
    ] as TestingLabTestingEventProjection[];

    render(
      <TestingLabCalendar
        events={edgeEvents}
        eventAnalytics={[
          {
            eventId: "unlimited",
            registeredTesters: 3,
            capacity: 0,
            fillRate: 0,
          },
          {
            eventId: "one-seat",
            registeredTesters: 1,
            capacity: 2,
            fillRate: 50,
          },
        ]}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    const cancelled = screen.getByRole("button", {
      name: /Untitled event/i,
    });
    expect(cancelled).toHaveClass("line-through");
    expect(screen.getByRole("button", { name: /Unlimited lab/i })).toHaveClass(
      "opacity-70",
    );
    expect(screen.getByLabelText("Hybrid event")).toBeInTheDocument();
    expect(screen.getAllByText("Capacity pending")).not.toHaveLength(0);
    expect(screen.getByText("3 registered")).toBeInTheDocument();
    expect(screen.getByText("1/2")).toBeInTheDocument();
    expect(screen.queryByText("Missing identity")).not.toBeInTheDocument();

    await user.hover(screen.getByRole("button", { name: /One seat left/i }));
    expect(await screen.findByText("1 tester spot available")).toBeInTheDocument();

    await user.click(cancelled);
    const dialog = screen.getByRole("dialog");
    expect(within(dialog).getByText("Untitled event")).toBeInTheDocument();
    expect(within(dialog).getByText("Capacity information is not available yet.")).toBeInTheDocument();
    expect(within(dialog).queryByText("undefined")).not.toBeInTheDocument();
    await user.click(within(dialog).getByRole("button", { name: "Close" }));
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("uses safe template calendar labels and toggles them independently", async () => {
    const user = userEvent.setup();
    render(
      <TestingLabCalendar
        events={[
          {
            ...events[0],
            configuration: { sourceTemplateId: "template-blank" },
          },
          {
            ...events[1],
            id: "orphan-event",
            configuration: { sourceTemplateId: "template-orphan" },
          },
        ]}
        templates={[
          { id: "template-blank", name: "   " },
          { id: undefined, name: "Ignored" },
        ]}
        initialDate={new Date(2030, 7, 10)}
      />,
    );

    const sidebar = screen.getByRole("complementary", {
      name: "Testing Lab planning",
    });
    expect(within(sidebar).getByText("Untitled calendar")).toBeInTheDocument();
    expect(within(sidebar).getByText("Event template")).toBeInTheDocument();

    await user.click(
      within(sidebar).getByRole("checkbox", { name: "Event template" }),
    );
    expect(screen.queryByText("Remote build review")).not.toBeInTheDocument();
  });

  it("keeps the selected mini-calendar date as the active anchor", async () => {
    const user = userEvent.setup();
    render(
      <TestingLabCalendar events={[]} initialDate={new Date(2030, 7, 10)} />,
    );
    const sidebar = screen.getByRole("complementary", {
      name: "Testing Lab planning",
    });
    const selectedDay = within(sidebar).getByRole("button", {
      name: /August 10th, 2030/,
    });

    await user.click(selectedDay);
    expect(screen.getByRole("heading", { name: "August 2030" })).toBeInTheDocument();
  });
});
