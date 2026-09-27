import { render, screen, within } from "@testing-library/react";
import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import type { TestingEventViewModel } from "./testing-events-presentation";

vi.mock("@/i18n/navigation", () => ({
  Link: ({ children, href, ...props }: { children: ReactNode; href: string }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

import {
  TestingEventCard,
  TestingEventRow,
  TestingEventsTable,
} from "./testing-event-views";

function event(
  overrides: Partial<TestingEventViewModel> = {},
): TestingEventViewModel {
  return {
    id: "event-1",
    title: "Campus playtest",
    description: "Test the latest build.",
    mode: "Hybrid",
    status: "open",
    statusLabel: "Tester sign-up open",
    startsAt: "2026-09-15T18:30:00.000Z",
    endsAt: "2026-09-15T20:00:00.000Z",
    location: "Main campus - Lab 2",
    testerCount: 3,
    testerLimit: 8,
    projectCount: 1,
    projectLimit: null,
    availableTesterCount: 1,
    testerRegistrationOpen: true,
    gameSubmissionsOpen: false,
    gameImageUrl: null,
    scheduleCount: 1,
    timeZoneId: "UTC",
    dateLocale: "en-US",
    hour12: true,
    ...overrides,
  };
}

describe("Testing event directory views", () => {
  it("renders a single plain capacity line with event context", () => {
    render(<TestingEventCard session={event()} projectId="project / 1" />);

    expect(screen.getByText("Sep 15, 2026")).toBeInTheDocument();
    expect(screen.getByText("6:30 PM UTC")).toBeInTheDocument();
    expect(screen.queryByText("3/8 testers")).not.toBeInTheDocument();
    expect(screen.getByText("1 project")).toBeInTheDocument();
    expect(screen.getByText("1 spot left")).toBeInTheDocument();
    expect(screen.queryByText(/tester seat left/i)).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "View event: Campus playtest" })).toHaveAttribute(
      "href",
      "/testing-lab/events/event-1?projectId=project%20%2F%201",
    );
  });

  it("hides available capacity after tester sign-up closes", () => {
    const { rerender } = render(
      <TestingEventCard session={event({ availableTesterCount: 2 })} />,
    );
    expect(screen.getByText("2 spots left")).toBeInTheDocument();

    rerender(
      <TestingEventCard
        session={event({
          status: "completed",
          statusLabel: "Playtest ended",
          testerRegistrationOpen: false,
          availableTesterCount: 1,
        })}
      />,
    );
    expect(screen.queryByText(/spots? left/)).not.toBeInTheDocument();
    expect(screen.getByText("3/8 testers")).toBeInTheDocument();
    expect(screen.getByText("Playtest ended")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "View event: Campus playtest" })).toHaveAttribute(
      "href",
      "/testing-lab/events/event-1",
    );
  });

  it("uses the tester's saved time zone and labels a full but open session as waitlist", () => {
    render(
      <TestingEventCard
        session={event({
          startsAt: "2026-09-15T00:30:00.000Z",
          endsAt: "2026-09-15T02:00:00.000Z",
          timeZoneId: "America/Los_Angeles",
          availableTesterCount: 0,
        })}
      />,
    );

    expect(screen.getByText("Sep 14, 2026")).toBeInTheDocument();
    expect(screen.getByText("5:30 PM PDT")).toBeInTheDocument();
    expect(screen.getByText("Waitlist open")).toBeInTheDocument();
    expect(screen.queryByText("0 spots left")).not.toBeInTheDocument();
  });

  it("distinguishes developer submissions from tester sign-up", () => {
    render(
      <TestingEventCard
        session={event({
          statusLabel: "Game submissions open",
          testerRegistrationOpen: false,
          gameSubmissionsOpen: true,
          availableTesterCount: 0,
        })}
      />,
    );

    expect(screen.getByText("Game submissions open")).toBeInTheDocument();
    expect(screen.queryByText(/spots? left/)).not.toBeInTheDocument();
  });

  it("uses the real game artwork instead of generated event cover art", () => {
    const { container } = render(
      <TestingEventCard session={event({ gameImageUrl: "/games/lantern-lake.webp" })} />,
    );

    expect(container.querySelector("img")).toHaveAttribute(
      "src",
      "/games/lantern-lake.webp",
    );
  });

  it.each([
    ["open", "Open"],
    ["in-progress", "In Progress"],
    ["completed", "Completed"],
    ["closed", "Closed"],
  ] as const)("renders %s status in the row view", (status, statusLabel) => {
    render(<TestingEventRow session={event({ status, statusLabel })} />);
    expect(screen.getByText(statusLabel)).toBeInTheDocument();
    expect(screen.getByRole("article")).toHaveTextContent("Main campus - Lab 2");
  });

  it("renders tabular date, duration, and capacity variants", () => {
    render(
      <TestingEventsTable
        projectId="project-1"
        sessions={[
          event({ id: "minutes", startsAt: "2026-09-15T18:30:00Z", endsAt: "2026-09-15T19:15:00Z" }),
          event({ id: "hours", title: "Two hours", startsAt: "2026-09-16T18:30:00Z", endsAt: "2026-09-16T20:30:00Z" }),
          event({ id: "mixed", title: "Mixed duration", startsAt: "2026-09-17T18:30:00Z", endsAt: "2026-09-17T20:45:00Z" }),
          event({ id: "pending", title: "Pending", startsAt: undefined, endsAt: undefined }),
          event({ id: "invalid", title: "Invalid", startsAt: "invalid", endsAt: "invalid" }),
          event({ id: "backwards", title: "Backwards", startsAt: "2026-09-18T20:00:00Z", endsAt: "2026-09-18T19:00:00Z" }),
        ]}
      />,
    );

    expect(screen.getByText("45 min")).toBeInTheDocument();
    expect(screen.getByText("2h")).toBeInTheDocument();
    expect(screen.getByText("2h 15m")).toBeInTheDocument();
    expect(screen.getAllByText("TBD")).toHaveLength(3);
    expect(screen.getAllByText("Schedule pending")).toHaveLength(2);
    expect(screen.getAllByText("Time pending")).toHaveLength(2);
    const pendingRow = screen.getByText("Pending").closest("tr");
    expect(pendingRow).not.toBeNull();
    expect(within(pendingRow!).getByRole("link", { name: "View event" })).toHaveAttribute(
      "href",
      "/testing-lab/events/pending?projectId=project-1",
    );
  });
});
