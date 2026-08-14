import { describe, expect, it, vi } from "vitest";

vi.mock("@/i18n/navigation", () => ({
  usePathname: () => "/dashboard",
  useRouter: () => ({ push: vi.fn() }),
}));

import { filterDashboardQuickActions } from "./dashboard-command-palette";

describe("dashboard command palette authorization", () => {
  it("does not expose administrative quick actions to a regular member", () => {
    expect(filterDashboardQuickActions([])).toEqual([]);
  });

  it("exposes only quick actions backed by an actor capability", () => {
    const actions = filterDashboardQuickActions([
      "TestingLab.ManageEvents",
      "Community.ManageMembers",
    ]);

    expect(actions.map((action) => action.title)).toEqual([
      "Review testing lab",
      "Manage members",
    ]);
  });
});
