import assert from "node:assert/strict";
import { existsSync, readdirSync, realpathSync } from "node:fs";
import { createRequire } from "node:module";
import { dirname, join, resolve } from "node:path";
import { spawnSync } from "node:child_process";
import { test } from "node:test";
import { fileURLToPath } from "node:url";

const repositoryRoot = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const virtualStore = join(repositoryRoot, "node_modules/.pnpm");
const require = createRequire(import.meta.url);
const packageRoots = new Set();

for (const entry of readdirSync(virtualStore, { withFileTypes: true })) {
  if (!entry.isDirectory() || entry.name.startsWith("sprintf-js@")) continue;
  const candidate = join(virtualStore, entry.name, "node_modules/sprintf-js");
  if (existsSync(candidate)) packageRoots.add(realpathSync(candidate));
}
for (const candidate of [
  join(virtualStore, "node_modules/sprintf-js"),
  join(repositoryRoot, "node_modules/sprintf-js"),
]) {
  if (existsSync(candidate)) packageRoots.add(realpathSync(candidate));
}
assert.ok(packageRoots.size > 0, "Installed consumers must resolve sprintf-js");

for (const packageRoot of packageRoots) {
  const label = packageRoot.replace(repositoryRoot, "");
  test(`${label}: every consumer uses the reviewed version`, () => {
    assert.equal(require(join(packageRoot, "package.json")).version, "1.1.3");
  });

  for (const entry of ["src/sprintf.js", "dist/sprintf.min.js"]) {
    const { sprintf, vsprintf } = require(join(packageRoot, entry));
    test(`${label}/${entry}: excessive numeric precision is bounded`, () => {
      for (const precision of ["101", "1000000000", "9".repeat(400)]) {
        assert.equal(sprintf(`%.${precision}f`, 1), `1.${"0".repeat(100)}`);
        assert.equal(sprintf(`%.${precision}e`, 1), `1.${"0".repeat(100)}e+0`);
        assert.equal(sprintf(`%.${precision}g`, 1), "1");
      }
      assert.equal(sprintf("%.0g", 12.3), "10");
      assert.equal(vsprintf("%.101f", [1]), `1.${"0".repeat(100)}`);
    });
    test(`${label}/${entry}: legal precision and normal formatting stay compatible`, () => {
      assert.equal(sprintf("%.0f", 1.6), "2");
      assert.equal(sprintf("%.2f", 1.25), "1.25");
      assert.equal(sprintf("%.2e", 1.25), "1.25e+0");
      assert.equal(sprintf("%.3g", 12.5), "12.5");
      assert.equal(sprintf("%.100f", 1), `1.${"0".repeat(100)}`);
      assert.equal(sprintf("%.100e", 1), `1.${"0".repeat(100)}e+0`);
      assert.equal(sprintf("%+06d", 12), "+00012");
      assert.equal(sprintf("%(name)s:%(value).2f", { name: "value", value: 1.25 }), "value:1.25");
      assert.equal(sprintf("%.101s", "x".repeat(120)), "x".repeat(101));
      assert.throws(() => sprintf("%d", "not a number"), TypeError);
    });
    test(`${label}/${entry}: an asynchronous formatter survives the disclosed input`, () => {
      const script = `const { sprintf } = require(${JSON.stringify(join(packageRoot, entry))}); setImmediate(() => { sprintf('%.101f', 1); sprintf('%.101e', 1); sprintf('%.101g', 1); sprintf('%.0g', 1); process.stdout.write('completed'); });`;
      const child = spawnSync(process.execPath, ["-e", script], {
        encoding: "utf8",
        timeout: 5000,
      });
      assert.ifError(child.error);
      assert.equal(child.status, 0, child.stderr);
      assert.equal(child.stdout, "completed");
    });
  }
}

test("the actual legacy argparse consumer retains parsing and help output", () => {
  const argparseRoots = readdirSync(virtualStore).filter((name) => name.startsWith("argparse@1."));
  assert.ok(argparseRoots.length > 0, "The legacy formatting consumer must be exercised");
  for (const name of argparseRoots) {
    const { ArgumentParser } = require(join(virtualStore, name, "node_modules/argparse"));
    const parser = new ArgumentParser({ prog: "format-compatibility", addHelp: false });
    parser.addArgument(["--value"], { type: "int", help: "numeric %(type)s value" });
    assert.equal(parser.parseArgs(["--value", "12"]).value, 12);
    assert.match(parser.formatHelp(), /--value VALUE/);
    assert.match(parser.formatHelp(), /numeric int value/);
  }
});
