#!/usr/bin/env node
/**
 * E2E stack supervisor — guarantees the full stack before tests/recordings.
 *
 * 1. docker compose up -d --wait   (databases must be up + healthy; idempotent)
 * 2. if API /health and web already serving -> nothing to start
 * 3. else spawn `pnpm run dev` and wait for both to become healthy
 * 4. caller decides teardown via the returned stop(); SIGTERM the pnpm dev
 *    tree — scripts/dev.mjs kills its own children (it owns API/web/watchers).
 *
 * Used by scripts/e2e-global-setup.mjs (playwright) and scripts/e2e-record.mjs.
 */
import { spawn, spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import path from "node:path";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const WEB_URL = (process.env.PLAYWRIGHT_WEB_BASE_URL ?? "http://localhost:3000").replace(/\/$/, "");
const API_HEALTH_URL = process.env.E2E_API_HEALTH_URL ?? "http://localhost:8080/health";
const BOOT_TIMEOUT_MS = 600_000;

async function healthy(url, okStatus) {
  try {
    const res = await fetch(url, { method: "GET", redirect: "manual" });
    return okStatus(res.status);
  } catch {
    return false;
  }
}

const webUp = () => healthy(WEB_URL, (s) => s < 500);
const apiUp = () => healthy(API_HEALTH_URL, (s) => s >= 200 && s < 300);
export const stackUp = async () => (await webUp()) && (await apiUp());

async function waitUntil(fn, what, timeoutMs) {
  const deadline = Date.now() + timeoutMs;
  while (!(await fn())) {
    if (Date.now() > deadline) {
      throw new Error(`[e2e-stack] timed out waiting for ${what} (${timeoutMs / 1000}s)`);
    }
    await new Promise((r) => setTimeout(r, 2000));
  }
}

export async function startStackSupervisor() {
  const compose = spawnSync("docker", ["compose", "-f", "compose.yaml", "up", "-d", "--wait"], {
    cwd: root,
    stdio: "inherit",
  });
  if (compose.status !== 0) {
    throw new Error(`[e2e-stack] databases (docker compose) failed (exit ${compose.status})`);
  }

  let dev = null;
  if (!(await stackUp())) {
    console.log("[e2e-stack] starting dev stack (pnpm dev)...");
    dev = spawn("pnpm", ["run", "dev"], {
      cwd: root,
      stdio: "inherit",
      detached: process.platform !== "win32",
    });
    await waitUntil(stackUp, `web ${WEB_URL} + api ${API_HEALTH_URL}`, BOOT_TIMEOUT_MS);
  }

  console.log("[e2e-stack] ready: databases + api + web healthy");
  return () => {
    if (dev && dev.exitCode === null) {
      try {
        dev.kill("SIGTERM");
      } catch {
        /* already gone */
      }
    }
  };
}

// CLI mode: run as a long-lived process until SIGTERM/SIGINT.
if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const stop = await startStackSupervisor();
  const shutdown = () => {
    stop();
    process.exit(0);
  };
  process.on("SIGTERM", shutdown);
  process.on("SIGINT", shutdown);
  setInterval(() => {}, 1 << 30);
}
