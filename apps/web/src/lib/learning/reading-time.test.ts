import { describe, expect, it } from "vitest";

import { estimateReadingMinutes } from "./reading-time";

describe("estimateReadingMinutes", () => {
  it("returns 1 for a short markdown body", () => {
    expect(estimateReadingMinutes({ body: "Hello world foo bar baz" })).toBe(1);
  });

  it("returns 2 for a 400-word markdown body", () => {
    expect(estimateReadingMinutes({ body: new Array(400).fill("word").join(" ") })).toBe(2);
  });

  it("splits on any whitespace, not just spaces", () => {
    expect(estimateReadingMinutes({ body: "line one\nline two\nline three" })).toBe(1);
  });

  it("walks a Lexical jsonBody and recurses through nested children", () => {
    const jsonBody = {
      root: {
        children: [{ text: "a b c" }, { children: [{ text: "d e" }] }],
      },
    };
    expect(estimateReadingMinutes({ jsonBody })).toBe(1);
  });

  it("counts allowlisted quiz fields across nested arrays", () => {
    const jsonBody = {
      questions: [{ question: "what is 1 plus 1", choices: [{ option: "two" }] }],
    };
    expect(estimateReadingMinutes({ jsonBody })).toBe(1);
  });

  it("returns null for video lessons regardless of body", () => {
    expect(
      estimateReadingMinutes({ lessonFormat: "Video", body: "https://example.com/video" }),
    ).toBeNull();
  });

  it("returns null for empty, whitespace-only, and explicit null input", () => {
    expect(estimateReadingMinutes({})).toBeNull();
    expect(estimateReadingMinutes({ body: "   " })).toBeNull();
    expect(estimateReadingMinutes({ body: null, jsonBody: null })).toBeNull();
  });

  it("strips HTML tags before counting words", () => {
    expect(estimateReadingMinutes({ body: "<p>Hello <strong>world</strong></p>" })).toBe(1);
  });

  it("matches allowlisted keys case-insensitively", () => {
    expect(estimateReadingMinutes({ jsonBody: { Question: "a b", TITLE: "c d" } })).toBe(1);
  });

  it("skips non-allowlisted string values such as urls and types", () => {
    expect(
      estimateReadingMinutes({
        jsonBody: { url: "https://example.com/a/b/c/d/e", type: "paragraph" },
      }),
    ).toBeNull();
  });
});
