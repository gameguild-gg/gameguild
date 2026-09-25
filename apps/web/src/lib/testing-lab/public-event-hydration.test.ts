import { beforeEach, describe, expect, it, vi } from "vitest";

const { getPublicTestingEvent } = vi.hoisted(() => ({
  getPublicTestingEvent: vi.fn(),
}));

vi.mock("./events-public-queries", () => ({ getPublicTestingEvent }));

import { hydratePublicTestingEvent } from "./public-event-hydration";

describe("hydratePublicTestingEvent", () => {
  beforeEach(() => vi.clearAllMocks());

  it("passes distinct game covers from the public event projection to the social banner", async () => {
    getPublicTestingEvent.mockResolvedValue({
      name: "Game Jam Sprint Playtest",
      slots: [],
      games: [
        { projectId: "game-1", title: "Mothlight", imageUrl: " /testing-lab/seeded-games/mothlight.svg " },
        { projectId: "game-2", title: "Hollow Signal", imageUrl: "/testing-lab/seeded-games/hollow-signal.svg" },
        { projectId: "game-3", title: "Duplicate artwork", imageUrl: "/testing-lab/seeded-games/mothlight.svg" },
        { projectId: "game-4", title: "No cover" },
      ],
    });

    const event = await hydratePublicTestingEvent("490dc4af-4480-41cb-89a1-41dd5e555d62");

    expect(event?.gameImages).toEqual([
      "/testing-lab/seeded-games/mothlight.svg",
      "/testing-lab/seeded-games/hollow-signal.svg",
    ]);
  });
});
