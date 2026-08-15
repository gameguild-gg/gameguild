import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  request: vi.fn(),
  getToken: vi.fn(),
}));

vi.mock("@/auth", () => ({ getToken: mocks.getToken }));
vi.mock("@game-guild/client", () => ({
  createServerClient: mocks.createServerClient,
}));

import {
  getDashboardContexts,
  hasAnyDashboardCapability,
} from "./dashboard-contexts";

describe("dashboard contexts query", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.createServerClient.mockReturnValue({ request: mocks.request });
  });

  it("loads management capabilities without requesting personal workspace contexts", async () => {
    mocks.request.mockResolvedValue({
      ok: true,
      data: {
        capabilities: ["TestingLab.ManageEvents"],
      },
    });

    const result = await getDashboardContexts();

    expect(mocks.request).toHaveBeenCalledWith({
      method: "GET",
      path: "/v1/access/capabilities",
    });
    expect(result.capabilities).toEqual(["TestingLab.ManageEvents"]);
    expect(result.contexts.map((context) => context.type)).toEqual([
      "Operations",
    ]);
    expect(result.counts).toEqual({
      teams: 0,
      projects: 0,
      pendingTasks: 0,
      invitations: 0,
    });
  });

  it("fails closed when contexts cannot be loaded", async () => {
    mocks.request.mockResolvedValue({ ok: false, error: { status: 503 } });

    await expect(getDashboardContexts()).resolves.toEqual({
      contexts: [],
      capabilities: [],
      counts: { teams: 0, projects: 0, pendingTasks: 0, invitations: 0 },
      navigation: [],
    });
  });

  it("matches management modules without treating participation as management", () => {
    expect(
      hasAnyDashboardCapability(
        ["TestingLab.ManageEvents"],
        "TestingLab.",
      ),
    ).toBe(true);
    expect(
      hasAnyDashboardCapability(
        ["TestingLab.Participate"],
        "TestingLab.Manage",
        "TestingLab.Review",
        "TestingLab.ViewAnalytics",
      ),
    ).toBe(false);
  });
});
