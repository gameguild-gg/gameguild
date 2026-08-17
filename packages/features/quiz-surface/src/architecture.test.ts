import { readdirSync, readFileSync } from "node:fs";
import { extname, join } from "node:path";
import { describe, expect, it } from "vitest";

describe("quiz surface architecture", () => {
  it("does not import grading or application internals", () => {
    const source = readProductionSource(import.meta.dirname);
    expect(source).not.toContain("@game-guild/grading");
    expect(source).not.toMatch(/from ["']@\//);
    expect(source).not.toContain("apps/web");
    expect(source).not.toContain("block-content-editor");
  });
});

function readProductionSource(directory: string): string {
  return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) return readProductionSource(path);
    if (entry.name.endsWith(".test.ts") || ![".ts", ".tsx"].includes(extname(entry.name))) return [];
    return [readFileSync(path, "utf8")];
  }).join("\n");
}
