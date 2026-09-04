import { readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const packageRoot = join(dirname(fileURLToPath(import.meta.url)), "..");

describe("quiz grading adapter boundaries", () => {
  it("depends only on public grading and quiz entrypoints", () => {
    const source = readSources(join(packageRoot, "src"));
    expect(source).not.toMatch(/@game-guild\/(?:grading|quiz)\//);
    expect(source).not.toContain("@game-guild/block-list");
    expect(source).not.toContain("@game-guild/quiz-content");
    expect(source).not.toContain("QuizBlockLike");
    expect(source).not.toContain("QuizBlockStorageLike");
  });
});

function readSources(directory: string): string {
  return readdirSync(directory)
    .map((entry) => join(directory, entry))
    .flatMap((entry) => statSync(entry).isDirectory()
      ? [readSources(entry)]
      : entry.endsWith(".ts") && !entry.endsWith(".test.ts")
        ? [readFileSync(entry, "utf8")]
        : [])
    .join("\n");
}
