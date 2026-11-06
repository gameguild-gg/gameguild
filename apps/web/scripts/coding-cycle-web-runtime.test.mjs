import assert from "node:assert/strict";
import { spawn } from "node:child_process";
import { once } from "node:events";
import { test } from "node:test";
import { startCodingCycleWeb } from "./coding-cycle-web-runtime.mjs";

function fixture(buildExit = 0) {
  const calls = [];
  const children = [];
  const output = [];
  let buildClosed = false;
  const options = {
    cwd: process.cwd(),
    port: 3012,
    env: {
      ...(process.env.SystemRoot ? { SystemRoot: process.env.SystemRoot } : {}),
      NODE_ENV: "development",
      NODE_OPTIONS: "--max-old-space-size=6144",
      NEXT_PUBLIC_API_URL: "http://127.0.0.1:8180",
    },
    resolveProcess: async (args, { env }) => {
      calls.push({ args, env, buildClosed });
      const stage = args[0];
      return {
        command: process.execPath,
        args: ["-e", `process.stdout.write(JSON.stringify({stage:${JSON.stringify(stage)},mode:process.env.NODE_ENV,isolated:process.env.CODING_CYCLE_E2E,api:process.env.NEXT_PUBLIC_API_URL})); setTimeout(() => process.exit(${stage === "build" ? buildExit : 0}), 30);`],
      };
    },
    spawnProcess: (command, args, options) => {
      assert.equal(options.shell, false);
      return spawn(command, args, options);
    },
    registerChild: (child, label) => {
      children.push({ child, label });
      let text = "";
      child.stdout.on("data", (data) => { text += data; });
      child.once("close", () => {
        if (label === "web-build") buildClosed = true;
        if (text) output.push(JSON.parse(text));
      });
    },
  };
  return { options, calls, children, output };
}

test("the compiler exits before the production server starts with the same isolated configuration", async () => {
  const f = fixture();
  const server = await startCodingCycleWeb(f.options);
  await once(server, "close");
  assert.deepEqual(f.calls.map((call) => call.args), [
    ["build", "--webpack"],
    ["start", "--port", "3012"],
  ]);
  assert.equal(f.calls[1].buildClosed, true);
  const { NODE_OPTIONS: buildHeap, ...buildConfig } = f.calls[0].env;
  const { NODE_OPTIONS: serverHeap, ...serverConfig } = f.calls[1].env;
  assert.equal(buildHeap, "--max-old-space-size=12288");
  assert.equal(serverHeap, "--max-old-space-size=6144");
  assert.deepEqual(buildConfig, serverConfig);
  assert.deepEqual(f.children.map(({ label }) => label), ["web-build", "web"]);
  assert.deepEqual(f.output, ["build", "start"].map((stage) => ({
    stage, mode: "production", isolated: "1", api: "http://127.0.0.1:8180",
  })));
});

test("a failed real build process prevents server startup", async () => {
  const f = fixture(7);
  await assert.rejects(startCodingCycleWeb(f.options), /Web build failed \(exit 7/);
  assert.equal(f.calls.length, 1);
  assert.equal(f.children.length, 1);
  assert.equal(f.children[0].child.exitCode, 7);
});

test("a build executable failure propagates before server startup", async () => {
  const f = fixture();
  f.options.resolveProcess = async (args) => {
    f.calls.push(args);
    return { command: `${process.execPath}.missing-coding-cycle`, args: [] };
  };
  await assert.rejects(startCodingCycleWeb(f.options), { code: "ENOENT" });
  assert.equal(f.calls.length, 1);
});

test("a server executable failure propagates after a successful build", async () => {
  const f = fixture();
  const resolveBuild = f.options.resolveProcess;
  f.options.resolveProcess = async (args, options) => args[0] === "build"
    ? resolveBuild(args, options)
    : { command: `${process.execPath}.missing-coding-server`, args: [] };
  await assert.rejects(startCodingCycleWeb(f.options), { code: "ENOENT" });
  assert.equal(f.children.length, 2);
  assert.equal(f.children[0].child.exitCode, 0);
});
