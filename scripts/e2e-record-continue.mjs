#!/usr/bin/env node
/**
 * Continue recording an existing spec: run it under --debug with the stack up.
 * Add `await page.pause();` where you want to resume, hit Record in Inspector.
 *
 * Usage: pnpm e2e:record:continue login        (resolves e2e/login.spec.ts)
 *        pnpm e2e:record:continue e2e/login.spec.ts
 */
import { spawnSync } from "node:child_process";
import { existsSync } from "node:fs";
import path from "node:path";

const input = process.argv[2];
if (!input) {
  console.error("usage: pnpm e2e:record:continue <test>   (e.g. login -> e2e/login.spec.ts)");
  process.exit(1);
}

const candidates = [
  input,
  input.endsWith(".spec.ts") ? input : `${input}.spec.ts`,
  path.join("e2e", input.endsWith(".spec.ts") ? input : `${input}.spec.ts`),
];
const target = candidates.find((c) => existsSync(c));
if (!target) {
  console.error(`no spec found for "${input}" (tried: ${candidates.join(", ")})`);
  process.exit(1);
}

const result = spawnSync("pnpm", ["exec", "playwright", "test", target, "--debug"], {
  stdio: "inherit",
  env: { ...process.env, E2E_RUN: "1" },
});
process.exit(result.status ?? 1);
