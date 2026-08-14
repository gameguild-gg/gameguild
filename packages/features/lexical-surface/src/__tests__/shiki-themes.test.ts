import { bundledLanguages, bundledThemes } from "shiki";
import { describe, expect, it } from "vitest";
import {
  getShikiThemeName,
  SHIKI_THEME_CONFIGS,
  SHIKI_THEME_KEYS,
  SHIKI_THEME_NAMES,
} from "../shared/monaco/shiki-themes";

describe("Shiki theme catalog", () => {
  it("only references themes included by the installed Shiki version", () => {
    expect(
      SHIKI_THEME_NAMES.filter((theme) => !(theme in bundledThemes)),
    ).toEqual([]);
  });

  it("includes every language required by package Monaco editors", () => {
    expect("json" in bundledLanguages).toBe(true);
    expect("mermaid" in bundledLanguages).toBe(true);
  });

  it("resolves light and dark variants from every catalog entry", () => {
    for (const theme of SHIKI_THEME_KEYS) {
      expect(getShikiThemeName(theme, false)).toBe(
        SHIKI_THEME_CONFIGS[theme].light,
      );
      expect(getShikiThemeName(theme, true)).toBe(
        SHIKI_THEME_CONFIGS[theme].dark,
      );
    }
  });
});
