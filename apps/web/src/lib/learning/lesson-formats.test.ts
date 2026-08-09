import { describe, expect, it } from "vitest";

import { LESSON_FORMATS } from "./lesson-formats";

describe("LESSON_FORMATS", () => {
  it("exposes only formats supported by the generated API contract", () => {
    expect(LESSON_FORMATS.map(({ value }) => value)).toEqual([
      "Markdown",
      "Lexical",
      "RevealJs",
      "Video",
    ]);
  });
});
