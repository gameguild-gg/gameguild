#!/usr/bin/env node

import { spawn } from "node:child_process";
import { readFileSync } from "node:fs";
import net from "node:net";

const API_PORT = 8080;
const WEB_PORT = 3000;
const API_HEALTH_URL = `http://localhost:${API_PORT}/health`;
const WEB_HEALTH_URL = `http://localhost:${WEB_PORT}/api/health`;
const shell = process.platform === "win32";
const children = new Set();

function loadEnv(file) {
  const env = {};
  for (const line of readFileSync(file, "utf8").split("\n")) {
    if (line.trim().startsWith("#")) continue;
    const match = line.match(/^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)\s*$/);
    if (!match) continue;
    const value = match[2].trim();
    env[match[1]] =
      value.startsWith('"') || value.startsWith("'")
        ? value.slice(1, -1)
        : value;
  }
  return env;
}

function limitNodeMemory(env) {
  if (/--max-old-space-size(?:=|\s)/.test(env.NODE_OPTIONS ?? "")) return env;
  return {
    ...env,
    NODE_OPTIONS: `${env.NODE_OPTIONS ?? ""} --max-old-space-size=4096`.trim(),
  };
}

const runtimeEnv = limitNodeMemory({
  ...process.env,
  ...loadEnv(".env"),
  GAMEGUILD_PRECOMPILED_SCOPE: "learning",
});

function run(command, args, options = {}) {
  const child = spawn(command, args, {
    shell,
    stdio: "inherit",
    detached: !shell,
    env: runtimeEnv,
    ...options,
  });
  children.add(child);
  child.once("exit", () => children.delete(child));
  return child;
}

function waitForExit(child, label) {
  return new Promise((resolve, reject) => {
    child.once("error", reject);
    child.once("exit", (code, signal) => {
      if (code === 0) resolve();
      else
        reject(
          new Error(
            `${label} failed (${signal ?? `exit ${code ?? "unknown"}`})`,
          ),
        );
    });
  });
}

async function runStep(label, command, args) {
  console.log(`[dev:learning] ${label}`);
  await waitForExit(run(command, args), label);
}

function killTree(child) {
  if (child.exitCode !== null) return;
  if (process.platform === "win32") {
    spawn("taskkill", ["/pid", String(child.pid), "/T", "/F"], {
      shell: true,
    });
    return;
  }
  try {
    process.kill(-child.pid, "SIGTERM");
  } catch {
    // The process group has already exited.
  }
}

function spawnReaper(processGroupId) {
  spawn(
    "sh",
    ["-c", `sleep 4; kill -9 -- -${processGroupId} 2>/dev/null; true`],
    { detached: true, stdio: "ignore" },
  ).unref();
}

function assertPortFree(port) {
  return new Promise((resolve, reject) => {
    const probe = net.createServer();
    probe.once("error", (error) => {
      if (error.code === "EADDRINUSE") {
        reject(
          new Error(
            `port ${port} is already in use; run pnpm run dev:stop before retrying`,
          ),
        );
        return;
      }
      reject(error);
    });
    probe.once("listening", () => probe.close(resolve));
    probe.listen(port, "localhost");
  });
}

function delay(milliseconds) {
  return new Promise((resolve) => setTimeout(resolve, milliseconds));
}

async function waitForHttp(url, label, child, timeoutMilliseconds) {
  const deadline = Date.now() + timeoutMilliseconds;
  let lastError = "not ready";

  while (Date.now() < deadline) {
    if (child.exitCode !== null) {
      throw new Error(`${label} exited before becoming ready`);
    }
    try {
      const response = await fetch(url, {
        redirect: "manual",
        signal: AbortSignal.timeout(5_000),
      });
      if (response.status >= 200 && response.status < 300) return;
      lastError = `HTTP ${response.status}`;
    } catch (error) {
      lastError = error instanceof Error ? error.message : String(error);
    }
    await delay(1_000);
  }

  throw new Error(`${label} did not become ready: ${lastError}`);
}

let shuttingDown = false;
function shutdown(exitCode = 0) {
  if (shuttingDown) return;
  shuttingDown = true;
  console.log("\n[dev:learning] shutting down...");
  const runningChildren = [...children];
  for (const child of runningChildren) killTree(child);
  if (process.platform !== "win32") {
    for (const child of runningChildren) {
      if (child.pid) spawnReaper(child.pid);
    }
  }
  setTimeout(() => process.exit(exitCode), 6_000);
}

const onSignal = () => (shuttingDown ? process.exit(0) : shutdown());
process.on("SIGINT", onSignal);
process.on("SIGTERM", onSignal);

async function main() {
  await runStep("starting Docker infrastructure", "docker", [
    "compose",
    "-f",
    "compose.yaml",
    "up",
    "-d",
    "--wait",
  ]);
  await Promise.all([assertPortFree(API_PORT), assertPortFree(WEB_PORT)]);

  console.log("[dev:learning] starting API without file watching");
  const api = run("dotnet", [
    "run",
    "--project",
    "apps/api/Source/GameGuild.API/GameGuild.API.csproj",
    "--urls",
    `http://localhost:${API_PORT}`,
  ]);
  await waitForHttp(API_HEALTH_URL, "API", api, 10 * 60 * 1000);

  await runStep("generating the API client", "pnpm", [
    "--filter",
    "@game-guild/client",
    "run",
    "generate",
  ]);
  await runStep("building the API client", "pnpm", [
    "--filter",
    "@game-guild/client",
    "run",
    "build",
  ]);
  await runStep("precompiling dashboard and Workspace Learning", "pnpm", [
    "--filter",
    "@game-guild/web",
    "run",
    "build:learning",
  ]);

  console.log("[dev:learning] starting the precompiled web server");
  const web = run("pnpm", [
    "--filter",
    "@game-guild/web",
    "run",
    "start:learning",
  ]);
  web.once("exit", (code) => shutdown(code ?? 1));
  await waitForHttp(WEB_HEALTH_URL, "web", web, 2 * 60 * 1000);

  console.log(
    `[dev:learning] ready at http://localhost:${WEB_PORT}/dashboard (precompiled, no HMR)`,
  );
}

main().catch((error) => {
  console.error(
    `[dev:learning] ${error instanceof Error ? error.message : String(error)}`,
  );
  shutdown(1);
});
