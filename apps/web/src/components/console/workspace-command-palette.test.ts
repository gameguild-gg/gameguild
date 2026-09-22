import { describe, expect, it, vi } from "vitest";

vi.mock("@/i18n/navigation", () => ({
  usePathname: () => "/dashboard",
  useRouter: () => ({ push: vi.fn() }),
}));

import { filterWorkspaceQuickActions } from "./workspace-command-palette";

describe("dashboard command palette authorization", () => {
  it("does not expose administrative quick actions to a regular member", () => {
    expect(filterWorkspaceQuickActions([])).toEqual([]);
  });

  it("exposes only quick actions backed by an actor capability", () => {
    const actions = filterWorkspaceQuickActions([
      "TestingLab.ManageEvents",
      "Community.ManageMembers",
    ]);

    expect(actions.map((action) => action.title)).toEqual([
      "Review testing lab",
      "Manage members",
    ]);
  });
});
