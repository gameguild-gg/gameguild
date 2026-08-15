import { describe, expect, it, vi } from "vitest";

vi.mock("@/i18n/navigation", () => ({
  Link: "a",
  usePathname: () => "/dashboard/testing-lab",
}));

import {
  dashboardNavigationData,
  filterDashboardNavigation,
} from "./dashboard-sidebar";

describe("dashboard management navigation", () => {
  it("places Testing Lab and Launch Pad under Community Management", () => {
    const community = dashboardNavigationData.find(
      (group) => group.label === "Community Management",
    );
    const testingLab = community?.items.find(
      (item) => item.title === "Testing Lab",
    );
    const launchPad = community?.items.find(
      (item) => item.title === "Launch Pad",
    );
    const platform = dashboardNavigationData.find(
      (group) => group.label === "Platform Management",
    );

    expect(
      testingLab?.subGroups?.map(({ title, url }) => ({ title, url })),
    ).toEqual([
      { title: "Overview", url: "/dashboard/testing-lab" },
      { title: "Events", url: "/dashboard/testing-lab/events" },
      { title: "Applications", url: "/dashboard/testing-lab/applications" },
      { title: "Projects", url: "/dashboard/testing-lab/projects" },
      { title: "Participants", url: "/dashboard/testing-lab/participants" },
      { title: "Feedback", url: "/dashboard/testing-lab/feedback" },
      { title: "Analytics", url: "/dashboard/testing-lab/analytics" },
      { title: "Locations", url: "/dashboard/testing-lab/locations" },
      { title: "Access", url: "/dashboard/testing-lab/access" },
      { title: "Settings", url: "/dashboard/testing-lab/settings" },
    ]);
    expect(launchPad?.url).toBe("/dashboard/launch-pad");
    expect(platform?.items.map((item) => item.title)).toEqual([
      "Roles",
      "Learning",
    ]);
  });

  it("hides administrative modules from a regular member", () => {
    const navigation = filterDashboardNavigation(dashboardNavigationData, []);

    expect(navigation.map((group) => group.label)).toEqual(["Overview"]);
    expect(navigation[0]?.items.map((item) => item.title)).toEqual(["Dashboard"]);
  });

  it("shows only the administrative module granted to the actor", () => {
    const navigation = filterDashboardNavigation(dashboardNavigationData, [
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
    ).toEqual(["Overview", "Events"]);
  });
});
