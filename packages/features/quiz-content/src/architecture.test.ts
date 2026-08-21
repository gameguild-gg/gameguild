import { readdirSync, readFileSync } from "node:fs";
import { extname, join } from "node:path";
import { describe, expect, it } from "vitest";

describe("quiz-content package architecture", () => {
  it("does not depend on React, Next.js, surfaces, or application code", () => {
    const source = readProductionSource(import.meta.dirname);
    expect(source).not.toMatch(/from ["']react(?:\/|["'])/);
    expect(source).not.toContain("next/");
    expect(source).not.toContain("@game-guild/quiz-surface");
    expect(source).not.toContain("block-content-editor");
    expect(source).not.toContain("apps/web");
    expect(source).not.toMatch(/from ["']@\//);
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
