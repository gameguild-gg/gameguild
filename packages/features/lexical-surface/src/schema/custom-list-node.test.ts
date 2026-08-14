import { describe, expect, it } from "vitest";
import {
  normalizeListStyleType,
  normalizeMarkerColor,
} from "./custom-list-node";

describe("custom list persisted styles", () => {
  it.each(["#fff", "#abcd", "#123456", "#12345678"])(
    "accepts the CSS color %s",
    (color) => {
      expect(normalizeMarkerColor(color)).toBe(color);
    },
  );

  it.each([
    "red",
    "var(--host-color)",
    "#12345",
    "#1234567",
    "#fff; } body { display:none",
  ])("rejects the unsafe or unsupported color %s", (color) => {
    expect(normalizeMarkerColor(color)).toBe("#3b82f6");
  });

  it("falls back to a style appropriate for the list type", () => {
    expect(normalizeListStyleType("unknown", "bullet")).toBe("disc");
    expect(normalizeListStyleType("unknown", "number")).toBe("decimal");
    expect(normalizeListStyleType("upper-roman", "number")).toBe("upper-roman");
  });
});
