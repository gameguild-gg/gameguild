import { describe, expect, it, vi } from "vitest";

vi.mock("@/i18n/navigation", () => ({
  Link: "a",
  usePathname: () => "/dashboard/community/testing-lab",
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
      { title: "Overview", url: "/dashboard/community/testing-lab" },
      { title: "Events", url: "/dashboard/community/testing-lab/events" },
      { title: "Applications", url: "/dashboard/community/testing-lab/applications" },
      { title: "Projects", url: "/dashboard/community/testing-lab/projects" },
      { title: "Participants", url: "/dashboard/community/testing-lab/participants" },
      { title: "Feedback", url: "/dashboard/community/testing-lab/feedback" },
      { title: "Analytics", url: "/dashboard/community/testing-lab/analytics" },
      { title: "Locations", url: "/dashboard/community/testing-lab/locations" },
      { title: "Access", url: "/dashboard/community/testing-lab/access" },
      { title: "Settings", url: "/dashboard/community/testing-lab/settings" },
    ]);
    expect(launchPad?.url).toBe("/dashboard/community/launch-pad");
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
