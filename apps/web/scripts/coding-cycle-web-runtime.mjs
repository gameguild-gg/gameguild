import { spawn } from "node:child_process";
import { once } from "node:events";
import { createRequire } from "node:module";
import { join } from "node:path";

function resolveNextProcess(args, { cwd }) {
  const require = createRequire(join(cwd, "package.json"));
  return { command: process.execPath, args: [require.resolve("next/dist/bin/next"), ...args] };
}

/**
 * Compile before starting the browser journey. A development server retains its
 * webpack compilations alongside the IDE and can restart between personas.
 * Build and serve the same isolated production output and public configuration.
 */
export async function startCodingCycleWeb({
  cwd,
  port,
  env,
  registerChild,
  spawnProcess = spawn,
  resolveProcess = resolveNextProcess,
}) {
  const runtimeEnv = { ...env, NODE_ENV: "production", CODING_CYCLE_E2E: "1" };
  async function launch(args, label, processEnv = runtimeEnv) {
    const invocation = await resolveProcess(args, { env: processEnv, cwd });
    const child = spawnProcess(invocation.command, invocation.args, {
      cwd,
      env: processEnv,
      detached: process.platform !== "win32",
      shell: false,
      windowsHide: true,
      stdio: ["ignore", "pipe", "pipe"],
    });
    const started = once(child, "spawn");
    registerChild(child, label);
    await started;
    return child;
  }

  // Match the verified Web build budget. The compiler finishes before either
  // browser persona starts, so its heap does not compete with their WASM heaps.
  const build = await launch(["build", "--webpack"], "web-build", {
    ...runtimeEnv,
    NODE_OPTIONS: "--max-old-space-size=12288",
  });
  await new Promise((resolve, reject) => {
    build.once("error", reject);
    build.once("close", (code, signal) => {
      if (code === 0) resolve();
      else reject(new Error(`Coding-cycle Web build failed (exit ${code}, signal ${signal ?? "none"}).`));
    });
  });

  return launch(["start", "--port", String(port)], "web");
}
