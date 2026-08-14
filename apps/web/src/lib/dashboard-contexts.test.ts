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

  it("loads capabilities from the authenticated contexts endpoint", async () => {
    mocks.request.mockResolvedValue({
      ok: true,
      data: {
        contexts: [
          { type: "Workspace", id: null, name: "Workspace", route: "/dashboard" },
          { type: "Operations", id: null, name: "Operations", route: "/dashboard" },
        ],
        capabilities: ["TestingLab.ManageEvents"],
        counts: { teams: 2, projects: 4, pendingTasks: 1, invitations: 3 },
      },
    });

    const result = await getDashboardContexts();

    expect(mocks.request).toHaveBeenCalledWith({
      method: "GET",
      path: "/v1/dashboard/contexts",
    });
    expect(result.capabilities).toEqual(["TestingLab.ManageEvents"]);
    expect(result.contexts.map((context) => context.type)).toEqual([
      "Workspace",
      "Operations",
    ]);
    expect(result.counts).toEqual({
      teams: 2,
      projects: 4,
      pendingTasks: 1,
      invitations: 3,
    });
  });

  it("fails closed when contexts cannot be loaded", async () => {
    mocks.request.mockResolvedValue({ ok: false, error: { status: 503 } });

    await expect(getDashboardContexts()).resolves.toEqual({
      contexts: [
        { type: "Workspace", id: null, name: "Workspace", route: "/dashboard" },
      ],
      capabilities: [],
      counts: { teams: 0, projects: 0, pendingTasks: 0, invitations: 0 },
      navigation: [
        {
          label: "Overview",
          items: [
            { title: "Dashboard", route: "/dashboard", children: [] },
            { title: "Invitations", route: "/dashboard/invitations", children: [] },
          ],
        },
      ],
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
