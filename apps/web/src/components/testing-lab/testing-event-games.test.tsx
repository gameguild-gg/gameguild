import "@testing-library/jest-dom/vitest";
import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { createElement } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { TestingLabPublicTestingEventGameProjection } from "@game-guild/client";

vi.mock("next/image", () => ({
  default: (props: Record<string, unknown>) =>
    createElement(
      "img",
      Object.fromEntries(
        Object.entries(props).filter(([key]) => key !== "fill" && key !== "unoptimized"),
      ),
    ),
}));
vi.mock("@game-guild/ui/components/badge", () => ({
  Badge: ({ children }: { children: React.ReactNode }) =>
    createElement("span", null, children),
}));
vi.mock("@/i18n/navigation", () => ({
  Link: ({ children, ...props }: React.ComponentProps<"a">) =>
    createElement("a", props, children),
}));

import { TestingEventGames } from "./testing-event-games";

const games: TestingLabPublicTestingEventGameProjection[] = [
  {
    projectId: "mothlight",
    title: "Mothlight",
    description: "Restore a moonlit valley.",
    imageUrl: "/mothlight.svg",
  },
  {
    projectId: "hollow-signal",
    title: "Hollow Signal",
    description: "Follow a radio signal.",
    imageUrl: "/hollow-signal.svg",
  },
];

describe("TestingEventGames hero carousel", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    Object.defineProperty(window, "matchMedia", {
      configurable: true,
      value: vi.fn().mockReturnValue({
        matches: false,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
      }),
    });
  });

  afterEach(() => {
    cleanup();
    vi.useRealTimers();
  });

  it("automatically advances and pauses while the reader is interacting", async () => {
    render(<TestingEventGames games={games} eventId="event-1" />);
    await act(async () => {
      vi.advanceTimersByTime(7000);
    });
    expect(
      screen.getByRole("region", { name: "Featured game: Hollow Signal" }),
    ).toBeInTheDocument();

    const currentCarousel = screen.getByRole("region", {
      name: "Featured game: Hollow Signal",
    });
    fireEvent.mouseEnter(currentCarousel);
    await act(async () => {
      vi.advanceTimersByTime(7000);
    });
    expect(currentCarousel).toBeInTheDocument();
    expect(
      screen.queryByRole("region", { name: "Featured game: Mothlight" }),
    ).not.toBeInTheDocument();
  });

  it("does not autoplay for readers who prefer reduced motion", async () => {
    window.matchMedia = vi.fn().mockReturnValue({
      matches: true,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    });
    render(<TestingEventGames games={games} eventId="event-1" />);

    await act(async () => {
      vi.advanceTimersByTime(14000);
    });

    expect(
      screen.getByRole("region", { name: "Featured game: Mothlight" }),
    ).toBeInTheDocument();
  });
});
