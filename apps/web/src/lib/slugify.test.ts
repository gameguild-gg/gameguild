import { describe, expect, it } from "vitest";
import { normalizeSlug, slugify } from "./slugify";

describe("slugify", () => {
  it("lowercases and hyphenates whitespace", () => {
    expect(slugify("Intro to Game Design")).toBe("intro-to-game-design");
  });

  it("converts underscores and dots to hyphens", () => {
    expect(slugify("week_01.notes")).toBe("week-01-notes");
  });

  it("drops characters outside [a-z0-9-]", () => {
    expect(slugify("Final EXAM!! (v2)")).toBe("final-exam-v2");
  });

  it("collapses consecutive hyphens", () => {
    expect(slugify("Multiple   Spaces")).toBe("multiple-spaces");
  });

  it("keeps edge hyphens so live typing does not fuse words", () => {
    expect(slugify("my ")).toBe("my-");
    expect(slugify("  Week 01  ")).toBe("-week-01-");
  });

  it("stays empty for input with no slug characters, including lone spaces", () => {
    expect(slugify("!!!")).toBe("");
    expect(slugify("   ")).toBe("");
    expect(slugify(" ")).toBe("");
  });

  it("keeps digits and hyphens", () => {
    expect(slugify("Module 3 - Advanced")).toBe("module-3-advanced");
  });
});

describe("normalizeSlug", () => {
  it("strips leading and trailing hyphens for submit-time values", () => {
    expect(normalizeSlug("my-")).toBe("my");
    expect(normalizeSlug("--week-01--")).toBe("week-01");
    expect(normalizeSlug("Week 01 ")).toBe("week-01");
  });

  it("is idempotent", () => {
    const once = normalizeSlug("  -- Multiple   Spaces --  ");
    expect(normalizeSlug(once)).toBe(once);
    expect(once).toBe("multiple-spaces");
  });

  it("returns an empty string when nothing remains", () => {
    expect(normalizeSlug("   ")).toBe("");
    expect(normalizeSlug("")).toBe("");
  });
});
