import { readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const packageRoot = join(dirname(fileURLToPath(import.meta.url)), "..");
const forbidden = [
  "@game-guild/quiz",
  "@game-guild/quiz-content",
  "@game-guild/quiz-surface",
  "@game-guild/grading-adapter-quiz",
];

describe("grading package boundaries", () => {
  it("does not depend on assessment-specific packages", () => {
    const packageJson = readFileSync(join(packageRoot, "package.json"), "utf8");
    const source = readSources(join(packageRoot, "src"));
    for (const dependency of forbidden) {
      expect(packageJson, dependency).not.toContain(`"${dependency}"`);
      expect(source, dependency).not.toContain(dependency);
    }
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
