import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { EventEmitter, once } from "node:events";
import { readFile } from "node:fs/promises";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import vm from "node:vm";

import { resolvePnpmProcess } from "../../../apps/web/scripts/pnpm-process.mjs";

const root = fileURLToPath(new URL("../../..", import.meta.url));
const nodePath = "C:\\Program Files\\nodejs\\node.exe";
const pnpmEntry = "C:\\owned path & literal\\pnpm.cjs";
const environment = { npm_execpath: pnpmEntry, OWNED_MARKER: "%literal% & value" };
const literalArguments = ["plain", "path with spaces", "owned & value", "%OWNED_MARKER%", "owned^literal", "[locale]/(auth)"];

function sourceFunction(source, name) {
  const start = source.indexOf(`function ${name}(`);
  assert.notEqual(start, -1);
  const end = source.indexOf("\n}", start);
  assert.notEqual(end, -1);
  return source.slice(start, end + 2);
}

function context(platform, status = 0) {
  const calls = [];
  const child = { status, exitCode: null, pid: 123456789 };
  const sandbox = {
    process: {
      platform,
      execPath: nodePath,
      env: environment,
      exit(code) { throw new Error(`owned exit ${code}`); },
      kill(pid, signal) { calls.push({ pid, signal }); },
    },
    shell: platform === "win32",
    spawn(command, args, options) {
      calls.push({ command, args: [...args], options });
      return child;
    },
    spawnSync(command, args, options) {
      calls.push({ command, args: Array.isArray(args) ? [...args] : args, options });
      return child;
    },
    resolvePnpmProcess(args, options) {
      return resolvePnpmProcess(args, {
        ...options, platform, nodePath,
        fileExists: async (candidate) => candidate === pnpmEntry,
      });
    },
  };
  return { calls, child, sandbox };
}

async function initializedRun(source, sandbox) {
  const start = source.indexOf("const pnpmProcess = ");
  const end = source.indexOf(";", start);
  const initialization = start === -1 ? "" : source.slice(start, end + 1);
  return await vm.runInNewContext(`(async () => {
    ${initialization}
    return (${sourceFunction(source, "run")});
  })()`, sandbox);
}

for (const platform of ["win32", "linux"]) {
  for (const argument of literalArguments) {
    test(`dev preserves ${platform} literal argument ${argument}`, async () => {
      const source = await readFile(path.join(root, "scripts/dev.mjs"), "utf8");
      const { calls, child, sandbox } = context(platform);
      const run = await initializedRun(source, sandbox);
      assert.equal(run("docker", ["compose", argument]), child);
      assert.equal(calls[0].command, "docker");
      assert.deepEqual(calls[0].args, ["compose", argument]);
      assert.equal(calls[0].options.shell, false);
      assert.equal(calls[0].options.detached, platform !== "win32");
      assert.equal(calls[0].options.stdio, "inherit");
    });
  }

  test(`dev resolves ${platform} pnpm without parsing arguments`, async () => {
    const source = await readFile(path.join(root, "scripts/dev.mjs"), "utf8");
    const { calls, sandbox } = context(platform);
    const run = await initializedRun(source, sandbox);
    run("pnpm", literalArguments);
    assert.equal(calls[0].command, platform === "win32" ? nodePath : "pnpm");
    assert.deepEqual(calls[0].args, platform === "win32" ? [pnpmEntry, ...literalArguments] : literalArguments);
    assert.equal(calls[0].options.shell, false);
  });
}

test("dev Windows taskkill uses literal PID arguments without a shell", async () => {
  const source = await readFile(path.join(root, "scripts/dev.mjs"), "utf8");
  const { calls, child, sandbox } = context("win32");
  const killTree = vm.runInNewContext(`(${sourceFunction(source, "killTree")})`, sandbox);
  killTree(child);
  assert.equal(calls[0].command, "taskkill");
  assert.deepEqual(calls[0].args, ["/pid", "123456789", "/T", "/F"]);
  assert.equal(calls[0].options.shell, false);
});

test("dev skips exited children and preserves POSIX group termination", async () => {
  const source = await readFile(path.join(root, "scripts/dev.mjs"), "utf8");
  const { calls, child, sandbox } = context("linux");
  const killTree = vm.runInNewContext(`(${sourceFunction(source, "killTree")})`, sandbox);
  killTree({ ...child, exitCode: 0 });
  assert.equal(calls.length, 0);
  killTree(child);
  assert.deepEqual(calls, [{ pid: -123456789, signal: "SIGTERM" }]);
});

test("dev fails before spawning when Windows pnpm cannot be resolved", async () => {
  const source = await readFile(path.join(root, "scripts/dev.mjs"), "utf8");
  const { calls, sandbox } = context("win32");
  sandbox.resolvePnpmProcess = async () => { throw new Error("owned missing pnpm"); };
  const start = source.indexOf("const pnpmProcess = ");
  assert.notEqual(start, -1);
  assert(start < source.indexOf("const compose = run("));
  await assert.rejects(async () => {
    const run = await initializedRun(source, sandbox);
    run("docker", ["compose"]);
  }, /owned missing pnpm/);
  assert.equal(calls.length, 0);
});

