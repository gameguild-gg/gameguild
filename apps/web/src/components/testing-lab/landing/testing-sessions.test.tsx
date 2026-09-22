import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactElement, ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { TestingEventViewModel } from "./testing-events-presentation";

vi.mock("@/i18n/navigation", () => ({
  Link: ({ children, href, ...props }: { children: ReactNode; href: string }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

vi.mock("@game-guild/ui/components/select", async () => {
  const React = await import("react");
  type MockElement = ReactElement<{
    "aria-label"?: string;
    children?: ReactNode;
    value?: string;
  }>;
  return {
    Select: ({
      value,
      onValueChange,
      children,
    }: {
      value: string;
      onValueChange: (value: string) => void;
      children: ReactNode;
    }) => {
      const [trigger, content] = React.Children.toArray(children) as MockElement[];
      const items = React.Children.toArray(content?.props.children) as MockElement[];
      return (
        <select
          aria-label={trigger?.props["aria-label"]}
          value={value}
          onChange={(event) => onValueChange(event.target.value)}
        >
          {items.map((item) => (
            <option key={item.props.value} value={item.props.value}>
              {item.props.children}
            </option>
          ))}
        </select>
      );
    },
    SelectTrigger: () => null,
    SelectValue: () => null,
    SelectContent: () => null,
    SelectItem: () => null,
  };
});

import { TestingEventsBrowser } from "./testing-sessions";

function event(
  id: string,
  overrides: Partial<TestingEventViewModel> = {},
): TestingEventViewModel {
  return {
    id,
    title: `Event ${id}`,
    description: `Description ${id}`,
    mode: "Online",
    status: "open",
    statusLabel: "Open",
    startsAt: "2026-09-20T18:00:00.000Z",
    endsAt: "2026-09-20T20:00:00.000Z",
    location: "Online",
    testerCount: 1,
    testerLimit: 10,
    projectCount: 1,
    projectLimit: 4,
    availableTesterCount: 9,
    scheduleCount: 1,
    ...overrides,
  };
}

const events = [
  event("october", {
    title: "Hybrid October",
    description: "Console build",
    mode: "Hybrid",
    startsAt: "2026-10-02T18:00:00.000Z",
    endsAt: "2026-10-02T20:00:00.000Z",
    location: "Remote and campus",
  }),
  event("past", {
    title: "Completed campus session",
    description: "Archived action build",
    mode: "In person",
    status: "completed",
    statusLabel: "Completed",
    startsAt: "2026-09-10T18:00:00.000Z",
    endsAt: "2026-09-10T20:00:00.000Z",
    location: "North campus",
  }),
  event("future", {
    title: "Online future session",
    description: "Cooperative game",
  }),
];

describe("TestingEventsBrowser", () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    vi.setSystemTime(new Date("2026-09-15T12:00:00.000Z"));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("renders the live-data warning and empty directory actions", () => {
    render(<TestingEventsBrowser events={[]} accessIssues={["API unavailable"]} />);

    expect(screen.getByRole("alert")).toHaveTextContent(
      "Live testing events could not be refreshed",
    );
    expect(screen.getByRole("heading", { name: "No events available" })).toBeInTheDocument();
    expect(screen.queryByLabelText("Event filters")).not.toBeInTheDocument();
  });

  it("sorts cards by schedule and preserves project context", () => {
    render(
      <TestingEventsBrowser
        events={events}
        accessIssues={[]}
        projectId="project / 1"
      />,
    );

    expect(
      screen.queryByText(/Open Events? - Join Now!/),
    ).not.toBeInTheDocument();
    const links = screen.getAllByRole("link", { name: "View event" });
    expect(links.map((link) => link.getAttribute("href"))).toEqual([
      "/testing-lab/events/past?projectId=project%20%2F%201",
      "/testing-lab/events/future?projectId=project%20%2F%201",
      "/testing-lab/events/october?projectId=project%20%2F%201",
    ]);
    expect(screen.getByText("3 of 3 events")).toBeInTheDocument();
  });

  it.each([
    ["cooperative", "Online future session"],
    ["north campus", "Completed campus session"],
    ["hybrid", "Hybrid October"],
  ])("searches normalized event metadata for %s", async (term, expectedTitle) => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(<TestingEventsBrowser events={events} accessIssues={[]} />);

    await user.type(screen.getByPlaceholderText("Search events..."), `  ${term}  `);

    expect(screen.getByText(expectedTitle)).toBeInTheDocument();
    expect(screen.getByText("1 of 3 events")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Clear filters" }));
    expect(screen.getByText("3 of 3 events")).toBeInTheDocument();
  });

  it("filters by status", async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(<TestingEventsBrowser events={events} accessIssues={[]} />);

    await user.selectOptions(
      screen.getByRole("combobox", { name: "Filter by status" }),
      "completed",
    );

    expect(screen.getByText("Completed campus session")).toBeInTheDocument();
    expect(screen.queryByText("Online future session")).not.toBeInTheDocument();
  });

  it("maps the in-person filter to the presented label", async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(<TestingEventsBrowser events={events} accessIssues={[]} />);

    await user.selectOptions(
      screen.getByRole("combobox", { name: "Filter by format" }),
      "InPerson",
    );

    expect(screen.getByText("Completed campus session")).toBeInTheDocument();
    expect(screen.getByText("1 of 3 events")).toBeInTheDocument();
  });

  it("filters other formats without label remapping", async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(<TestingEventsBrowser events={events} accessIssues={[]} />);

    await user.selectOptions(
      screen.getByRole("combobox", { name: "Filter by format" }),
      "Hybrid",
    );

    expect(screen.getByText("Hybrid October")).toBeInTheDocument();
    expect(screen.getByText("1 of 3 events")).toBeInTheDocument();
  });

  it("sorts unscheduled events deterministically before scheduled events", () => {
    const { rerender } = render(
      <TestingEventsBrowser
        events={[
          event("scheduled"),
          event("unscheduled", { startsAt: undefined, endsAt: undefined }),
        ]}
        accessIssues={[]}
      />,
    );
    expect(screen.getAllByRole("link", { name: "View event" })[0]).toHaveAttribute(
      "href",
      "/testing-lab/events/unscheduled",
    );

    rerender(
      <TestingEventsBrowser
        events={[
          event("unscheduled", { startsAt: undefined, endsAt: undefined }),
          event("scheduled"),
        ]}
        accessIssues={[]}
      />,
    );
    expect(screen.getAllByRole("link", { name: "View event" })[0]).toHaveAttribute(
      "href",
      "/testing-lab/events/unscheduled",
    );
  });

  it.each([
    ["Upcoming", ["Online future session", "Hybrid October"]],
    ["This month", ["Completed campus session", "Online future session"]],
  ])("filters the %s schedule", async (option, expectedTitles) => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <TestingEventsBrowser
        events={[...events, event("unscheduled", { title: "Unscheduled", startsAt: undefined })]}
        accessIssues={[]}
      />,
    );

    await user.selectOptions(
      screen.getByRole("combobox", { name: "Filter by schedule" }),
      option === "Upcoming" ? "upcoming" : "month",
    );

    for (const title of expectedTitles) {
      expect(screen.getByText(title)).toBeInTheDocument();
    }
    expect(screen.queryByText("Unscheduled")).not.toBeInTheDocument();
    expect(screen.getByText("2 of 4 events")).toBeInTheDocument();
  });

  it("shows a filtered empty state and restores results", async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(<TestingEventsBrowser events={events} accessIssues={[]} />);

    await user.type(screen.getByPlaceholderText("Search events..."), "missing event");
    expect(screen.getByRole("heading", { name: "No events match your filters" })).toBeInTheDocument();
    const clearActions = screen.getAllByRole("button", { name: "Clear filters" });
    await user.click(clearActions.at(-1)!);
    expect(screen.getAllByRole("link", { name: "View event" })).toHaveLength(3);
  });

  it("switches between row and table views", async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(<TestingEventsBrowser events={[events[2]!]} accessIssues={[]} />);

    expect(
      screen.queryByText(/Open Events? - Join Now!/),
    ).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Switch to cards view" })).toHaveAttribute(
      "aria-pressed",
      "true",
    );

    await user.click(screen.getByRole("button", { name: "Switch to rows view" }));
    expect(screen.getByRole("article")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Switch to table view" }));
    expect(screen.getByRole("table")).toBeInTheDocument();
  });
});
