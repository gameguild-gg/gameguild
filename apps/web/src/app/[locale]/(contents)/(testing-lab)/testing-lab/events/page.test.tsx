import { fireEvent, render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  getPublicTestingEventsDirectory: vi.fn(),
}));

vi.mock("@/lib/testing-lab/events-queries", () => ({
  getPublicTestingEventsDirectory: mocks.getPublicTestingEventsDirectory,
}));

vi.mock("@/i18n/navigation", () => ({
  Link: ({
    children,
    href,
    ...rest
  }: {
    children: ReactNode;
    href: string;
  }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

import TestingLabEventsPage from "./page";

describe("Public Testing Lab events directory", () => {
  beforeEach(() => vi.clearAllMocks());

  it("uses the legacy events UX with real public event data", async () => {
    mocks.getPublicTestingEventsDirectory.mockResolvedValue({
      accessIssues: [],
      events: [
        {
          id: "event-1",
          name: "August campus playtest",
          description: "Test community games with their creators.",
          mode: "InPerson",
          status: "ApplicationsOpen",
          applicationCount: 3,
          startsAt: "2026-08-12T18:00:00.000Z",
          endsAt: "2026-08-12T20:00:00.000Z",
          slots: [
            {
              id: "slot-1",
              campusName: "Downtown campus",
              roomName: "Play Lab",
              availableTesterCount: 7,
              availableProjectCount: 2,
              registeredTesterCount: 3,
              approvedProjectCount: 1,
              maxTesters: 10,
              maxProjects: 3,
            },
          ],
        },
      ],
    });

    render(await TestingLabEventsPage());

    expect(
      screen.getByRole("heading", { name: "Test. Play. Earn." }),
    ).toBeInTheDocument();
    expect(screen.getByText(/1 open event/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText("Search events...")).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Switch to cards view" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Switch to rows view" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Switch to table view" }),
    ).toBeInTheDocument();
    expect(screen.getByText("August campus playtest")).toBeInTheDocument();
    expect(screen.getByText(/Downtown campus/)).toBeInTheDocument();
    expect(screen.getByText("3/10 testers")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "View event" })).toHaveAttribute(
      "href",
      "/testing-lab/events/event-1",
    );
    expect(mocks.getPublicTestingEventsDirectory).toHaveBeenCalledWith({
      take: 100,
    });

    fireEvent.change(screen.getByPlaceholderText("Search events..."), {
      target: { value: "not present" },
    });
    expect(
      screen.getByRole("heading", { name: "No events match your filters" }),
    ).toBeInTheDocument();
  });

  it("renders the legacy no-events state without mock data", async () => {
    mocks.getPublicTestingEventsDirectory.mockResolvedValue({
      accessIssues: [],
      events: [],
    });

    render(await TestingLabEventsPage());

    expect(
      screen.getByRole("heading", { name: "No events available" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: "Back to Testing Lab" }),
    ).toHaveAttribute("href", "/testing-lab");
  });
});
