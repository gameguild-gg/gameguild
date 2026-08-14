import { describe, expect, it } from "vitest";
import {
  DEFAULT_MONACO_PREFERENCES,
  normalizeMonacoPreferences,
} from "../shared/ui/editor-preferences";

describe("editor preferences", () => {
  it("fills missing Monaco options with package defaults", () => {
    expect(normalizeMonacoPreferences({})).toEqual(DEFAULT_MONACO_PREFERENCES);
  });

  it("normalizes values persisted by the legacy host editor", () => {
    expect(
      normalizeMonacoPreferences({
        fontSize: 48,
        tabSize: 3,
        renderLineHighlight: "rectangle",
      }),
    ).toMatchObject({
      shikiTheme: "github",
      fontSize: 24,
      tabSize: 2,
      renderLineHighlight: "line",
    });
  });
});
