import { render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/i18n/navigation", () => ({
  usePathname: () => "/dashboard/testing-lab/settings/locations",
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

describe("TestingLabSettingsLayout", () => {
  it("groups general, location, and access settings without horizontal overflow", () => {
    render(
      <TestingLabSettingsLayout>
        <div>Settings content</div>
      </TestingLabSettingsLayout>,
    );

    const navigation = screen.getByRole("navigation", {
      name: "Testing Lab settings",
    });
    expect(navigation).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "General" })).toHaveAttribute(
      "href",
      "/dashboard/testing-lab/settings/general",
    );
    expect(screen.getByRole("link", { name: "Locations" })).toHaveAttribute(
      "href",
      "/dashboard/testing-lab/settings/locations",
    );
    expect(screen.getByRole("link", { name: "Access" })).toHaveAttribute(
      "href",
      "/dashboard/testing-lab/settings/access",
    );
  });
});