for (const platform of ["win32", "linux"]) {
  test(`library builder preserves ${platform} target argument vectors`, async () => {
    const source = await readFile(path.join(root, "tools/emception/scripts/ensure-emception-libs.mjs"), "utf8");
    const { calls, sandbox } = context(platform);
    const targetsStart = source.indexOf("const targets = [");
    const targetsEnd = source.indexOf("\n];", targetsStart);
    assert.notEqual(targetsStart, -1);
    assert.notEqual(targetsEnd, -1);
    sandbox.join = path.join;
    sandbox.emceptionRoot = "/owned emception & literal";
    const targets = vm.runInNewContext(`${source.slice(targetsStart, targetsEnd + 3)}\n targets`, sandbox);
    assert.equal(targets.length, 5);
    const callStart = source.indexOf("const result = spawnSync(");
    const invocationStart = source.lastIndexOf("const invocation = ", callStart);
    const callEnd = source.indexOf("\n        if (result.status", callStart);
    assert.notEqual(callStart, -1);
    assert.notEqual(callEnd, -1);
    for (const [index, target] of targets.entries()) {
      await vm.runInNewContext(`(async () => { ${source.slice(invocationStart === -1 ? callStart : invocationStart, callEnd)}\n return result; })()`, { ...sandbox, target });
      const call = calls[index];
      const args = ["run", index === 0 ? "build:lib" : "build"];
      assert.equal(call.command, platform === "win32" ? nodePath : "pnpm");
      assert.deepEqual(call.args, platform === "win32" ? [pnpmEntry, ...args] : args);
      assert.equal(call.options.cwd, target.cwd);
      assert.equal(call.options.stdio, "inherit");
      assert.equal(call.options.shell, false);
    }
  });

  for (const status of [0, 7, null]) {
    test(`dependency builder retains ${platform} status ${status}`, async () => {
      const source = await readFile(path.join(root, "apps/web/scripts/build-emception-dependencies.mjs"), "utf8");
      const { calls, sandbox } = context(platform, status);
      const run = await initializedRun(source, sandbox);
      const cwd = "/owned build & literal";
      if (status === 0) run(literalArguments, cwd);
      else assert.throws(() => run(literalArguments, cwd), new RegExp(`owned exit ${status ?? 1}`));
      assert.equal(calls[0].command, platform === "win32" ? nodePath : "pnpm");
      assert.deepEqual(calls[0].args, platform === "win32" ? [pnpmEntry, ...literalArguments] : literalArguments);
      assert.equal(calls[0].options.cwd, cwd);
      assert.equal(calls[0].options.stdio, "inherit");
      assert.equal(calls[0].options.shell, false);
    });
  }

  test(`coding cycle preserves ${platform} web process contract`, async () => {
    const source = await readFile(path.join(root, "apps/web/scripts/coding-cycle-browser-e2e.mjs"), "utf8");
    const runtime = await readFile(path.join(root, "apps/web/scripts/coding-cycle-web-runtime.mjs"), "utf8");
    const runtimeModule = runtime.replace(/^import [^\n]+\n/gm, "").replace(/^export /gm, "");
    const { calls, sandbox } = context(platform);
    const webStart = source.indexOf("  const web = await startCodingCycleWeb({");
    const end = source.indexOf("\n  await waitForHttp", webStart);
    assert.notEqual(webStart, -1);
    assert.notEqual(end, -1);
    const nextEntry = "/owned next & literal/dist/bin/next";
    const logs = [];
    sandbox.join = path.join;
    sandbox.once = once;
    sandbox.createRequire = (manifest) => {
      assert.equal(manifest, path.join(sandbox.WEB_DIR, "package.json"));
      return { resolve(specifier) {
        assert.equal(specifier, "next/dist/bin/next");
        return nextEntry;
      } };
    };
    sandbox.spawn = (command, args, options) => {
      const child = new EventEmitter();
      calls.push({ command, args: [...args], options, child });
      setImmediate(() => {
        child.emit("spawn");
        if (args[1] === "build") setImmediate(() => child.emit("close", 0, null));
      });
      return child;
    };
    sandbox.WEB_PORT = 3210;
    sandbox.WEB_DIR = "/owned web & literal";
    sandbox.webEnv = { OWNED_WEB: "literal & environment" };
    sandbox.envArrayToObject = (value) => value;
    sandbox.children = [];
    sandbox.WEB_BUILD_LOG = "/owned web-build.log";
    sandbox.WEB_LOG = "/owned web.log";
    sandbox.pipeLog = (child, file) => logs.push({ child, file });
    const web = await vm.runInNewContext(`(async () => {
      ${runtimeModule}
      ${source.slice(webStart, end)}
      return web;
    })()`, sandbox);
    assert.equal(calls.length, 2);
    assert.equal(web, calls[1].child);
    assert.deepEqual(calls.map((call) => call.args), [
      [nextEntry, "build", "--webpack"],
      [nextEntry, "start", "--port", "3210"],
    ]);
    for (const call of calls) {
      assert.equal(call.command, nodePath);
      assert.equal(call.options.cwd, sandbox.WEB_DIR);
      assert.equal(call.options.env.OWNED_MARKER, environment.OWNED_MARKER);
      assert.equal(call.options.env.OWNED_WEB, sandbox.webEnv.OWNED_WEB);
      assert.equal(call.options.env.NODE_ENV, "production");
      assert.equal(call.options.env.CODING_CYCLE_E2E, "1");
      assert.equal(call.options.detached, platform !== "win32");
      assert.deepEqual([...call.options.stdio], ["ignore", "pipe", "pipe"]);
      assert.equal(call.options.shell, false);
      assert.equal(call.options.windowsHide, true);
    }
    assert.deepEqual(Array.from(sandbox.children, ({ label }) => label), ["web-build", "web"]);
    assert.equal(sandbox.children[0].proc, calls[0].child);
    assert.equal(sandbox.children[1].proc, web);
    assert.deepEqual(logs, [
      { child: calls[0].child, file: sandbox.WEB_BUILD_LOG },
      { child: web, file: sandbox.WEB_LOG },
    ]);
  });
}


