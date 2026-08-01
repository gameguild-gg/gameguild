import { describe, expect, it, vi } from "vitest";

vi.mock("@/i18n/navigation", () => ({
  Link: "a",
  usePathname: () => "/dashboard/testing-lab",
}));

import { dashboardNavigationData } from "./dashboard-sidebar";

describe("dashboard Testing Lab navigation", () => {
  it("exposes only the six canonical work areas", () => {
    const platform = dashboardNavigationData.find(
      (group) => group.label === "Platform Management",
    );
    const testingLab = platform?.items.find(
      (item) => item.title === "Testing Lab",
    );

    expect(
      testingLab?.subGroups?.map(({ title, url }) => ({ title, url })),
    ).toEqual([
      { title: "Overview", url: "/dashboard/testing-lab" },
      { title: "Events", url: "/dashboard/testing-lab/events" },
      { title: "Projects", url: "/dashboard/testing-lab/projects" },
      { title: "Participants", url: "/dashboard/testing-lab/participants" },
      { title: "Analytics", url: "/dashboard/testing-lab/analytics" },
      { title: "Settings", url: "/dashboard/testing-lab/settings" },
    ]);
  });
});
