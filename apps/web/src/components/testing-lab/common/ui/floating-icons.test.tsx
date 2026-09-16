import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { FloatingIcons } from "./floating-icons";

describe("FloatingIcons", () => {
  it("renders a decorative, assistive-technology-hidden background", () => {
    const { container } = render(<FloatingIcons />);
    const background = container.firstElementChild;

    expect(background).toHaveAttribute("aria-hidden", "true");
    expect(background).toHaveClass("pointer-events-none", "overflow-hidden");
    expect(background?.querySelectorAll("svg")).toHaveLength(7);
    expect(background?.lastElementChild).toHaveClass("bg-radial");
  });
});