for (const platform of ["win32", "linux"]) {
  test(`coding cycle gives the ${platform} web worker a bounded heap independent of its parent`, async () => {
    const source = await readFile(path.join(root, "apps/web/scripts/coding-cycle-browser-e2e.mjs"), "utf8");
    const { calls, sandbox } = context(platform);
    sandbox.process.env = { ...environment, NODE_OPTIONS: "--no-warnings --max-old-space-size=128" };
    sandbox.API_BASE = "http://127.0.0.1:48123";
    sandbox.WEB_BASE = "http://localhost:48124";
    sandbox.WEB_PORT = 48124;
    sandbox.WEB_DIR = "/owned memory validation";
    sandbox.log = () => {};
    sandbox.startCodingCycleWeb = async (options) => { calls.push({ options }); return {}; };
    sandbox.envArrayToObject = vm.runInNewContext(`(${sourceFunction(source, "envArrayToObject")})`, sandbox);
    const start = source.indexOf("  const webEnv = [");
    const end = source.indexOf("\n  await waitForHttp", start);
    assert.notEqual(start, -1);
    assert.notEqual(end, -1);
    await vm.runInNewContext(`(async () => { ${source.slice(start, end)} })()`, sandbox);
    assert.equal(calls.length, 1);
    assert.equal(calls[0].options.env.NODE_ENV, "production");
    assert.equal(calls[0].options.env.GAMEGUILD_DISABLE_WEBPACK_CACHE, "1");
    const heap = spawnSync(process.execPath, ["-e", "process.emitWarning('coding-cycle NODE_OPTIONS probe'); console.log(require('node:v8').getHeapStatistics().heap_size_limit)"], {
      encoding: "utf8",
      env: { ...process.env, ...calls[0].options.env },
      timeout: 10_000,
      windowsHide: true,
    });
    assert.equal(heap.status, 0, heap.stderr);
    assert.equal(heap.stderr, "", "the inherited --no-warnings option must remain active");
    const heapBytes = Number(heap.stdout.trim());
    assert.ok(heapBytes >= 6144 * 1024 * 1024, `worker heap was only ${heapBytes} bytes`);
    assert.ok(heapBytes <= 6400 * 1024 * 1024, `worker heap was not bounded: ${heapBytes} bytes`);
  });
}


for (const [mode, codingCycle, expected] of [
  ["development", "1", false],
  ["development", undefined, undefined],
  ["development", "0", undefined],
  ["production", "1", undefined],
]) {
  test(`coding-cycle fixed worker policy for ${mode}/${codingCycle ?? "unset"}`, async () => {
    const source = await readFile(path.join(root, "apps/web/next.config.ts"), "utf8");
    const start = source.indexOf("  experimental: {");
    const end = source.indexOf("\n  },", start);
    assert.notEqual(start, -1);
    assert.notEqual(end, -1);
    const env = { NODE_ENV: mode };
    if (codingCycle !== undefined) env.CODING_CYCLE_E2E = codingCycle;
    const config = vm.runInNewContext(`({ ${source.slice(start, end + 5)} })`, {
      process: { env },
    });
    assert.equal(config.experimental.devMemoryThresholdRestart, expected);
    assert.equal(config.experimental.authInterrupts, true);
    assert.equal(config.experimental.cpus, 1);
  });
}
