import { render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  pathname: vi.fn<() => string | null>(),
}));

vi.mock("@/i18n/navigation", () => ({
  usePathname: mocks.pathname,
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

import { TestingLabSettingsNav } from "./testing-lab-settings-nav";

describe("TestingLabSettingsNav", () => {
  beforeEach(() => {
    mocks.pathname.mockReturnValue("/workspace/testing-lab/settings/locations");
  });

  it("marks the current settings section for visual and assistive navigation", () => {
    render(<TestingLabSettingsNav />);

    expect(screen.getByRole("link", { name: "Locations" })).toHaveAttribute(
      "aria-current",
      "page",
    );
    expect(screen.getByRole("link", { name: "Analytics" })).toHaveAttribute(
      "href",
      "/workspace/testing-lab/settings/analytics",
    );
    expect(screen.getByRole("link", { name: "Calendars" })).toHaveAttribute(
      "href",
      "/workspace/testing-lab/settings/templates",
    );
    expect(screen.getByRole("link", { name: "General" })).not.toHaveAttribute(
      "aria-current",
    );
  });

  it("shows only settings sections the current actor can access", () => {
    render(
      <TestingLabSettingsNav capabilities={["TestingLab.ViewAnalytics"]} />,
    );

    expect(screen.getByRole("link", { name: "Analytics" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "General" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Locations" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Access" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Calendars" })).not.toBeInTheDocument();
  });

  it("marks nested settings routes as current", () => {
    mocks.pathname.mockReturnValue(
      "/workspace/testing-lab/settings/templates/template-1",
    );
    render(<TestingLabSettingsNav />);
    expect(screen.getByRole("link", { name: "Calendars" })).toHaveAttribute(
      "aria-current",
      "page",
    );
  });

  it("handles a missing pathname without marking a section current", () => {
    mocks.pathname.mockReturnValue(null);
    render(<TestingLabSettingsNav capabilities={[]} />);
    expect(
      screen.queryByRole("link", { name: "General" }),
    ).not.toBeInTheDocument();
  });
});
