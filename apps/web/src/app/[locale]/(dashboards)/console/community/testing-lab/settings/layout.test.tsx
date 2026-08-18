import { render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/i18n/navigation", () => ({
  usePathname: () => "/console/community/testing-lab/settings/locations",
  Link: ({
    children,
    href,
    ...props
  }: {
    children: ReactNode;
    href: string;
  }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

import TestingLabSettingsLayout from "./layout";

vi.mock("@/lib/require-dashboard-capability", () => ({
  requireDashboardCapability: vi.fn().mockResolvedValue(undefined),
}));

describe("TestingLabSettingsLayout", () => {
  it("groups general, location, and access settings without horizontal overflow", async () => {
    render(await TestingLabSettingsLayout({ children: <div>Settings content</div> }));

    const navigation = screen.getByRole("navigation", {
      name: "Testing Lab settings",
    });
    expect(navigation).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "General" })).toHaveAttribute(
      "href",
      "/console/community/testing-lab/settings/general",
    );
    expect(screen.getByRole("link", { name: "Locations" })).toHaveAttribute(
      "href",
      "/console/community/testing-lab/settings/locations",
    );
    expect(screen.getByRole("link", { name: "Access" })).toHaveAttribute(
      "href",
      "/console/community/testing-lab/settings/access",
    );
  });
});
