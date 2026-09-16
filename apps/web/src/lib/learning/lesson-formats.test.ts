import { describe, expect, it } from "vitest";

import { getLessonFormatLabel, LESSON_FORMATS } from "./lesson-formats";

describe("LESSON_FORMATS", () => {
  it("exposes only formats supported by the generated API contract", () => {
    expect(LESSON_FORMATS.map(({ value }) => value)).toEqual([
      "Markdown",
      "Lexical",
      "RevealJs",
      "Video",
      "Html",
      "ExternalLink",
    ]);
  });

  it("uses the configured label and preserves unknown future formats", () => {
    expect(getLessonFormatLabel(null)).toBe("Markdown");
    expect(getLessonFormatLabel("Lexical")).toBe("Rich text (Lexical)");
    expect(getLessonFormatLabel("FutureFormat")).toBe("FutureFormat");
  });
});
