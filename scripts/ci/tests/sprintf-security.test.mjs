import assert from "node:assert/strict";
import { existsSync, readFileSync, readdirSync, realpathSync } from "node:fs";
import { createRequire } from "node:module";
import { dirname, join, resolve } from "node:path";
import { test } from "node:test";
import { fileURLToPath } from "node:url";

const repositoryRoot = resolve(
  dirname(fileURLToPath(import.meta.url)),
  "../../..",
);
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
assert.ok(packageRoots.size > 0, "The installed graph must contain sprintf-js");

for (const packageRoot of packageRoots) {
  const { sprintf, vsprintf } = require(packageRoot);
  const label = packageRoot.replace(repositoryRoot, "");
  test(`${label}: excessive floating precision cannot raise RangeError`, () => {
    for (const kind of ["e", "f", "g"]) {
      for (const precision of ["101", "1000000000", "9".repeat(400)]) {
        let formatted;
        assert.doesNotThrow(() => {
          formatted = sprintf(`%.${precision}${kind}`, 1.2345);
        });
        assert.equal(typeof formatted, "string");
        assert.ok(formatted.length <= 110);
        assert.doesNotThrow(() => vsprintf(`%.${precision}${kind}`, [1.2345]));
      }
    }
    assert.doesNotThrow(() => sprintf("%.0g", 1.2345));
  });
  test(`${label}: ordinary formatting and valid precision remain compatible`, () => {
    assert.equal(sprintf("%s: %04d", "value", 7), "value: 0007");
    assert.equal(sprintf("%.2f", 1.2345), "1.23");
    assert.equal(sprintf("%.2e", 1.2345), "1.23e+0");
    assert.equal(sprintf("%.3g", 1.2345), "1.23");
    assert.equal(vsprintf("%(name)s", [{ name: "value" }]), "value");
    assert.equal(sprintf("%.100f", 1.2345), (1.2345).toFixed(100));
  });
}

test("the advisory mitigation requires the exact installed patch on every consumer", () => {
  const manifest = JSON.parse(
    readFileSync(join(repositoryRoot, "package.json"), "utf8"),
  );
  assert.equal(
    manifest.pnpm.patchedDependencies?.["sprintf-js@1.1.3"],
    "patches/sprintf-js@1.1.3.patch",
  );
  assert.equal(manifest.pnpm.auditConfig, undefined);
  const scannerExceptions = readFileSync(
    join(repositoryRoot, ".trivyignore"),
    "utf8",
  )
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line && !line.startsWith("#"));
  assert.deepEqual(scannerExceptions, ["CVE-2026-93687", "CVE-2026-97058"]);
  for (const packageRoot of packageRoots) {
    assert.match(packageRoot, /sprintf-js@1\.1\.3_patch_hash[=_]/);
    assert.match(
      readFileSync(join(packageRoot, "src/sprintf.js"), "utf8"),
      /function sprintf_precision/,
    );
  }
});
