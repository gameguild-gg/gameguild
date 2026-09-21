import { describe, expect, it, vi } from "vitest";

vi.mock("@/i18n/navigation", () => ({
  Link: "a",
  usePathname: () => "/workspace/testing-lab",
}));

import {
  workspaceNavigationData,
  filterWorkspaceNavigation,
} from "./workspace-sidebar";

describe("dashboard management navigation", () => {
  it("places Testing Lab and Launch Pad under an explicit administration scope", () => {
    const community = workspaceNavigationData.find(
      (group) => group.label === "Community Management",
    );
    const testingLab = community?.items.find(
      (item) => item.title === "Testing Lab",
    );
    const launchPad = community?.items.find(
      (item) => item.title === "Launch Pad",
    );
    const platform = workspaceNavigationData.find(
      (group) => group.label === "Platform Management",
    );

    expect(
      testingLab?.subGroups?.map(({ title, url }) => ({ title, url })),
    ).toEqual([
      { title: "Sessions", url: "/workspace/testing-lab/events" },
      { title: "Settings", url: "/workspace/testing-lab/settings" },
    ]);
    expect(launchPad?.url).toBe("/console/community/launch-pad");
    expect(platform?.items.map((item) => item.title)).toEqual(["Economy", "Roles"]);
    const economy = platform?.items.find((item) => item.title === "Economy");
    expect(economy?.subGroups?.map(({ title, url }) => ({ title, url }))).toEqual([
      { title: "Payout review", url: "/console/economy/payout-reviews" },
    ]);
    const learning = community?.items.find((item) => item.title === "Learning");
    expect(learning?.subGroups?.map(({ url }) => url)).toEqual([
      "/console/learning",
      "/console/learning/courses",
      "/console/learning/tutorials",
      "/console/learning/resources",
    ]);
  });

  it("hides administrative modules from a regular member", () => {
    const navigation = filterWorkspaceNavigation(workspaceNavigationData, []);

    expect(navigation.map((group) => group.label)).toEqual(["Workspace"]);
    expect(navigation[0]?.items.map((item) => item.title)).toEqual([
      "Home",
      "Projects",
      "Teams",
      "Learning",
      "Calendar",
    ]);
  });

  it("keeps Projects and Teams as direct workspace links without child routes", () => {
    const workspace = workspaceNavigationData.find(
      (group) => group.label === "Workspace",
    );
    const projects = workspace?.items.find((item) => item.title === "Projects");
    const teams = workspace?.items.find((item) => item.title === "Teams");

    expect(projects).toMatchObject({ url: "/workspace/projects" });
    expect(projects?.subGroups).toBeUndefined();
    expect(teams).toMatchObject({ url: "/workspace/teams" });
    expect(teams?.subGroups).toBeUndefined();
  });

  it("shows only the administrative module granted to the actor", () => {
    const navigation = filterWorkspaceNavigation(workspaceNavigationData, [
      "TestingLab.ManageEvents",
    ]);
    const community = navigation.find(
      (group) => group.label === "Community Management",
    );

    expect(community?.items.map((item) => item.title)).toEqual([
      "Testing Lab",
    ]);
    expect(
      community?.items[0]?.subGroups?.map((item) => item.title),
    ).toEqual(["Sessions"]);
  });

  it("keeps global Testing Lab settings grouped behind one entry", () => {
    const navigation = filterWorkspaceNavigation(workspaceNavigationData, [
      "TestingLab.ManageSettings",
      "TestingLab.ViewAnalytics",
    ]);
    const testingLab = navigation
      .find((group) => group.label === "Community Management")
      ?.items.find((item) => item.title === "Testing Lab");

    expect(testingLab?.subGroups?.map((item) => item.title)).toEqual([
      "Settings",
    ]);
  });
});
