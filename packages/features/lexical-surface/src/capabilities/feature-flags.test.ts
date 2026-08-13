import { describe, expect, it } from "vitest";
import { resolveLexicalSurfaceFeatures } from "./feature-flags";

const catalogFeatures = [
  "equation",
  "excalidraw",
  "table",
  "layout",
  "collapsible",
  "sticky",
  "admonition",
  "button",
  "divider",
  "mermaid",
  "vegaLite",
  "media",
] as const;

describe("resolveLexicalSurfaceFeatures", () => {
  it.each([
    { insertMenu: false, picker: true },
    { insertMenu: true, picker: false },
  ])(
    "keeps catalog plugins when one insertion channel is enabled",
    (channels) => {
      const features = resolveLexicalSurfaceFeatures(channels, false);

      for (const feature of catalogFeatures) {
        expect(features[feature]).toBe(true);
      }
    },
  );

  it("disables catalog plugins when both insertion channels are disabled", () => {
    const features = resolveLexicalSurfaceFeatures(
      { insertMenu: false, picker: false },
      false,
    );

    for (const feature of catalogFeatures) {
      expect(features[feature]).toBe(false);
    }
    expect(features.list).toBe(true);
    expect(features.link).toBe(true);
    expect(features.emoji).toBe(true);
    expect(features.autoEmbed).toBe(true);
  });

  it("still honors individual feature flags while one channel is enabled", () => {
    const features = resolveLexicalSurfaceFeatures(
      { insertMenu: false, picker: true, mermaid: false },
      false,
    );

    expect(features.mermaid).toBe(false);
    expect(features.table).toBe(true);
  });
});
