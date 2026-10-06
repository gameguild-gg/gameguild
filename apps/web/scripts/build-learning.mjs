#!/usr/bin/env node

import { spawn } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptPath = fileURLToPath(import.meta.url);
const webRoot = path.resolve(path.dirname(scriptPath), "..");
const nextBin = path.join(
  webRoot,
  "node_modules",
  "next",
  "dist",
  "bin",
  "next",
);
const shell = process.platform === "win32";
const nodeOptions = /--max-old-space-size(?:=|\s)/.test(
  process.env.NODE_OPTIONS ?? "",
)
  ? process.env.NODE_OPTIONS
  : `${process.env.NODE_OPTIONS ?? ""} --max-old-space-size=4096`.trim();
const buildEnv = {
  ...process.env,
  GAMEGUILD_PRECOMPILED_SCOPE: "learning",
  NODE_OPTIONS: nodeOptions,
};

export const LEARNING_BUILD_PATHS = [
  "src/app/[locale]/(public)/page.tsx",
  "src/app/[locale]/(auth)/**/page.tsx",
  "src/app/[locale]/(dashboards)/dashboard/**/page.tsx",
  "src/app/[locale]/(dashboards)/workspace/page.tsx",
  "src/app/[locale]/**/workspace/learning/**/page.tsx",
  "src/app/api/auth/**/route.ts",
  "src/app/api/health/route.ts",
  "src/app/api/assets/**/route.ts",
  "src/app/api/courses/**/route.ts",
  "src/app/api/learning/**/route.ts",
];

function run(command, args) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      cwd: webRoot,
      env: buildEnv,
      shell,
      stdio: "inherit",
    });
    child.once("error", reject);
    child.once("exit", (code, signal) => {
      if (code === 0) resolve();
      else {
        reject(
          new Error(
            `${command} failed (${signal ?? `exit ${code ?? "unknown"}`})`,
          ),
        );
      }
    });
  });
}

export async function buildLearning() {
  await run("pnpm", ["run", "build:emception-dependencies"]);
  await run("pnpm", ["run", "sync:emception"]);
  await run("pnpm", ["run", "sync:javascript-runtime"]);
  await run(process.execPath, [
    nextBin,
    "build",
    "--webpack",
    "--debug-build-paths",
    LEARNING_BUILD_PATHS.join(","),
  ]);
}

if (process.argv[1] && path.resolve(process.argv[1]) === scriptPath) {
  buildLearning().catch((error) => {
    console.error(
      `[build:learning] ${error instanceof Error ? error.message : String(error)}`,
    );
    process.exitCode = 1;
  });
}
