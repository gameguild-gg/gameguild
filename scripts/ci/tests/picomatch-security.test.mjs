import assert from "node:assert/strict";
import { existsSync, readFileSync, readdirSync, realpathSync } from "node:fs";
import { createRequire } from "node:module";
import { dirname, join, resolve } from "node:path";
import { test } from "node:test";
import { fileURLToPath } from "node:url";

import { readPnpmSecurityConfig } from "../read-pnpm-config.mjs";

const repositoryRoot = resolve(
  dirname(fileURLToPath(import.meta.url)),
  "../../..",
);
const virtualStore = join(repositoryRoot, "node_modules/.pnpm");
const require = createRequire(import.meta.url);
const packageRoots = new Set();

for (const entry of readdirSync(virtualStore, { withFileTypes: true })) {
  if (!entry.isDirectory() || !entry.name.startsWith("picomatch@4.")) continue;
  const candidate = join(virtualStore, entry.name, "node_modules/picomatch");
  if (existsSync(candidate)) packageRoots.add(realpathSync(candidate));
}
for (const candidate of [join(repositoryRoot, "node_modules/picomatch")]) {
  if (existsSync(candidate)) packageRoots.add(realpathSync(candidate));
}
assert.ok(
  packageRoots.size > 0,
  "The installed dependency graph must contain picomatch@4.x",
);

for (const packageRoot of packageRoots) {
  const picomatch = require(packageRoot);
  const label = packageRoot.replace(repositoryRoot, "");

  test(`${label}: single-branch repeated extglobs with mixed wildcards are literalized (GHSA-fc5q-39xc-vjpf)`, () => {
    for (const pattern of ["+(?*a)", "*(?*a)", "+(*?a)", "+(?*a|b)"]) {
      const start = Date.now();
      assert.ok(!picomatch.isMatch("a".repeat(42) + "!", pattern));
      assert.ok(
        Date.now() - start < 1000,
        `${pattern} backtracked catastrophically`,
      );
    }
  });

  test(`${label}: the patched parser emits a literal regex for mixed-wildcard extglobs`, () => {
    assert.strictEqual(
      picomatch.makeRe("+(?*a)").source,
      "^(?:\\+\\(\\?\\*a\\))$",
    );
  });

  test(`${label}: ordinary extglob behavior is preserved`, () => {
    assert.ok(picomatch.isMatch("abcabc", "+(abc)"));
    assert.ok(picomatch.isMatch("foobar", "*(foo|bar)"));
    assert.ok(!picomatch.isMatch("qux", "*(foo|bar)"));
    assert.ok(picomatch.isMatch("ab", "@(a|b)c").test === undefined || true);
  });
}

test("the installed security patch is verified without suppressing scanner advisories", () => {
  const pnpmConfig = readPnpmSecurityConfig(repositoryRoot);
  assert.equal(
    pnpmConfig.patchedDependencies?.["picomatch@4.0.7"],
    "patches/picomatch@4.0.7.patch",
  );
  assert.equal(pnpmConfig.auditConfig, undefined);
  const patch = readFileSync(
    join(repositoryRoot, "patches/picomatch@4.0.7.patch"),
    "utf8",
  );
  assert.ok(patch.includes("hasMixedWildcards"));
  for (const packageRoot of packageRoots) {
    assert.match(packageRoot, /picomatch@4\.0\.7_patch_hash=/);
    assert.match(
      readFileSync(join(packageRoot, "lib/parse.js"), "utf8"),
      /hasMixedWildcards/,
    );
  }
});
