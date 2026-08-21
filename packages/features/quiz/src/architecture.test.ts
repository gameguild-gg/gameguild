import { readdirSync, readFileSync } from "node:fs";
import { extname, join } from "node:path";
import { describe, expect, it } from "vitest";

describe("quiz package architecture", () => {
  it("does not depend on UI, grading, block, or application layers", () => {
    const source = readProductionSource(join(import.meta.dirname));
    expect(source).not.toMatch(/from ["']react(?:\/|["'])/);
    expect(source).not.toMatch(/from ["']lexical(?:\/|["'])/);
    expect(source).not.toContain("@game-guild/grading");
    expect(source).not.toContain("@game-guild/quiz-surface");
    expect(source).not.toContain("@game-guild/block-list");
    expect(source).not.toContain("apps/web");
  });
});

function readProductionSource(directory: string): string {
  return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) return readProductionSource(path);
    if (entry.name.endsWith(".test.ts") || extname(entry.name) !== ".ts") return [];
    return [readFileSync(path, "utf8")];
  }).join("\n");
}
