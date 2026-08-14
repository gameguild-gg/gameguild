import { describe, expect, it } from "vitest";
import { resolveLexicalSurfaceFeatures } from "./feature-flags";
import { getEnabledInsertions, INSERTION_CATALOG } from "./insertion-catalog";

describe("insertion catalog", () => {
  it("has stable, unique identifiers", () => {
    const ids = INSERTION_CATALOG.map((definition) => definition.id);
    expect(new Set(ids).size).toBe(ids.length);
  });

  it("declares valid feature keys and insertion surfaces", () => {
    const features = resolveLexicalSurfaceFeatures(undefined, false);

    for (const definition of INSERTION_CATALOG) {
      expect(definition.feature in features).toBe(true);
      expect(definition.surfaces.length).toBeGreaterThan(0);
      expect(definition.execute || definition.dialog).toBeTruthy();
    }
  });

  it("keeps toolbar and picker feature sets aligned", () => {
    const features = resolveLexicalSurfaceFeatures(undefined, false);
    const toolbar = getEnabledInsertions(features, "toolbar").map(
      ({ id }) => id,
    );
    const picker = getEnabledInsertions(features, "picker").map(({ id }) => id);

    expect(toolbar).toEqual(picker);
  });

  it("filters disabled document features", () => {
    const features = resolveLexicalSurfaceFeatures(
      { media: false, mermaid: false },
      false,
    );

    expect(
      getEnabledInsertions(features, "toolbar").map(({ id }) => id),
    ).not.toContain("media");
    expect(
      getEnabledInsertions(features, "picker").map(({ id }) => id),
    ).not.toContain("mermaid");
  });
});
