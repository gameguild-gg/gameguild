import { fireEvent, render, screen } from "@testing-library/react";
import { CalendarDays } from "lucide-react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";

let pathname: string | null = "/workspace/testing-lab/events/event-1/overview";

vi.mock("@/i18n/navigation", () => ({
  usePathname: () => pathname,
  Link: ({ children, href, ...props }: { children: ReactNode; href: string }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

vi.mock("next/link", () => ({
  default: ({ children, href, ...props }: { children: ReactNode; href: string }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

import { TestingEventWorkspaceNav } from "./testing-event-workspace-nav";
import { TestingLabPageHeader } from "./testing-lab-page-header";
import { TestingLabAccessIssues, TestingLabEmptyState } from "./testing-lab-state";
import { TestingEventsEmptyState } from "./landing/testing-events-empty-state";
import { TestingLabHero } from "./landing/testing-lab-hero";
import { TestingLabHowItWorks } from "./landing/testing-lab-how-it-works";
import { TestingLabLearnMore } from "./landing/testing-lab-learn-more";
import { TestingLabStats } from "./landing/testing-lab-stats";

describe("Testing Lab foundations", () => {
  beforeEach(() => {
    pathname = "/workspace/testing-lab/events/event-1/overview";
  });

  it("renders the page header hierarchy, actions, navigation, and border variants", () => {
    const { rerender } = render(
      <TestingLabPageHeader
        icon={CalendarDays}
        title="Testing Lab"
        description="Manage testing sessions"
        actions={<button type="button">New event</button>}
        navigation={<a href="#calendar">Calendar</a>}
      />,
    );

    expect(screen.getByRole("heading", { level: 1, name: "Testing Lab" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "New event" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Calendar" })).toHaveAttribute("href", "#calendar");
    expect(screen.getByRole("banner")).toHaveClass("border-b");

    rerender(
      <TestingLabPageHeader
        icon={CalendarDays}
        title="Schedule"
        description="Event schedule"
        headingLevel={2}
        bordered={false}
      />,
    );
    expect(screen.getByRole("heading", { level: 2, name: "Schedule" })).toBeInTheDocument();
    expect(screen.getByRole("banner")).not.toHaveClass("border-b");
  });

  it("renders access errors only when issues exist", () => {
    const { rerender } = render(<TestingLabAccessIssues issues={[]} />);
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();

    rerender(<TestingLabAccessIssues issues={["Locations failed.", "Access failed."]} />);
    expect(screen.getByRole("alert")).toHaveTextContent(
      "Locations failed. Access failed.",
    );
  });

  it("renders an actionable generic empty state", () => {
    const { rerender } = render(
      <TestingLabEmptyState
        title="No sessions"
        description="Create the first testing session."
        action={<button type="button">Create session</button>}
      />,
    );
    expect(screen.getByRole("heading", { name: "No sessions" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Create session" })).toBeInTheDocument();

    rerender(
      <TestingLabEmptyState
        title="No sessions"
        description="Create the first testing session."
      />,
    );
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });

  it("marks nested event routes active and limits non-managers to applications", () => {
    pathname = "/workspace/testing-lab/events/event-1/schedule/slot-1";
    const { rerender } = render(<TestingEventWorkspaceNav eventId="event-1" />);

    expect(screen.getByRole("link", { name: "Schedule" })).toHaveAttribute(
      "aria-current",
      "page",
    );
    expect(screen.getByRole("link", { name: "Overview" })).not.toHaveAttribute(
      "aria-current",
    );

    rerender(
      <TestingEventWorkspaceNav eventId="event-1" canManageWorkspace={false} />,
    );
    expect(screen.getAllByRole("link")).toHaveLength(1);
    expect(screen.getByRole("link", { name: "Applications" })).toHaveAttribute(
      "href",
      "/workspace/testing-lab/events/event-1/applications",
    );
  });

  it("keeps navigation usable before the router provides a pathname", () => {
    pathname = null;
    render(<TestingEventWorkspaceNav eventId="event-1" />);

    expect(screen.getAllByRole("link")).toHaveLength(6);
    expect(screen.getByRole("link", { name: "Overview" })).not.toHaveAttribute(
      "aria-current",
    );
  });

  it("lets users clear filters or navigate from a truly empty directory", () => {
    const clearFilters = vi.fn();
    const { rerender } = render(
      <TestingEventsEmptyState
        filtered
        hasEvents
        clearFilters={clearFilters}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Clear filters" }));
    expect(clearFilters).toHaveBeenCalledOnce();

    rerender(
      <TestingEventsEmptyState
        filtered={false}
        hasEvents={false}
        clearFilters={clearFilters}
      />,
    );
    expect(screen.getByRole("link", { name: "Prepare a project" })).toHaveAttribute(
      "href",
      "/workspace/projects",
    );
    expect(screen.getByRole("link", { name: "Back to Testing Lab" })).toHaveAttribute(
      "href",
      "/testing-lab",
    );
  });

  it("exposes the public landing content and calls to action", () => {
    render(
      <>
        <TestingLabHero />
        <TestingLabHowItWorks />
        <TestingLabLearnMore />
        <TestingLabStats
          totalEvents={12}
          openEvents={3}
          upcomingEvents={5}
          openTesterSeats={42}
        />
      </>,
    );

    expect(screen.getByRole("heading", { level: 1, name: "Game Testing Lab" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Browse Events" })).toHaveAttribute(
      "href",
      "/testing-lab",
    );
    expect(screen.getByRole("link", { name: "Submit a project" })).toHaveAttribute(
      "href",
      "/workspace/projects",
    );
    expect(screen.getByRole("heading", { name: "How to Get Involved" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Learn More" })).toHaveAttribute(
      "href",
      "#learn-more",
    );
    expect(screen.getByText("12")).toBeInTheDocument();
    expect(screen.getByText("42")).toBeInTheDocument();
  });
});
