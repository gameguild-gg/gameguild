import { render, screen } from "@testing-library/react";
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

import TestingLabPage from "./page";

describe("Public Testing Lab page", () => {
  beforeEach(() => vi.clearAllMocks());

  it("renders the legacy landing and links to the dedicated events directory", async () => {
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
          slots: [
            {
              id: "slot-1",
              campusName: "Downtown campus",
              availableTesterCount: 7,
              availableProjectCount: 2,
            },
          ],
        },
      ],
    });

    render(await TestingLabPage());

    expect(
      screen.getByRole("heading", { name: "Game Testing Lab" }),
    ).toBeInTheDocument();
    expect(
      screen.getByText("Community playtesting is live"),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "How to Get Involved" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Browse Events" })).toHaveAttribute(
      "href",
      "/testing-lab/events",
    );
    expect(
      screen.queryByText("August campus playtest"),
    ).not.toBeInTheDocument();
    expect(screen.getByText("Total Events")).toBeInTheDocument();
    expect(screen.getByText("Tester Seats")).toBeInTheDocument();
    expect(screen.getByText("7")).toBeInTheDocument();
    expect(mocks.getPublicTestingEventsDirectory).toHaveBeenCalledWith({
      take: 100,
    });
  });

  it("keeps the legacy landing available when no public event exists", async () => {
    mocks.getPublicTestingEventsDirectory.mockResolvedValue({
      accessIssues: [],
      events: [],
    });

    render(await TestingLabPage());

    expect(
      screen.getByRole("heading", { name: "Game Testing Lab" }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("heading", { name: "No public testing events" }),
    ).not.toBeInTheDocument();
  });
});
